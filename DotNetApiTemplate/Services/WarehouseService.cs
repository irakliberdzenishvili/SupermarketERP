
using AutoMapper;
using DotNetApiTemplate.DB;
using DotNetApiTemplate.Helpers;
using DotNetApiTemplate.Models.Data;
using DotNetApiTemplate.Models.Response;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SupermarketERP.Constants;
using SupermarketERP.Exceptions;
using SupermarketERP.Helpers;
using SupermarketERP.Interfaces;
using SupermarketERP.Models.Data;
using SupermarketERP.Models.Request.Warehouse;
using SupermarketERP.Models.Response.Warehouse;

namespace SupermarketERP.Services;

public class WarehouseService(
    IMapper mapper,
    ApplicationDbContext db,
    AuthHelper authHelper,
    UserManager<User> userManager,
    WarehouseHelper warehouseHelper
) : IWarehouseService
{
    private string CurrentUserId => authHelper.UserId;

    private string CurrentUserRole => authHelper.UserRole;

    private async Task<bool> IsAdmin()
    {
        var user = await userManager.FindByIdAsync(CurrentUserId);

        if (user == null)
            throw new UserFriendlyException(ErrorMessages.UserNotFound);

        return await userManager.IsInRoleAsync(user, Roles.Admin);
    }

    private async Task<bool> IsWarehouseManager()
    {
        var user = await userManager.FindByIdAsync(CurrentUserId);

        return user != null &&
            await userManager.IsInRoleAsync(user, Roles.WarehouseManager);
    }

    private WarehouseResponse MapWarehouse(Warehouse warehouse)
    {
        var response = mapper.Map<WarehouseResponse>(warehouse);

        response.IsManager =
            warehouse.WarehouseManager != null &&
            warehouse.WarehouseManager.UserId == CurrentUserId;

        response.IsAdmin = CurrentUserRole == Roles.Admin;

        return response;
    }

    #region GET WAREHOUSES

    public async Task<PagedApiResponse<WarehouseResponse>> GetWarehouses(
        string? searchTerm,
        int pageNumber = 1,
        int pageSize = 10)
    {
        if (!await IsAdmin())
            throw new UserFriendlyException(ErrorMessages.Forbidden);

        if (pageNumber <= 0 || pageSize <= 0)
            throw new UserFriendlyException(ErrorMessages.InvalidPagination);

        var query = db.Warehouses
            .AsNoTracking()
            .Include(w => w.Branches)
            .Include(w => w.WarehouseManager)
                .ThenInclude(wm => wm.User)
            .Where(w => w.IsActive && !w.IsDeleted);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(w =>
                w.Code.Contains(searchTerm) ||
                w.Name.Contains(searchTerm) ||
                w.Address.Contains(searchTerm) ||
                (w.PhoneNumber != null &&
                 w.PhoneNumber.Contains(searchTerm)));
        }

        var totalCount = await query.CountAsync();

        var warehouses = await query
            .OrderBy(w => w.CreatedAt)
            .ThenBy(w => w.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var responses = warehouses
            .Select(MapWarehouse)
            .ToList();

        return new PagedApiResponse<WarehouseResponse>(
            responses,
            totalCount,
            pageSize,
            pageNumber);
    }

    #endregion

    #region CREATE WAREHOUSE


    public async Task<ApiResponse<WarehouseResponse>> CreateWarehouse(
        CreateWarehouseRequest request)
    {
        if (!await IsAdmin())
            throw new UserFriendlyException(ErrorMessages.Forbidden);

        if (string.IsNullOrWhiteSpace(request.WarehouseManagerUserId))
        {
            throw new UserFriendlyException(
        ErrorMessages.WarehouseManagerUserIdIsRequired);
        }

        var user = await userManager.FindByIdAsync(
            request.WarehouseManagerUserId);

        if (user == null)
            throw new UserFriendlyException(ErrorMessages.UserNotFound);

        if (await userManager.IsInRoleAsync(user, Roles.Admin))
        {
            throw new UserFriendlyException(
                ErrorMessages.WarehouseManagerCannotBeAdmin);
        }

        var alreadyManager = await db.WarehouseManagers
            .AnyAsync(wm => wm.UserId == user.Id);

        if (alreadyManager)
        {
             throw new UserFriendlyException(
                  ErrorMessages.WarehouseManagerAlreadyAssigned);
        }
            

        var activeBranchWorker = await db.BranchWorkers
            .AnyAsync(w =>
                w.UserId == user.Id &&
                w.IsActive &&
                !w.IsDeleted);

        if (activeBranchWorker)
        {
            throw new UserFriendlyException(
                ErrorMessages.WarehouseManagerCannotBeBranchWorker);
        }

        var branchIds = (request.BranchIds ?? new List<Guid>())
            .Distinct()
            .ToList();

        var branches = await db.Branches
            .Where(b =>
                branchIds.Contains(b.Id) &&
                b.IsActive &&
                !b.IsDeleted)
            .ToListAsync();

        if (branches.Count != branchIds.Count)
            throw new UserFriendlyException(ErrorMessages.BranchNotFound);

        if (branches.Any(b => b.WarehouseId.HasValue))
        {
            throw new UserFriendlyException(
                ErrorMessages.BranchAlreadyAssigned);
        }

        var warehouse = mapper.Map<Warehouse>(request);

        warehouse.Code = await warehouseHelper.GenerateWarehouseCode();

        warehouse.CreatedAt = DateTime.UtcNow;
        warehouse.UpdatedAt = null;

        await using var transaction =
            await db.Database.BeginTransactionAsync();

        var roleResult = await userManager.IsInRoleAsync(
            user, Roles.WarehouseManager);

        if (!roleResult)
        {
            var result = await userManager.AddToRoleAsync(
                user, Roles.WarehouseManager);

            EnsureIdentitySuccess(result);
        }

        await db.Warehouses.AddAsync(warehouse);

        foreach (var branch in branches)
        {
            branch.WarehouseId = warehouse.Id;
            branch.UpdatedAt = DateTime.UtcNow;
        }

        var warehouseManager = new WarehouseManager
        {
            UserId = user.Id,
            WarehouseId = warehouse.Id,
            Warehouse = warehouse,
            AssignedAt = DateTime.UtcNow
        };

        await db.WarehouseManagers.AddAsync(warehouseManager);

        await db.SaveChangesAsync();
        await transaction.CommitAsync();

        var createdWarehouse = await db.Warehouses
            .AsNoTracking()
            .Include(b => b.Branches)
            .Include(wm => wm.WarehouseManager)
                .ThenInclude(u => u.User)
            .FirstAsync(w => w.Id == warehouse.Id);

        return new ApiResponse<WarehouseResponse>
        {
            Data = MapWarehouse(createdWarehouse),
            IsSuccess = true,
            Message = "Warehouse created successfully."
        };
    }

    #endregion

    #region GET WAREHOUSE BY ID

    public async Task<ApiResponse<WarehouseResponse>> GetWarehouseById(
        Guid id)
    {
        var isAdmin = await IsAdmin();
        var isManager = await IsWarehouseManager();

        if (!isAdmin && !isManager)
            throw new UserFriendlyException(ErrorMessages.Forbidden);

        var query = db.Warehouses
            .AsNoTracking()
            .Include(w => w.WarehouseManager)
                .ThenInclude(wm => wm.User)
            .Include(w => w.Branches)
            .Where(w =>
                w.Id == id &&
                w.IsActive &&
                !w.IsDeleted);

        if (!isAdmin)
        {
            query = query.Where(w =>
                w.WarehouseManager != null &&
                w.WarehouseManager.UserId == CurrentUserId);
        }

        var warehouse = await query.FirstOrDefaultAsync();

        if (warehouse == null)
        {
            throw new UserFriendlyException(
                ErrorMessages.WarehouseNotFound);
        }

        return new ApiResponse<WarehouseResponse>
        {
            Data = MapWarehouse(warehouse),
            IsSuccess = true,
            Message = "Warehouse retrieved successfully."
        };
    }

    #endregion

    #region GET WAREHOUSE BY CODE

    public async Task<ApiResponse<WarehouseResponse>> GetWarehouseByCode(
        string code)
    {
        var isAdmin = await IsAdmin();
        var isManager = await IsWarehouseManager();

        if (!isAdmin && !isManager)
            throw new UserFriendlyException(ErrorMessages.Forbidden);

        var query = db.Warehouses
            .AsNoTracking()
            .Include(w => w.WarehouseManager)
                .ThenInclude(wm => wm.User)
            .Include(w => w.Branches)
            .Where(w =>
                w.Code == code &&
                w.IsActive &&
                !w.IsDeleted);

        if (!isAdmin)
        {
            query = query.Where(w =>
                w.WarehouseManager != null &&
                w.WarehouseManager.UserId == CurrentUserId);
        }

        var warehouse = await query.FirstOrDefaultAsync();

        if (warehouse == null)
        {
            throw new UserFriendlyException(
                ErrorMessages.WarehouseNotFound);
        }

        return new ApiResponse<WarehouseResponse>
        {
            Data = MapWarehouse(warehouse),
            IsSuccess = true,
            Message = "Warehouse retrieved successfully."
        };
    }

    #endregion

    #region CHANGE WAREHOUSE STATUS

    public async Task<ApiResponse<WarehouseResponse>> ChangeWarehouseStatus(
        Guid warehouseId,
        ChangeWarehouseStatusRequest request)
    {
        if (!await IsAdmin())
            throw new UserFriendlyException(ErrorMessages.Forbidden);

        var warehouse = await db.Warehouses
            .Include(w => w.WarehouseManager)
                .ThenInclude(wm => wm.User)
            .FirstOrDefaultAsync(w =>
                w.Id == warehouseId &&
                !w.IsDeleted);

        if (warehouse == null)
        {
            throw new UserFriendlyException(
                ErrorMessages.WarehouseNotFound);
        }

        if (request.IsActive)
        {
            var hasManager = await db.WarehouseManagers
                .AnyAsync(wm => wm.WarehouseId == warehouseId);

            if (!hasManager)
            {
                throw new UserFriendlyException(
                    ErrorMessages.WarehouseManagerIsRequired);
            }
        }

        warehouse.IsActive = request.IsActive;
        warehouse.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();

        return new ApiResponse<WarehouseResponse>
        {
            Data = MapWarehouse(warehouse),
            IsSuccess = true,
            Message = warehouse.IsActive
                ? "Warehouse activated successfully."
                : "Warehouse deactivated successfully."
        };
    }

    #endregion

    private static void EnsureIdentitySuccess(IdentityResult result)
    {
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                string.Join("; ",
                    result.Errors.Select(e => e.Description)));
        }
    }
}
