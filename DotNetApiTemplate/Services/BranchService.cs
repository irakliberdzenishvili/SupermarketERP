
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
    private string? CurrentUserId => authHelper.UserId;
    private string CurrentUserRole => authHelper.UserRole;

    private string GetRequiredUserId()
    {
        if (string.IsNullOrWhiteSpace(CurrentUserId))
            throw new UserFriendlyException(
                ErrorMessages.Unauthorized);

        return CurrentUserId;
    }

    private void EnsureAdmin()
    {
        GetRequiredUserId();

        if (CurrentUserRole != Roles.Admin)
            throw new UserFriendlyException(
                ErrorMessages.Forbidden);
    }

    private static void EnsureIdentitySucceeded(
        IdentityResult result)
    {
        if (result.Succeeded)
            return;

        throw new InvalidOperationException(
            string.Join("; ",
                result.Errors.Select(e => e.Description)));
    }

    #region BRANCH MAPPING

    private BranchResponse MapBranch(Branch branch)
    {
        var response = mapper.Map<BranchResponse>(branch);

        response.IsManager =
            CurrentUserRole == Roles.BranchManager &&
            !string.IsNullOrWhiteSpace(CurrentUserId) &&
            branch.BranchManagerId == CurrentUserId;

        return response;
    }

    #endregion

    #region BRANCH STATUS

    public async Task<ApiResponse<BranchResponse>> ChangeBranchStatus(
        Guid branchId,
        ActivateBranchRequest request)
    {
        EnsureAdmin();

        var branch = await db.Branches
            .FirstOrDefaultAsync(b => b.Id == branchId);

        if (branch == null)
            throw new UserFriendlyException(
                ErrorMessages.BranchNotFound);

        branch.IsActive = request.IsActive;
        branch.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();

        return new ApiResponse<BranchResponse>
        {
            Data = MapBranch(branch),
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
        EnsureAdmin();

        var branch = await db.Branches
            .FirstOrDefaultAsync(b => b.Id == branchId);

        if (branch == null)
            throw new UserFriendlyException(
                ErrorMessages.BranchNotFound);

        var user = await userManager.FindByIdAsync(request.UserId);

        if (user == null)
            throw new UserFriendlyException(
                ErrorMessages.UserNotFound);

        if (await userManager.IsInRoleAsync(user, Roles.Admin))
            throw new UserFriendlyException(
                ErrorMessages.Forbidden);

        var employee = await db.Employees
            .FirstOrDefaultAsync(e => e.UserId == user.Id);

        if (employee == null)
            throw new InvalidOperationException(
                "The selected user does not have an employee record.");

        if (!employee.IsActive)
            throw new UserFriendlyException(
                ErrorMessages.Forbidden);

        if (employee.BranchId != Guid.Empty &&
            employee.BranchId != branchId)
        {
            throw new InvalidOperationException(
                "Employee belongs to another branch.");
        }

        var alreadyAssigned = await db.Branches
            .AnyAsync(b =>
                b.BranchManagerId == user.Id &&
                b.Id != branchId);

        if (alreadyAssigned)
            throw new UserFriendlyException(
                ErrorMessages.ManagerAlreadyAssigned);

        var previousManagerId = branch.BranchManagerId;

        await using var transaction =
            await db.Database.BeginTransactionAsync();

        var currentRoles = await userManager.GetRolesAsync(user);

        if (currentRoles.Count != 1 ||
            currentRoles[0] != Roles.BranchManager)
        {
            if (currentRoles.Count > 0)
            {
                var removeResult = await userManager
                    .RemoveFromRolesAsync(user, currentRoles);

                EnsureIdentitySucceeded(removeResult);
            }

            var addResult = await userManager
                .AddToRoleAsync(user, Roles.BranchManager);

            EnsureIdentitySucceeded(addResult);
        }

        employee.BranchId = branch.Id;
        employee.UpdatedAt = DateTime.UtcNow;

        branch.BranchManagerId = user.Id;
        branch.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();

        if (!string.IsNullOrWhiteSpace(previousManagerId) &&
            previousManagerId != user.Id)
        {
            var previousManager = await userManager
                .FindByIdAsync(previousManagerId);

            if (previousManager != null &&
                await userManager.IsInRoleAsync(
                    previousManager,
                    Roles.BranchManager))
            {
                var removeResult = await userManager
                    .RemoveFromRoleAsync(
                        previousManager,
                        Roles.BranchManager);

                EnsureIdentitySucceeded(removeResult);
            }
        }

        await transaction.CommitAsync();

        await db.Entry(branch)
        .Reference(b => b.BranchManager)
        .LoadAsync();

        return new ApiResponse<BranchResponse>
        {
            Data = MapBranch(branch),
            IsSuccess = true,
            Message = "Manager assigned successfully."
        };
    }

    #endregion

    #region CREATE BRANCH

    public async Task<ApiResponse<BranchResponse>> CreateBranch(
        CreateBranchRequest request)
    {
        EnsureAdmin();

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
            // Email failure should not undo branch creation.
            // Log the exception in production.
        }

        return new ApiResponse<BranchResponse>
        {
            Data = MapBranch(newBranch),
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
            .Include(b => b.Employees)
            .Include(b => b.BranchManager)
            .FirstOrDefaultAsync(b => b.Code == code);

        if (branch == null)
            throw new UserFriendlyException(
                ErrorMessages.BranchNotFound);

        return new ApiResponse<BranchResponse>
        {
            Data = MapBranch(branch),
            IsSuccess = true,
            Message = "Branch Found"
        };
    }

    #endregion

    #region GET BRANCH BY ID

    public async Task<ApiResponse<BranchResponse>> GetBranchById(
        Guid id)
    {
        var branch = await db.Branches
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == id);

        if (branch == null)
            throw new UserFriendlyException(
                ErrorMessages.BranchNotFound);

        return new ApiResponse<BranchResponse>
        {
            Data = MapBranch(branch),
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
        {
            throw new UserFriendlyException(
                ErrorMessages.InvalidPagination);
        }

        var branches = db.Branches
            .AsNoTracking()
            .Include(b => b.BranchManager)
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

        var mappedBranches = pagedBranches
            .Select(MapBranch)
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
        var currentUserId = GetRequiredUserId();

        var branch = await db.Branches
            .FirstOrDefaultAsync(b => b.Id == branchId);

        if (branch == null)
            throw new UserFriendlyException(
                ErrorMessages.BranchNotFound);

        var isAdmin = CurrentUserRole == Roles.Admin;

        var isBranchManager =
            CurrentUserRole == Roles.BranchManager &&
            branch.BranchManagerId == currentUserId;

        if (!isAdmin && !isBranchManager)
            throw new UserFriendlyException(
                ErrorMessages.Forbidden);

        mapper.Map(request, branch);
        branch.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();

        return new ApiResponse<BranchResponse>
        {
            Data = MapBranch(branch),
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
        var currentUserId = GetRequiredUserId();
        var currentUserRole = CurrentUserRole;

        if (pageNumber <= 0 || pageSize <= 0)
        {
            throw new UserFriendlyException(
                ErrorMessages.InvalidPagination);
        }

        var isAdmin = currentUserRole == Roles.Admin;
        var isBranchManager =
            currentUserRole == Roles.BranchManager;

        if (!isAdmin && !isBranchManager)
        {
            throw new UserFriendlyException(
                ErrorMessages.Forbidden);
        }

        var branch = await db.Branches
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == branchId);

        if (branch == null)
        {
            throw new UserFriendlyException(
                ErrorMessages.BranchNotFound);
        }

        if (isBranchManager &&
            branch.BranchManagerId != currentUserId)
        {
            throw new UserFriendlyException(
                ErrorMessages.Forbidden);
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
}
