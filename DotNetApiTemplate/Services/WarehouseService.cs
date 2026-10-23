using AutoMapper;
using DotNetApiTemplate.DB;
using DotNetApiTemplate.Exceptions;
using DotNetApiTemplate.Helpers;
using DotNetApiTemplate.Models.Data;
using DotNetApiTemplate.Models.Response;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SupermarketERP.Constants;
using SupermarketERP.Helpers;
using SupermarketERP.Interfaces;
using SupermarketERP.Models.Data;
using SupermarketERP.Models.Request.Warehouse;
using SupermarketERP.Models.Response.Warehouse;

namespace SupermarketERP.Services
{
    public class WarehouseService(
        IMapper mapper,
        ApplicationDbContext db,
        AuthHelper authHelper,
        UserManager<User> userManager,
        WarehouseHelper warehouseHelper) : IWarehouseService
    {
        private string CurrentUserId => authHelper.UserId;
        private string CurrentUserRole => authHelper.UserRole;
        private Guid? CurrentEmployeeId =>  GetCurrentEmployeeId();

         private Guid? GetCurrentEmployeeId()
        {
                Guid? currentEmployeeId = db.Employees
        .AsNoTracking()
        .Where(e => e.UserId == CurrentUserId
            && e.IsActive
            && !e.IsDeleted)
        .Select(e => (Guid?)e.Id)
        .FirstOrDefault();

            return currentEmployeeId;
        }
        private async Task<bool> IsAdmin()
        {
            var user = await userManager.FindByIdAsync(CurrentUserId);

            if (user == null)
            {
                throw new UserFriendlyException(ErrorMessages.UserNotFound);
            }

            return await userManager.IsInRoleAsync(user, Roles.Admin);
        }

        private WarehouseResponse MapWarehouse(
    Warehouse warehouse,
    Guid? currentEmployeeId)
        {
            var response = mapper.Map<WarehouseResponse>(warehouse);

            response.IsManager = currentEmployeeId.HasValue
                && warehouse.WarehouseManagerId == currentEmployeeId.Value;

            response.IsAdmin = CurrentUserRole == Roles.Admin;

            return response;
        }
        public async Task<PagedApiResponse<WarehouseResponse>> GetWarehouses(string? searchTerm, int pageNumber = 1, int pageSize = 10)
        {
            if (!await IsAdmin())
            {
                throw new UserFriendlyException(ErrorMessages.Forbidden);
            }

            var warehouses = db.Warehouses
    .AsNoTracking()
    .Include(w => w.Branches)
      .Include(w => w.WarehouseManager)
         .ThenInclude(e => e.Employee)
            .ThenInclude(u => u.User)
    .Where(w => w.IsActive && !w.IsDeleted);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                warehouses = warehouses.Where(w => w.Code.Contains(searchTerm)
                || w.Address.Contains(searchTerm)
                || w.PhoneNumber.Contains(searchTerm));
            }

            var totalCount = await warehouses.CountAsync();

            var pagedWarehouses = await warehouses
                .OrderBy(b => b.CreatedAt)
                .ThenBy(b => b.Id)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();


            var mappedWarehouses = pagedWarehouses
            .Select(warehouse => MapWarehouse(warehouse, CurrentEmployeeId))
            .ToList();

            return new PagedApiResponse<WarehouseResponse>(
                mappedWarehouses,
                totalCount,
                pageSize,
                pageNumber);

        }

        public async Task<ApiResponse<WarehouseResponse>> CreateWarehouse(
      CreateWarehouseRequest request)
        {
            if (!await IsAdmin())
            {
                throw new UserFriendlyException(ErrorMessages.Forbidden);
            }

            if (request.WarehouseManagerId == Guid.Empty)
            {
                throw new UserFriendlyException(
                    ErrorMessages.WarehouseManagerIdIsRequired);
            }

            var branchIds = request.BranchIds.Distinct().ToList();

            var branches = await db.Branches
                .Where(b => branchIds.Contains(b.Id)
                    && b.IsActive
                    && !b.IsDeleted)
                .ToListAsync();

            if (branches.Count != branchIds.Count)
            {
                throw new UserFriendlyException(ErrorMessages.BranchNotFound);
            }

            if (branches.Any(b => b.WarehouseId != null))
            {
                throw new UserFriendlyException(ErrorMessages.BranchAlreadyAssigned);
            }

            var employee = await db.Employees
                .Include(e => e.User)
                .FirstOrDefaultAsync(e =>
                    e.Id == request.WarehouseManagerId
                    && e.IsActive
                    && !e.IsDeleted);

            if (employee == null)
            {
                throw new UserFriendlyException(ErrorMessages.EmployeeNotFound);
            }

            var user = employee.User;

            if (user == null)
            {
                throw new UserFriendlyException(ErrorMessages.UserNotFound);
            }

            var alreadyManager = await db.WarehouseManagers
                .AnyAsync(wm => wm.EmployeeId == employee.Id);

            if (alreadyManager)
            {
                throw new UserFriendlyException(ErrorMessages.Forbidden);
            }

            var warehouse = mapper.Map<Warehouse>(request);

            warehouse.Code = await warehouseHelper.GenerateWarehouseCode();

            await using var transaction =
                await db.Database.BeginTransactionAsync();

            var isWarehouseManager = await userManager.IsInRoleAsync(
                user, Roles.WarehouseManager);

            if (!isWarehouseManager)
            {
                var roleResult = await userManager.AddToRoleAsync(
                    user, Roles.WarehouseManager);

                if (!roleResult.Succeeded)
                {
                    throw new UserFriendlyException(ErrorMessages.Forbidden);
                }
            }

            var isEmployee = await userManager.IsInRoleAsync(
                user, Roles.Employee);

            if (isEmployee)
            {
                var removeResult = await userManager.RemoveFromRoleAsync(
                    user, Roles.Employee);

                if (!removeResult.Succeeded)
                {
                    throw new UserFriendlyException(ErrorMessages.Forbidden);
                }
            }

            await db.Warehouses.AddAsync(warehouse);

            foreach (var branch in branches)
            {
                branch.WarehouseId = warehouse.Id;
            }

            var warehouseManager = new WarehouseManager
            {
                EmployeeId = employee.Id,
                Employee = employee,
                WarehouseId = warehouse.Id,
                Warehouse = warehouse
            };

            await db.WarehouseManagers.AddAsync(warehouseManager);

            await db.SaveChangesAsync();

            await transaction.CommitAsync();

            var createdWarehouse = await db.Warehouses
                .AsNoTracking()
                .Include(w => w.Branches)
                .Include(w => w.WarehouseManager)
                    .ThenInclude(wm => wm.Employee)
                        .ThenInclude(e => e.User)
                .FirstAsync(w => w.Id == warehouse.Id);


            var mappedResponse = MapWarehouse(
                createdWarehouse,
                CurrentEmployeeId);

            return new ApiResponse<WarehouseResponse>
            {
                Data = mappedResponse,
                IsSuccess = true,
                Message = "Warehouse Created"
            };
        }

