
using AutoMapper;
using DotNetApiTemplate.DB;
using DotNetApiTemplate.Exceptions;
using DotNetApiTemplate.Helpers;
using DotNetApiTemplate.Interfaces;
using DotNetApiTemplate.Models.Data;
using DotNetApiTemplate.Models.Response;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SupermarketERP.Constants;
using SupermarketERP.Helpers;
using SupermarketERP.Interfaces;
using SupermarketERP.Models.Data;
using SupermarketERP.Models.Data.Branch;
using SupermarketERP.Models.Request.Branch;
using SupermarketERP.Models.Request.Employee;
using SupermarketERP.Models.Response.EmployeeResponse;

namespace SupermarketERP.Services;

public class BranchService(
    AuthHelper authHelper,
    ApplicationDbContext db,
    IMapper mapper,
    IEmailService emailService,
    UserManager<User> userManager
) : IBranchService
{
    private BranchResponse MapBranch(
     Branch branch,
     Guid? currentEmployeeId)
    {
        var response = mapper.Map<BranchResponse>(branch);

        response.IsManager =
            currentEmployeeId.HasValue &&
            branch.BranchManagerId == currentEmployeeId.Value;

        return response;
    }

    #region CHANGE BRANCH STATUS

    public async Task<ApiResponse<BranchResponse>> ChangeBranchStatus(
        Guid branchId,
        ActivateBranchRequest request)
    {
        var currentUserId = authHelper.UserId;
        var currentUserRole = authHelper.UserRole;

        if (string.IsNullOrWhiteSpace(currentUserId))
            throw new UserFriendlyException(ErrorMessages.Unauthorized);

        if (currentUserRole != Roles.Admin)
            throw new UserFriendlyException(ErrorMessages.Forbidden);

        var branch = await db.Branches
            .Include(b => b.BranchManager)
            .FirstOrDefaultAsync(b => b.Id == branchId);

        if (branch == null)
            throw new UserFriendlyException(ErrorMessages.BranchNotFound);

        branch.IsActive = request.IsActive;
        branch.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();

        return new ApiResponse<BranchResponse>
        {
            Data = MapBranch(branch, null),
            IsSuccess = true,
            Message = branch.IsActive
                ? "Branch activated successfully."
                : "Branch deactivated successfully."
        };
    }

    #endregion

    #region ASSIGN BRANCH MANAGER

    public async Task<ApiResponse<BranchResponse>> AssignBranchManager(
    Guid branchId,
    AssignBranchManagerRequest request)
    {
        var currentUserId = authHelper.UserId;
        var currentUserRole = authHelper.UserRole;

        if (string.IsNullOrWhiteSpace(currentUserId))
            throw new UserFriendlyException(ErrorMessages.Unauthorized);

        if (currentUserRole != Roles.Admin)
            throw new UserFriendlyException(ErrorMessages.Forbidden);

        var branch = await db.Branches
            .FirstOrDefaultAsync(b => b.Id == branchId);

        if (branch == null)
            throw new UserFriendlyException(ErrorMessages.BranchNotFound);

        var user = await userManager.FindByIdAsync(request.UserId);

        if (user == null)
            throw new UserFriendlyException(ErrorMessages.UserNotFound);

        if (await userManager.IsInRoleAsync(user, Roles.Admin))
            throw new UserFriendlyException(ErrorMessages.Forbidden);

        var employee = await db.Employees
            .FirstOrDefaultAsync(e => e.UserId == user.Id);

        if (employee == null)
            throw new InvalidOperationException(
                "The selected user does not have an employee record.");

        if (!employee.IsActive)
            throw new UserFriendlyException(ErrorMessages.Forbidden);

        if (employee.BranchId != Guid.Empty &&
            employee.BranchId != branchId)
        {
            throw new InvalidOperationException(
                "Employee belongs to another branch.");
        }

        var alreadyAssigned = await db.Branches
            .AnyAsync(b =>
                b.BranchManagerId == employee.Id &&
                b.Id != branchId);

        if (alreadyAssigned)
            throw new UserFriendlyException(
                ErrorMessages.ManagerAlreadyAssigned);

        var previousManagerId = branch.BranchManagerId;

        await using var transaction =
            await db.Database.BeginTransactionAsync();

        var currentRoles = await userManager.GetRolesAsync(user);

        var rolesToRemove = currentRoles
            .Where(r => r != Roles.BranchManager)
            .ToList();

        if (rolesToRemove.Count > 0)
        {
            var removeResult = await userManager.RemoveFromRolesAsync(
                user,
                rolesToRemove);

            if (!removeResult.Succeeded)
            {
                throw new InvalidOperationException(
                    string.Join("; ",
                        removeResult.Errors.Select(e => e.Description)));
            }
        }

        if (!currentRoles.Contains(Roles.BranchManager))
        {
            var addResult = await userManager.AddToRoleAsync(
                user,
                Roles.BranchManager);

            if (!addResult.Succeeded)
            {
                throw new InvalidOperationException(
                    string.Join("; ",
                        addResult.Errors.Select(e => e.Description)));
            }
        }

        employee.BranchId = branch.Id;
        employee.UpdatedAt = DateTime.UtcNow;

        branch.BranchManagerId = employee.Id;
        branch.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();

        if (previousManagerId.HasValue &&
            previousManagerId.Value != employee.Id)
        {
            var previousManager = await db.Employees
                .Include(e => e.User)
                .FirstOrDefaultAsync(
                    e => e.Id == previousManagerId.Value);

            if (previousManager?.User != null)
            {
                var previousUser = previousManager.User;

                var managesAnotherBranch = await db.Branches
                    .AnyAsync(b =>
                        b.BranchManagerId == previousManager.Id);

                if (!managesAnotherBranch &&
                    await userManager.IsInRoleAsync(
                        previousUser,
                        Roles.BranchManager))
                {
                    var result = await userManager.RemoveFromRoleAsync(
                        previousUser,
                        Roles.BranchManager);

                    if (!result.Succeeded)
                    {
                        throw new InvalidOperationException(
                            string.Join("; ",
                                result.Errors.Select(e => e.Description)));
                    }

                    // Restore the previous manager to the employee role.
                    if (!await userManager.IsInRoleAsync(
                        previousUser,
                        Roles.Employee))
                    {
                        var employeeRoleResult =
                            await userManager.AddToRoleAsync(
                                previousUser,
                                Roles.Employee);

                        if (!employeeRoleResult.Succeeded)
                        {
                            throw new InvalidOperationException(
                                string.Join("; ",
                                    employeeRoleResult.Errors
                                        .Select(e => e.Description)));
                        }
                    }
                }
            }
        }

        await transaction.CommitAsync();

        await db.Entry(branch)
            .Reference(b => b.BranchManager)
            .LoadAsync();

        return new ApiResponse<BranchResponse>
        {
            Data = MapBranch(branch, null),
            IsSuccess = true,
            Message = "Manager assigned successfully."
        };
    }

    #endregion

    #region CREATE BRANCH

    public async Task<ApiResponse<BranchResponse>> CreateBranch(
        CreateBranchRequest request)
    {
        var currentUserId = authHelper.UserId;
        var currentUserRole = authHelper.UserRole;

        if (string.IsNullOrWhiteSpace(currentUserId))
            throw new UserFriendlyException(ErrorMessages.Unauthorized);

        if (currentUserRole != Roles.Admin)
            throw new UserFriendlyException(ErrorMessages.Forbidden);

        var existingBranch = await db.Branches
            .AnyAsync(b =>
                b.Name == request.Name &&
                b.City == request.City &&
                b.Address == request.Address);

        if (existingBranch)
            throw new UserFriendlyException(
                ErrorMessages.BranchAlreadyExists);

        var newBranch = mapper.Map<Branch>(request);

        newBranch.Code =
            BranchHelper.GenerateBranchCode(request.Name);

        newBranch.CreatedAt = DateTime.UtcNow;
        newBranch.UpdatedAt = null;
        newBranch.BranchManagerId = null;

        await db.Branches.AddAsync(newBranch);
        await db.SaveChangesAsync();

        var htmlBody = $"""
            <h2>Branch Created Successfully</h2>
            <p>Branch Name: {System.Net.WebUtility.HtmlEncode(newBranch.Name)}</p>
            <p>Branch Code: {System.Net.WebUtility.HtmlEncode(newBranch.Code)}</p>
            """;

        try
        {
            if (!string.IsNullOrWhiteSpace(newBranch.Email))
            {
                await emailService.SendEmailAsync(
                    newBranch.Email,
                    "New Branch Created",
                    htmlBody);
            }
        }
        catch (Exception)
        {
            // Email failure does not undo branch creation.
            // Log the exception in production.
        }

        return new ApiResponse<BranchResponse>
        {
            Data = MapBranch(newBranch, null),
            IsSuccess = true,
            Message = "Branch Created"
        };
    }

    #endregion

    #region GET BRANCH BY CODE

    public async Task<ApiResponse<BranchResponse>> GetBranchByCode(
        string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new UserFriendlyException(
                ErrorMessages.BranchCodeIsRequired);

        var branch = await db.Branches
            .AsNoTracking()
            .Include(b => b.BranchManager)
            .FirstOrDefaultAsync(b => b.Code == code);

        if (branch == null)
            throw new UserFriendlyException(ErrorMessages.BranchNotFound);

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

        return new ApiResponse<BranchResponse>
        {
            Data = MapBranch(branch, currentEmployeeId),
            IsSuccess = true,
            Message = "Branch Found"
        };
    }

    #endregion

    #region GET BRANCH BY ID

    public async Task<ApiResponse<BranchResponse>> GetBranchById(Guid id)
    {
        var branch = await db.Branches
            .AsNoTracking()
            .Include(b => b.BranchManager)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (branch == null)
            throw new UserFriendlyException(ErrorMessages.BranchNotFound);

        var currentUserId = authHelper.UserId;

        Guid? currentEmployeeId = null;

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

        var response = MapBranch(branch, currentEmployeeId);

        return new ApiResponse<BranchResponse>
        {
            Data = response,
            IsSuccess = true,
            Message = "Branch Found"
        };
    }

    #endregion

    #region GET BRANCHES

    public async Task<PagedApiResponse<BranchResponse>> GetBranches(
        string? city,
        string? searchTerm,
        int pageNumber = 1,
        int pageSize = 10)
    {
        if (pageNumber <= 0 || pageSize <= 0)
            throw new UserFriendlyException(
                ErrorMessages.InvalidPagination);

        var branches = db.Branches
            .AsNoTracking()
            .Include(b => b.BranchManager)
                .ThenInclude(u => u.User)
            .Include(e => e.Employees)
            .Where(b => b.IsActive && !b.IsDeleted);

        if (!string.IsNullOrWhiteSpace(city))
        {
            branches = branches.Where(b => b.City == city);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            branches = branches.Where(b =>
                b.Name.Contains(searchTerm) ||
                b.Address.Contains(searchTerm) ||
                b.Code.Contains(searchTerm));
        }

        var totalCount = await branches.CountAsync();

        var pagedBranches = await branches
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

        var mappedBranches = pagedBranches
            .Select(branch => MapBranch(branch, currentEmployeeId))
            .ToList();

        return new PagedApiResponse<BranchResponse>(
            mappedBranches,
            totalCount,
            pageSize,
            pageNumber);
    }

    #endregion

    #region UPDATE BRANCH

    public async Task<ApiResponse<BranchResponse>> UpdateBranch(
        Guid branchId,
        UpdateBranchRequest request)
    {
        var currentUserId = authHelper.UserId;
        var currentUserRole = authHelper.UserRole;

        if (string.IsNullOrWhiteSpace(currentUserId))
            throw new UserFriendlyException(ErrorMessages.Unauthorized);

        var branch = await db.Branches
            .Include(b => b.BranchManager)
            .FirstOrDefaultAsync(b => b.Id == branchId);

        if (branch == null)
            throw new UserFriendlyException(ErrorMessages.BranchNotFound);

        var isAdmin = currentUserRole == Roles.Admin;
        Guid? currentEmployeeId = null;

        if (!isAdmin)
        {
            if (currentUserRole != Roles.BranchManager)
                throw new UserFriendlyException(
                    ErrorMessages.Forbidden);

            currentEmployeeId = await db.Employees
                .AsNoTracking()
                .Where(e =>
                    e.UserId == currentUserId &&
                    e.IsActive)
                .Select(e => (Guid?)e.Id)
                .FirstOrDefaultAsync();

            if (!currentEmployeeId.HasValue ||
                branch.BranchManagerId != currentEmployeeId.Value)
            {
                throw new UserFriendlyException(
                    ErrorMessages.Forbidden);
            }
        }

        mapper.Map(request, branch);

        branch.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();

        return new ApiResponse<BranchResponse>
        {
            Data = MapBranch(branch, currentEmployeeId),
            IsSuccess = true,
            Message = "Branch Updated"
        };
    }

    #endregion

    #region GET ASSIGNED EMPLOYEES

    public async Task<PagedApiResponse<EmployeeResponse>> GetAssignedEmployees(
        Guid branchId,
        int pageNumber = 1,
        int pageSize = 10)
    {
        var currentUserId = authHelper.UserId;
        var currentUserRole = authHelper.UserRole;

        if (string.IsNullOrWhiteSpace(currentUserId))
            throw new UserFriendlyException(ErrorMessages.Unauthorized);

        if (pageNumber <= 0 || pageSize <= 0)
            throw new UserFriendlyException(
                ErrorMessages.InvalidPagination);

        var isAdmin = currentUserRole == Roles.Admin;
        var isBranchManager =
            currentUserRole == Roles.BranchManager;

        if (!isAdmin && !isBranchManager)
            throw new UserFriendlyException(
                ErrorMessages.Forbidden);

        var branch = await db.Branches
            .AsNoTracking()
            .Include(b => b.BranchManager)
            .FirstOrDefaultAsync(b => b.Id == branchId);

        if (branch == null)
            throw new UserFriendlyException(
                ErrorMessages.BranchNotFound);

        if (isBranchManager)
        {
            var employeeId = await db.Employees
                .AsNoTracking()
                .Where(e =>
                    e.UserId == currentUserId &&
                    e.IsActive)
                .Select(e => (Guid?)e.Id)
                .FirstOrDefaultAsync();

            if (!employeeId.HasValue ||
                branch.BranchManagerId != employeeId.Value)
            {
                throw new UserFriendlyException(
                    ErrorMessages.Forbidden);
            }
        }

        var query = db.Employees
            .AsNoTracking()
            .Where(e => e.BranchId == branchId);

        var totalCount = await query.CountAsync();

        var employees = await query
            .OrderBy(e => e.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var mappedResponse =
            mapper.Map<List<EmployeeResponse>>(employees);

        return new PagedApiResponse<EmployeeResponse>(
            mappedResponse,
            totalCount,
            pageSize,
            pageNumber);
    }



    #endregion

    public async Task<ApiResponse<EmployeeResponse>> AssignEmployeeToBranch(
    Guid branchId,
    AssignEmployeeRequest request)
    {
        var currentUserId = authHelper.UserId;
        var currentUserRole = authHelper.UserRole;

        var isAdmin = currentUserRole == Roles.Admin;
        var isBranchManager = currentUserRole == Roles.BranchManager;

        if (!isAdmin && !isBranchManager)
        {
            throw new UserFriendlyException(ErrorMessages.Forbidden);
        }

        var branch = await db.Branches
            .FirstOrDefaultAsync(b => b.Id == branchId && b.IsActive);

        if (branch == null)
        {
            throw new UserFriendlyException(ErrorMessages.BranchNotFound);
        }

        if (isBranchManager && !isAdmin)
        {
            var manager = await db.Employees
                .FirstOrDefaultAsync(e => e.UserId == currentUserId);

            if (manager == null || branch.BranchManagerId != manager.Id)
            {
                throw new UserFriendlyException(ErrorMessages.Forbidden);
            }
        }

        var user = await db.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId);

        if (user == null)
        {
            throw new UserFriendlyException(ErrorMessages.UserNotFound);
        }

        var alreadyAssigned = await db.Employees
            .AnyAsync(e => e.UserId == user.Id);

        if (alreadyAssigned)
        {
            throw new UserFriendlyException(
                ErrorMessages.EmployeeAlreadyAssigned);
        }

        var currentRoles = await userManager.GetRolesAsync(user);

        var employee = new Employee
        {
            FirstName = user.Email,
            LastName = user.Email,
            BranchId = branch.Id,
            UserId = user.Id,
            UpdatedAt = null
        };

        await using var transaction = await db.Database.BeginTransactionAsync();

        if (!currentRoles.Contains(Roles.Employee))
        {
            var result = await userManager.AddToRoleAsync(
                user,
                Roles.Employee);

            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    string.Join("; ",
                        result.Errors.Select(e => e.Description)));
            }
        }

        await db.Employees.AddAsync(employee);
        await db.SaveChangesAsync();

        await transaction.CommitAsync();

        var mappedResponse = mapper.Map<EmployeeResponse>(employee);

        
        return new ApiResponse<EmployeeResponse>
        {
            Data = mappedResponse,
            IsSuccess = true,
            Message = "Employee Assigned"
        };
    }
}
