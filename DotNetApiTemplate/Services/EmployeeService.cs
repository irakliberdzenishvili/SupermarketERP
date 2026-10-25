using AutoMapper;
using DotNetApiTemplate.DB;
using DotNetApiTemplate.Helpers;
using DotNetApiTemplate.Interfaces;
using DotNetApiTemplate.Models.Data;
using DotNetApiTemplate.Models.Response;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SupermarketERP.Constants;
using SupermarketERP.Exceptions;
using SupermarketERP.Helpers;
using SupermarketERP.Interfaces;
using SupermarketERP.Models.Data;
using SupermarketERP.Models.Request.Employee;
using SupermarketERP.Models.Response;
using SupermarketERP.Models.Response.Employee;

namespace SupermarketERP.Services
{
    public class EmployeeService(
        AuthHelper authHelper,
        ApplicationDbContext db,
        UserManager<User> userManager,
        IMapper mapper,
        EmployeeHelper employeeHelper,
        IEmailService emailService,
        IConfiguration configuration) : IEmployeeService
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

        public async Task<ApiResponse<EmployeeResponse>> CreateEmployee(
    CreateEmployeeRequest request)
        {
            if (!await IsAdmin())
            {
                throw new UserFriendlyException(ErrorMessages.Forbidden);
            }

            var user = await employeeHelper.GetUserForProvisioning(
                request.UserId);

            await employeeHelper.ValidateEmployeeRequest(request);

            await employeeHelper.ValidateUserEligibility(user);

            await using var transaction =
                await db.Database.BeginTransactionAsync();

            await employeeHelper.AssignWorkplace(user, request);

            await employeeHelper.AssignEmployeeRole(user, request.Role);

            await db.SaveChangesAsync();

            await transaction.CommitAsync();

            var token = await userManager.GeneratePasswordResetTokenAsync(user);

            var resetLink =
                $"{configuration["Frontend:BaseUrl"]}/reset-password" +
                $"?userId={Uri.EscapeDataString(user.Id)}" +
                $"&token={Uri.EscapeDataString(token)}";

            var subject = "Welcome to SupermarketERP";

            var htmlBody = $"""
        <h2>Welcome to SupermarketERP!</h2>

        <p>Your employee account has been activated.</p>

        <p><strong>Role:</strong>
            {System.Net.WebUtility.HtmlEncode(request.Role)}
        </p>

        <p>Click the link below to set your password:</p>

        <p>
            <a href="{System.Net.WebUtility.HtmlEncode(resetLink)}">
                Set Your Password
            </a>
        </p>

        <p>If you weren't expecting this email,
           please contact your administrator.</p>
        """;

            await emailService.SendEmailAsync(
                user.Email!,
                subject,
                htmlBody);

            return await employeeHelper.GetEmployeeResponse(user.Id);
        }
    }
}
