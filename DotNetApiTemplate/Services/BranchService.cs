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
using SupermarketERP.Models.Request;
using SupermarketERP.Models.Request.Branch;
using SupermarketERP.Models.Response;

namespace SupermarketERP.Services;

public class BranchService(
    AuthHelper authHelper,
    ApplicationDbContext db,
    IMapper mapper,
    IEmailService emailService,
    UserManager<User> userManager
) : IBranchService
{
    private string CurrentUserId => authHelper.UserId;
    private string CurrentUserRole => authHelper.UserRole;

    private bool IsAdmin => CurrentUserRole == Roles.Admin;

    #region HELPERS

    private void RequireAuthenticated()
    {
        if (string.IsNullOrWhiteSpace(CurrentUserId))
        {
            throw new UserFriendlyException(
                ErrorMessages.Unauthorized);
        }
    }

    private void RequireAdmin()
    {
        RequireAuthenticated();

        if (!IsAdmin)
        {
            throw new UserFriendlyException(
                ErrorMessages.Forbidden);
        }
    }

    private static void EnsureIdentitySuccess(
        IdentityResult result)
    {
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                string.Join("; ",
                    result.Errors.Select(e => e.Description)));
        }
    }

    private BranchResponse MapBranch(Branch branch)
    {
        var response = mapper.Map<BranchResponse>(branch);

        response.IsManager =
            !string.IsNullOrWhiteSpace(CurrentUserId) &&
            branch.BranchManager?.UserId == CurrentUserId;

        response.IsAdmin = IsAdmin;

        return response;
    }

    private async Task EnsureBranchManagerAccess(
        Branch branch)
    {
        RequireAuthenticated();

        if (IsAdmin)
            return;

        if (CurrentUserRole != Roles.BranchManager)
        {
            throw new UserFriendlyException(
                ErrorMessages.Forbidden);
        }

        if (!branch.IsActive || branch.IsDeleted)
        {
            throw new UserFriendlyException(
                ErrorMessages.Forbidden);
        }

        var isManager = await db.BranchManagers
            .AsNoTracking()
            .AnyAsync(bm =>
                bm.UserId == CurrentUserId &&
                bm.BranchId == branch.Id);

        if (!isManager)
        {
            throw new UserFriendlyException(
                ErrorMessages.Forbidden);
        }
    }

    private async Task<bool> HasWarehouseAssignment(
        string userId)
    {
        var isWarehouseWorker = await db.WarehouseWorkers
            .AnyAsync(w =>
                w.UserId == userId &&
                w.IsActive &&
                !w.IsDeleted);

        if (isWarehouseWorker)
            return true;

        return await db.WarehouseManagers
            .AnyAsync(wm => wm.UserId == userId);
    }

    private async Task EnsureUserHasRole(
        User user,
        string role)
    {
        if (await userManager.IsInRoleAsync(user, role))
            return;

        var result = await userManager.AddToRoleAsync(
            user, role);

        EnsureIdentitySuccess(result);
    }

    private async Task RemoveUserRoles(
        User user,
        params string[] roles)
    {
        var currentRoles = await userManager.GetRolesAsync(user);

        var rolesToRemove = currentRoles
            .Where(roles.Contains)
            .ToList();

        if (rolesToRemove.Count == 0)
            return;

        var result = await userManager.RemoveFromRolesAsync(
            user, rolesToRemove);

        EnsureIdentitySuccess(result);
    }

    #endregion

    #region CHANGE BRANCH STATUS

    public async Task<ApiResponse<BranchResponse>>
        ChangeBranchStatus(
            Guid branchId,
            ChangeBranchStatusRequest request)
    {
        RequireAdmin();

        var branch = await db.Branches
            .Include(b => b.BranchManager)
                .ThenInclude(bm => bm.User)
            .FirstOrDefaultAsync(b =>
                b.Id == branchId &&
                !b.IsDeleted);

        if (branch == null)
        {
            throw new UserFriendlyException(
                ErrorMessages.BranchNotFound);
        }

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

    public async Task<ApiResponse<BranchResponse>>
        AssignBranchManager(
            Guid branchId,
            AssignBranchManagerRequest request)
    {
        RequireAdmin();

        var branch = await db.Branches
            .Include(b => b.BranchManager)
                .ThenInclude(bm => bm.User)
            .FirstOrDefaultAsync(b =>
                b.Id == branchId &&
                !b.IsDeleted);

        if (branch == null)
        {
            throw new UserFriendlyException(
                ErrorMessages.BranchNotFound);
        }

        if (!branch.IsActive)
        {
            throw new UserFriendlyException(
                ErrorMessages.Forbidden);
        }

        var user = await userManager.FindByIdAsync(
            request.UserId);

        if (user == null)
        {
            throw new UserFriendlyException(
                ErrorMessages.UserNotFound);
        }

        if (await userManager.IsInRoleAsync(user, Roles.Admin))
        {
            throw new UserFriendlyException(
                ErrorMessages.Forbidden);
        }

        if (await HasWarehouseAssignment(user.Id))
        {
            throw new UserFriendlyException(
                ErrorMessages.Forbidden);
        }

        var alreadyManager = await db.BranchManagers
            .AnyAsync(bm =>
                bm.UserId == user.Id &&
                bm.BranchId != branchId);

        if (alreadyManager)
        {
            throw new UserFriendlyException(
                ErrorMessages.ManagerAlreadyAssigned);
        }

        var currentManager = branch.BranchManager;

        if (currentManager?.UserId == user.Id)
        {
            return new ApiResponse<BranchResponse>
            {
                Data = MapBranch(branch),
                IsSuccess = true,
                Message = "User is already the branch manager."
            };
        }

        var branchWorker = await db.BranchWorkers
            .FirstOrDefaultAsync(w =>
                w.UserId == user.Id &&
                !w.IsDeleted);

        if (branchWorker != null)
        {
            if (branchWorker.BranchId != branchId)
            {
                throw new UserFriendlyException(
                    ErrorMessages.Forbidden);
            }

            if (!branchWorker.IsActive)
            {
                throw new UserFriendlyException(
                    ErrorMessages.Forbidden);
            }
        }

        await using var transaction =
            await db.Database.BeginTransactionAsync();

        User? previousUser = currentManager?.User;

        if (currentManager != null)
        {
            db.BranchManagers.Remove(currentManager);

            branch.BranchManager = null;

            await db.SaveChangesAsync();
        }

        if (branchWorker != null)
        {
            branchWorker.IsActive = false;
            branchWorker.IsDeleted = true;
            branchWorker.UpdatedAt = DateTime.UtcNow;
        }

        await RemoveUserRoles(
            user,
            Roles.BranchWorker,
            Roles.Cashier,
            Roles.InventoryManager,
            Roles.User);

        await EnsureUserHasRole(
            user,
            Roles.BranchManager);

        var newManager = new BranchManager
        {
            Id = Guid.CreateVersion7(),
            UserId = user.Id,
            BranchId = branch.Id,
            AssignedAt = DateTime.UtcNow
        };

        await db.BranchManagers.AddAsync(newManager);

        branch.BranchManager = newManager;
        branch.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();

        if (previousUser != null &&
            previousUser.Id != user.Id)
        {
            var managesAnotherBranch =
                await db.BranchManagers.AnyAsync(bm =>
                    bm.UserId == previousUser.Id);

            if (!managesAnotherBranch)
            {
                await RemoveUserRoles(
                    previousUser,
                    Roles.BranchManager);

                await EnsureUserHasRole(
                    previousUser,
                    Roles.User);
            }
        }

        await transaction.CommitAsync();

        return new ApiResponse<BranchResponse>
        {
            Data = MapBranch(branch),
            IsSuccess = true,
            Message = "Branch manager assigned successfully."
        };
    }

    #endregion

    #region CREATE BRANCH

    public async Task<ApiResponse<BranchResponse>>
        CreateBranch(CreateBranchRequest request)
    {
        RequireAdmin();

        var existingBranch = await db.Branches
            .AnyAsync(b =>
                b.Name == request.Name &&
                b.City == request.City &&
                b.Address == request.Address &&
                !b.IsDeleted);

        if (existingBranch)
        {
            throw new UserFriendlyException(
                ErrorMessages.BranchAlreadyExists);
        }

        var branch = mapper.Map<Branch>(request);

        branch.Code =
            BranchHelper.GenerateBranchCode(request.Name);

        branch.CreatedAt = DateTime.UtcNow;
        branch.UpdatedAt = null;
        branch.BranchManager = null;

        await db.Branches.AddAsync(branch);
        await db.SaveChangesAsync();

        var htmlBody = $"""
            <h2>Branch Created Successfully</h2>
            <p>Branch Name: {System.Net.WebUtility.HtmlEncode(branch.Name)}</p>
            <p>Branch Code: {System.Net.WebUtility.HtmlEncode(branch.Code)}</p>
            """;

        try
        {
            if (!string.IsNullOrWhiteSpace(branch.Email))
            {
                await emailService.SendEmailAsync(
                    branch.Email,
                    "New Branch Created",
                    htmlBody);
            }
        }
        catch (Exception)
        {
            // Email failure must not undo branch creation.
            // Log this exception in production.
        }

        return new ApiResponse<BranchResponse>
        {
            Data = MapBranch(branch),
            IsSuccess = true,
            Message = "Branch created successfully."
        };
    }

    #endregion

    #region GET BRANCH BY CODE

    public async Task<ApiResponse<BranchResponse>>
        GetBranchByCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new UserFriendlyException(
                ErrorMessages.BranchCodeIsRequired);
        }

        var branch = await db.Branches
            .AsNoTracking()
            .AsSplitQuery()
            .Include(b => b.BranchManager)
                .ThenInclude(bm => bm.User)
            .Include(b => b.Workers)
            .FirstOrDefaultAsync(b =>
                b.Code == code &&
                !b.IsDeleted);

        if (branch == null)
        {
            throw new UserFriendlyException(
                ErrorMessages.BranchNotFound);
        }

        return new ApiResponse<BranchResponse>
        {
            Data = MapBranch(branch),
            IsSuccess = true,
            Message = "Branch found."
        };
    }

    #endregion

    #region GET BRANCH BY ID

    public async Task<ApiResponse<BranchResponse>>
        GetBranchById(Guid id)
    {
        var branch = await db.Branches
            .AsNoTracking()
            .AsSplitQuery()
            .Include(b => b.BranchManager)
                .ThenInclude(bm => bm.User)
            .Include(b => b.Workers)
            .FirstOrDefaultAsync(b =>
                b.Id == id &&
                !b.IsDeleted);

        if (branch == null)
        {
            throw new UserFriendlyException(
                ErrorMessages.BranchNotFound);
        }

        return new ApiResponse<BranchResponse>
        {
            Data = MapBranch(branch),
            IsSuccess = true,
            Message = "Branch found."
        };
    }

    #endregion

    #region GET BRANCHES

    public async Task<PagedApiResponse<BranchResponse>>
        GetBranches(
            string? city,
            string? searchTerm,
            int pageNumber = 1,
            int pageSize = 10)
    {
        if (pageNumber <= 0 ||
            pageSize <= 0 ||
            pageSize > 100)
        {
            throw new UserFriendlyException(
                ErrorMessages.InvalidPagination);
        }

        var query = db.Branches
            .AsNoTracking()
            .AsSplitQuery()
            .Include(b => b.BranchManager)
                .ThenInclude(bm => bm.User)
            .Include(b => b.Workers)
            .Where(b =>
                b.IsActive &&
                !b.IsDeleted);

        if (!string.IsNullOrWhiteSpace(city))
        {
            query = query.Where(b => b.City == city);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(b =>
                b.Name.Contains(searchTerm) ||
                b.Address.Contains(searchTerm) ||
                b.Code.Contains(searchTerm));
        }

        var totalCount = await query.CountAsync();

        var branches = await query
            .OrderBy(b => b.CreatedAt)
            .ThenBy(b => b.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var responses = branches
            .Select(MapBranch)
            .ToList();

        return new PagedApiResponse<BranchResponse>(
            responses,
            totalCount,
            pageSize,
            pageNumber);
    }

    #endregion

    #region UPDATE BRANCH

    public async Task<ApiResponse<BranchResponse>>
        UpdateBranch(
            Guid branchId,
            UpdateBranchRequest request)
    {
        RequireAuthenticated();

        var branch = await db.Branches
            .Include(b => b.BranchManager)
                .ThenInclude(bm => bm.User)
            .FirstOrDefaultAsync(b =>
                b.Id == branchId &&
                !b.IsDeleted);

        if (branch == null)
        {
            throw new UserFriendlyException(
                ErrorMessages.BranchNotFound);
        }

        await EnsureBranchManagerAccess(branch);

        var duplicate = await db.Branches
            .AnyAsync(b =>
                b.Id != branchId &&
                b.Name == request.Name &&
                b.City == request.City &&
                b.Address == request.Address &&
                !b.IsDeleted);

        if (duplicate)
        {
            throw new UserFriendlyException(
                ErrorMessages.BranchAlreadyExists);
        }

        mapper.Map(request, branch);

        branch.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();

        return new ApiResponse<BranchResponse>
        {
            Data = MapBranch(branch),
            IsSuccess = true,
            Message = "Branch updated successfully."
        };
    }

    #endregion

    #region GET ASSIGNED BRANCH WORKERS

    public async Task<PagedApiResponse<BranchWorkerResponse>>
        GetAssignedBranchWorkers(
            Guid branchId,
            int pageNumber = 1,
            int pageSize = 10)
    {
        RequireAuthenticated();

        if (pageNumber <= 0 ||
            pageSize <= 0 ||
            pageSize > 100)
        {
            throw new UserFriendlyException(
                ErrorMessages.InvalidPagination);
        }

        var branch = await db.Branches
            .AsNoTracking()
            .FirstOrDefaultAsync(b =>
                b.Id == branchId &&
                !b.IsDeleted);

        if (branch == null)
        {
            throw new UserFriendlyException(
                ErrorMessages.BranchNotFound);
        }

        await EnsureBranchManagerAccess(branch);

        var query = db.BranchWorkers
            .AsNoTracking()
            .Where(w =>
                w.BranchId == branchId &&
                !w.IsDeleted);

        var totalCount = await query.CountAsync();

        var workers = await query
            .OrderBy(w => w.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var responses =
            mapper.Map<List<BranchWorkerResponse>>(workers);

        return new PagedApiResponse<BranchWorkerResponse>(
            responses,
            totalCount,
            pageSize,
            pageNumber);
    }

    #endregion

    #region ASSIGN BRANCH WORKER

    public async Task<ApiResponse<BranchWorkerResponse>>
        AssignBranchWorkerToBranch(
            Guid branchId,
            AssignBranchWorkerRequest request)
    {
        RequireAuthenticated();

        var branch = await db.Branches
            .FirstOrDefaultAsync(b =>
                b.Id == branchId &&
                b.IsActive &&
                !b.IsDeleted);

        if (branch == null)
        {
            throw new UserFriendlyException(
                ErrorMessages.BranchNotFound);
        }

        await EnsureBranchManagerAccess(branch);

        var user = await userManager.FindByIdAsync(
            request.UserId);

        if (user == null)
        {
            throw new UserFriendlyException(
                ErrorMessages.UserNotFound);
        }

        if (await userManager.IsInRoleAsync(user, Roles.Admin))
        {
            throw new UserFriendlyException(
                ErrorMessages.Forbidden);
        }

        var isBranchManager = await db.BranchManagers
            .AnyAsync(bm => bm.UserId == user.Id);

        if (isBranchManager)
        {
            throw new UserFriendlyException(
                ErrorMessages.Forbidden);
        }

        if (await HasWarehouseAssignment(user.Id))
        {
            throw new UserFriendlyException(
                ErrorMessages.Forbidden);
        }

        var branchWorker = await db.BranchWorkers
            .FirstOrDefaultAsync(w =>
                w.UserId == user.Id);

        if (branchWorker != null && !branchWorker.IsDeleted)
        {
            throw new UserFriendlyException(
                ErrorMessages.EmployeeAlreadyAssigned);
        }

        await using var transaction =
            await db.Database.BeginTransactionAsync();

        if (branchWorker == null)
        {
            branchWorker = new BranchWorker
            {
                Id = Guid.CreateVersion7(),
                UserId = user.Id,
                FirstName = user.UserName
                    ?? user.Email
                    ?? string.Empty,
                LastName = string.Empty,
                BranchId = branch.Id,
                HireDate = DateTime.UtcNow,
                IsActive = true,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow
            };

            await db.BranchWorkers.AddAsync(branchWorker);
        }
        else
        {
            branchWorker.BranchId = branch.Id;
            branchWorker.IsActive = true;
            branchWorker.IsDeleted = false;
            branchWorker.HireDate = DateTime.UtcNow;
            branchWorker.UpdatedAt = DateTime.UtcNow;
        }

        await RemoveUserRoles(user, Roles.User);

        await EnsureUserHasRole(
            user,
            Roles.BranchWorker);

        branch.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        await transaction.CommitAsync();

        return new ApiResponse<BranchWorkerResponse>
        {
            Data = mapper.Map<BranchWorkerResponse>(
                branchWorker),
            IsSuccess = true,
            Message = "Branch worker assigned successfully."
        };
    }

    #endregion

    #region UNASSIGN BRANCH WORKER

    public async Task<ApiResponse<BranchWorkerResponse>>
        UnassignBranchWorkerFromBranch(
            Guid branchId,
            UnassignBranchWorkerRequest request)
    {
        RequireAdmin();

        var branch = await db.Branches
            .FirstOrDefaultAsync(b =>
                b.Id == branchId &&
                !b.IsDeleted);

        if (branch == null)
        {
            throw new UserFriendlyException(
                ErrorMessages.BranchNotFound);
        }

        var user = await userManager.FindByIdAsync(
            request.UserId);

        if (user == null)
        {
            throw new UserFriendlyException(
                ErrorMessages.UserNotFound);
        }

        var branchWorker = await db.BranchWorkers
            .FirstOrDefaultAsync(w =>
                w.UserId == user.Id &&
                w.BranchId == branchId &&
                !w.IsDeleted);

        if (branchWorker == null)
        {
            throw new UserFriendlyException(
                ErrorMessages.EmployeeNotFound);
        }

        var isBranchManager = await db.BranchManagers
            .AnyAsync(bm => bm.UserId == user.Id);

        if (isBranchManager)
        {
            throw new UserFriendlyException(
                ErrorMessages.Forbidden);
        }

        await using var transaction =
            await db.Database.BeginTransactionAsync();

        branchWorker.IsActive = false;
        branchWorker.IsDeleted = true;
        branchWorker.UpdatedAt = DateTime.UtcNow;

        branch.UpdatedAt = DateTime.UtcNow;

        await RemoveUserRoles(
            user,
            Roles.BranchWorker,
            Roles.Cashier,
            Roles.InventoryManager);

        await EnsureUserHasRole(
            user,
            Roles.User);

        await db.SaveChangesAsync();
        await transaction.CommitAsync();

        return new ApiResponse<BranchWorkerResponse>
        {
            Data = mapper.Map<BranchWorkerResponse>(
                branchWorker),
            IsSuccess = true,
            Message = "Branch worker unassigned successfully."
        };
    }

    #endregion

    #region STATISTICS

    public Task<PagedApiResponse<BranchResponse>>
        GetStatistics(
            Guid branchId,
            int pageNumber = 1,
            int pageSize = 10)
    {
        throw new NotImplementedException();
    }

    #endregion
}