        public async Task<ApiResponse<WarehouseResponse>> GetWarehouseById(Guid id)
        {
            var isAdmin = await IsAdmin();
            var isWarehouseManager = CurrentUserRole == Roles.WarehouseManager;

            if (!isAdmin && !isWarehouseManager)
            {
                throw new UserFriendlyException(ErrorMessages.Forbidden);
            }

            var query =  db.Warehouses
                .AsNoTracking()
                .Include(x => x.WarehouseManager)
                   .ThenInclude(b => b.Employee)
                      .ThenInclude(b => b.User)
                .Include(x => x.Branches)
                .Where(x => x.Id == id && x.IsActive && !x.IsDeleted);

            if (!isAdmin)
            {

                if (!CurrentEmployeeId.HasValue)
                {
                    throw new UserFriendlyException(ErrorMessages.EmployeeNotFound);
                }

                query = query.Where(w => w.WarehouseManager != null && w.WarehouseManager.EmployeeId == CurrentEmployeeId);
            }

            var warehouse = await query.FirstOrDefaultAsync();

            if (warehouse == null)
            {
                throw new UserFriendlyException(ErrorMessages.WarehouseNotFound);
            }

            var mappedResponse = MapWarehouse(warehouse, CurrentEmployeeId);

            return new ApiResponse<WarehouseResponse>
            {
                Data = mappedResponse,
                IsSuccess = true,
                Message = "Warehouse Retrieved"
            };
        }

        public async Task<ApiResponse<WarehouseResponse>> GetWarehouseByCode(string code)
        {
            var isAdmin = await IsAdmin();
            var isWarehouseManager = CurrentUserRole == Roles.WarehouseManager;

            if (!isAdmin && !isWarehouseManager)
            {
                throw new UserFriendlyException(ErrorMessages.Forbidden);
            }

            var query =  db.Warehouses
                .AsNoTracking()
                .Include(b => b.WarehouseManager)
                   .ThenInclude(wm => wm.Employee)
                     .ThenInclude(e => e.User)
                .Include(b => b.Branches)
                .Where(w => w.Code == code && !w.IsDeleted && w.IsActive);


           if (!isAdmin)
            {
                if (!CurrentEmployeeId.HasValue)
                {
                    throw new UserFriendlyException(ErrorMessages.EmployeeNotFound);
                }


                query = query.Where(w => w.WarehouseManager != null && w.WarehouseManager.EmployeeId == CurrentEmployeeId);
            }

            var warehouse = await query.FirstOrDefaultAsync();

            if (warehouse == null)
            {
                throw new UserFriendlyException(ErrorMessages.WarehouseNotFound);
            }

            var mappedResponse = MapWarehouse(warehouse, CurrentEmployeeId);

            return new ApiResponse<WarehouseResponse>
            {
                Data = mappedResponse,
                IsSuccess = true,
                Message = "Warehouse Retrieved"
            };
        }

        public async Task<ApiResponse<WarehouseResponse>> ChangeWarehouseStatus(
    Guid warehouseId,
    ChangeWarehouseStatusRequest request)
        {
            if (!await IsAdmin())
            {
                throw new UserFriendlyException(ErrorMessages.Forbidden);
            }

            var warehouse = await db.Warehouses
                .Include(w => w.WarehouseManager)
                .ThenInclude(e => e.Employee)
                .FirstOrDefaultAsync(w =>
                    w.Id == warehouseId && !w.IsDeleted);

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
                Data = MapWarehouse(warehouse, null),
                IsSuccess = true,
                Message = warehouse.IsActive
                    ? "Warehouse activated successfully."
                    : "Warehouse deactivated successfully."
            };
        }
    }
}
