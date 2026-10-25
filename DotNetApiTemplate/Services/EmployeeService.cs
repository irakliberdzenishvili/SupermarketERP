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

namespace SupermarketERP.Services;

public class EmployeeService(
    AuthHelper authHelper,
    ApplicationDbContext db,
    UserManager<User> userManager,
    IMapper mapper,
    EmployeeHelper employeeHelper,
    IEmailService emailService,
    IConfiguration configuration) : IEmployeeService
{
    private async Task<bool> HasEmployeeAccess(
        Guid? resourceId = null,
        bool isBranch = false,
        bool adminOnly = false)
    {
        var user = await userManager.FindByIdAsync(authHelper.UserId);

        if (user == null)
            throw new UserFriendlyException(ErrorMessages.UserNotFound);

        if (await userManager.IsInRoleAsync(user, Roles.Admin))
            return true;

        if (adminOnly || !resourceId.HasValue)
            return false;

        if (isBranch)
        {
            if (!await userManager.IsInRoleAsync(user, Roles.BranchManager))
                return false;

            return await db.Branches.AnyAsync(b =>
                b.Id == resourceId.Value &&
                !b.IsDeleted &&
                b.IsActive &&
                b.BranchManager != null &&
                b.BranchManager.UserId == user.Id &&
                b.BranchManager.IsActive &&
                !b.BranchManager.IsDeleted);
        }

        if (!await userManager.IsInRoleAsync(user, Roles.WarehouseManager))
            return false;

        return await db.WarehouseManagers.AnyAsync(wm =>
            wm.UserId == user.Id &&
            wm.WarehouseId == resourceId.Value &&
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
            throw new UserFriendlyException(ErrorMessages.ResourceNotFound);

        if (isBranch && isWarehouse)
            throw new InvalidOperationException(
                "Resource ID belongs to both a branch and a warehouse.");

        if (!await HasEmployeeAccess(resourceId, isBranch))
            throw new UserFriendlyException(ErrorMessages.Forbidden);

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
                query = query.Where(e => e.IsActive == status.Value);

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

            var workers = await query
                .OrderBy(e => e.Id)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            response.BranchWorkers =
                mapper.Map<List<BranchWorkerResponse>>(workers);
        }
        else
        {
            var query = db.WarehouseWorkers
                .AsNoTracking()
                .Where(e =>
                    e.WarehouseId == resourceId &&
                    !e.IsDeleted);

            if (status.HasValue)
                query = query.Where(e => e.IsActive == status.Value);

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

            var workers = await query
                .OrderBy(e => e.Id)
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
        if (!await HasEmployeeAccess(adminOnly: true))
            throw new UserFriendlyException(ErrorMessages.Forbidden);

        var user = await employeeHelper.GetUserForProvisioning(request.UserId);

        await employeeHelper.ValidateEmployeeRequest(request);
        await employeeHelper.ValidateUserEligibility(user);

        await using (var transaction = await db.Database.BeginTransactionAsync())
        {
            await employeeHelper.AssignWorkplace(user, request);
            await employeeHelper.AssignEmployeeRole(user, request.Role);

            await db.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        var token = await userManager.GeneratePasswordResetTokenAsync(user);

        var resetLink =
            $"{configuration["Frontend:BaseUrl"]}/reset-password" +
            $"?userId={Uri.EscapeDataString(user.Id)}" +
            $"&token={Uri.EscapeDataString(token)}";

        var htmlBody = $"""
            <h2>Welcome to SupermarketERP!</h2>
            <p>Your employee account has been activated.</p>
            <p><strong>Role:</strong> {System.Net.WebUtility.HtmlEncode(request.Role)}</p>
            <p>Click the link below to set your password:</p>
            <p>
                <a href="{System.Net.WebUtility.HtmlEncode(resetLink)}">
                    Set Your Password
                </a>
            </p>
            <p>If you weren't expecting this email, please contact your administrator.</p>
            """;

        await emailService.SendEmailAsync(
            user.Email!,
            "Welcome to SupermarketERP",
            htmlBody);

        return await employeeHelper.GetEmployeeResponse(user.Id);
    }


    public async Task<ApiResponse<EmployeeResponse>> GetEmployeeById(Guid resourceId)
    {
        var isBranch = await db.Branches.AnyAsync(b =>
            b.Id == resourceId && b.IsActive && !b.IsDeleted);

        var isWarehouse = await db.Warehouses.AnyAsync(w =>
            w.Id == resourceId && w.IsActive && !w.IsDeleted);

        if (!isBranch && !isWarehouse)
        {
            throw new UserFriendlyException(ErrorMessages.ResourceNotFound);
        }

        if (isBranch && isWarehouse)
        {
            throw new InvalidOperationException(
                "Resource ID belongs to both a branch and a warehouse.");
        }

        if (!await HasEmployeeAccess(resourceId, isBranch))
        {
            throw new UserFriendlyException(ErrorMessages.Forbidden);
        }

        var response = new EmployeeResponse();

        if (isBranch)
        {
            var branchWorker = await db.BranchWorkers
                .AsNoTracking()
                .FirstOrDefaultAsync(b =>
                    b.BranchId == resourceId &&
                    b.IsActive &&
                    !b.IsDeleted);

            if (branchWorker == null)
            {
                throw new UserFriendlyException(ErrorMessages.BranchWorkerNotFound);
            }

            response.BranchWorkers = new List<BranchWorkerResponse>
            {
            mapper.Map<BranchWorkerResponse>(branchWorker)
            };
        }
        else
        {
            var warehouseWorker = await db.WarehouseWorkers
                .AsNoTracking()
                .FirstOrDefaultAsync(w =>
                    w.WarehouseId == resourceId &&
                    w.IsActive &&
                    !w.IsDeleted);

            if (warehouseWorker == null)
            {
                throw new UserFriendlyException(ErrorMessages.WarehouseWorkerNotFound);
            }

            response.WarehouseWorkers = new List<WarehouseWorkerResponse>
            {
            mapper.Map<WarehouseWorkerResponse>(warehouseWorker)
            };
        }

        return new ApiResponse<EmployeeResponse>
        {
            Data = response,
            IsSuccess = true,
            Message = "Employee retrieved successfully."
        };
    }

}