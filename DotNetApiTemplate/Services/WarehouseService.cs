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

        private async Task<bool> IsAdmin()
        {
            var user = await userManager.FindByIdAsync(CurrentUserId);

            if (user == null)
            {
                throw new UserFriendlyException(ErrorMessages.UserNotFound);
            }

            if (!await userManager.IsInRoleAsync(user, Roles.Admin))
            {
                throw new UserFriendlyException(ErrorMessages.Forbidden);
            }

            return true;
        }

        private WarehouseResponse MapWarehouse(
    Warehouse warehouse,
    Guid? currentEmployeeId)
        {
            var response = mapper.Map<WarehouseResponse>(warehouse);

            response.IsManager = currentEmployeeId.HasValue
                && warehouse.WarehouseManager?.EmployeeId == currentEmployeeId.Value;

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

            Guid? currentEmployeeId = null;

            var currentUserId = authHelper.UserId;

            if (!string.IsNullOrWhiteSpace(currentUserId))
            {
                currentEmployeeId = await db.Employees
                    .AsNoTracking()
                    .Where(e =>
                        e.UserId == currentUserId &&
                        e.IsActive)
                    .Select(e => (Guid?)e.Id)
                    .FirstOrDefaultAsync();
            }

            var mappedWarehouses = pagedWarehouses
            .Select(warehouse => MapWarehouse(warehouse, currentEmployeeId))
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

            Guid? currentEmployeeId = await db.Employees
                .AsNoTracking()
                .Where(e => e.UserId == CurrentUserId && e.IsActive)
                .Select(e => (Guid?)e.Id)
                .FirstOrDefaultAsync();

            var mappedResponse = MapWarehouse(
                createdWarehouse,
                currentEmployeeId);

            return new ApiResponse<WarehouseResponse>
            {
                Data = mappedResponse,
                IsSuccess = true,
                Message = "Warehouse Created"
            };
        }
    }
}
