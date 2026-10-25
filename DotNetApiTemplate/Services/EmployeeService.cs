using AutoMapper;
using DotNetApiTemplate.DB;
using DotNetApiTemplate.Exceptions;
using DotNetApiTemplate.Helpers;
using DotNetApiTemplate.Models.Data;
using DotNetApiTemplate.Models.Response;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SupermarketERP.Constants;
using SupermarketERP.Interfaces;
using SupermarketERP.Models.Response;
using SupermarketERP.Models.Response.Employee;

namespace SupermarketERP.Services
{
    public class EmployeeService(
        AuthHelper authHelper,
        ApplicationDbContext db,
        UserManager<User> userManager,
        IMapper mapper) : IEmployeeService
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

        private async Task<bool> IsAllowedToSeeEmployees(
    Guid resourceId,
    bool isBranch)
        {
            var user = await userManager.FindByIdAsync(CurrentUserId);

            if (user == null)
                throw new UserFriendlyException(ErrorMessages.UserNotFound);

            if (await IsAdmin())
                return true;

            if (isBranch)
            {
                if (!await userManager.IsInRoleAsync(user, Roles.BranchManager))
                    return false;

                return await db.Branches.AnyAsync(b =>
                    b.Id == resourceId &&
                    !b.IsDeleted &&
                    b.IsActive &&
                    b.BranchManager != null &&
                    b.BranchManager.UserId == CurrentUserId &&
                    b.BranchManager.IsActive &&
                    !b.BranchManager.IsDeleted);
            }

            if (!await userManager.IsInRoleAsync(user, Roles.WarehouseManager))
                return false;

            return await db.WarehouseManagers.AnyAsync(wm =>
                wm.UserId == CurrentUserId &&
                wm.WarehouseId == resourceId &&
                !wm.IsDeleted &&
                wm.Warehouse.IsActive &&
                !wm.Warehouse.IsDeleted);
        }
        public async Task<PagedApiResponse<EmployeeResponse>> GetEmployees(
     string? role,
     bool? status,
     Guid resourceId,
     int pageNumber = 1,
     int pageSize = 10)
        {
            pageNumber = Math.Max(1, pageNumber);
            pageSize = Math.Clamp(pageSize, 1, 100);

            role = role?.Trim();

            var isBranch = await db.Branches
                .AnyAsync(b => b.Id == resourceId && !b.IsDeleted);

            var isWarehouse = await db.Warehouses
                .AnyAsync(w => w.Id == resourceId && !w.IsDeleted);

            if (!isBranch && !isWarehouse)
            {
                throw new UserFriendlyException(
                    ErrorMessages.ResourceNotFound);
            }

            if (isBranch && isWarehouse)
            {
                throw new InvalidOperationException(
                    "Resource ID belongs to both a branch and a warehouse.");
            }

            if (!await IsAllowedToSeeEmployees(resourceId, isBranch))
            {
                throw new UserFriendlyException(
                    ErrorMessages.Forbidden);
            }

            var response = new EmployeeResponse();
            int totalCount;

            if (isBranch)
            {
                var query = db.BranchWorkers
                    .AsNoTracking()
                    .Where(e =>
                        e.BranchId == resourceId &&
                        !e.IsDeleted);

                if (status.HasValue)
                {
                    query = query.Where(e =>
                        e.IsActive == status.Value);
                }

                if (!string.IsNullOrWhiteSpace(role))
                {
                    query = query.Where(e =>
                        db.UserRoles.Any(ur =>
                            ur.UserId == e.UserId &&
                            db.Roles.Any(r =>
                                r.Id == ur.RoleId &&
                                r.Name == role)));
                }

                totalCount = await query.CountAsync();

                var employees = await query
                    .OrderBy(e => e.Id)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                response.BranchWorkers =
                    mapper.Map<List<BranchWorkerResponse>>(employees);
            }
            else
            {
                var query = db.WarehouseWorkers
                    .AsNoTracking()
                    .Where(w =>
                        w.WarehouseId == resourceId &&
                        !w.IsDeleted);

                if (status.HasValue)
                {
                    query = query.Where(w =>
                        w.IsActive == status.Value);
                }

                if (!string.IsNullOrWhiteSpace(role))
                {
                    query = query.Where(w =>
                        db.UserRoles.Any(ur =>
                            ur.UserId == w.UserId &&
                            db.Roles.Any(r =>
                                r.Id == ur.RoleId &&
                                r.Name == role)));
                }

                totalCount = await query.CountAsync();

                var workers = await query
                    .OrderBy(w => w.Id)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                response.WarehouseWorkers =
                    mapper.Map<List<WarehouseWorkerResponse>>(workers);
            }

            return new PagedApiResponse<EmployeeResponse>(
                new[] { response },
                totalCount,
                pageNumber,
                pageSize)
            {
                IsSuccess = true,
                Message = "Employees retrieved successfully."
            };
        }
    }
}
