
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

    private void RequireAuthenticated()
    {
        if (string.IsNullOrWhiteSpace(CurrentUserId))
            throw new UserFriendlyException(ErrorMessages.Unauthorized);
    }

    private void RequireAdmin()
    {
        RequireAuthenticated();

        if (!IsAdmin)
            throw new UserFriendlyException(ErrorMessages.Forbidden);
    }

    private async Task<Guid?> GetCurrentBranchWorkerId()
    {
        if (string.IsNullOrWhiteSpace(CurrentUserId))
            return null;

        return await db.BranchWorkers
            .AsNoTracking()
            .Where(w =>
                w.UserId == CurrentUserId &&
                w.IsActive &&
                !w.IsDeleted)
            .Select(w => (Guid?)w.Id)
            .FirstOrDefaultAsync();
    }

    private BranchResponse MapBranch(
        Branch branch,
        Guid? currentWorkerId)
    {
        var response = mapper.Map<BranchResponse>(branch);

        response.IsManager =
            currentWorkerId.HasValue &&
            branch.BranchManagerId == currentWorkerId.Value;

        response.IsAdmin = IsAdmin;

        return response;
    }

    private static void EnsureIdentitySuccess(IdentityResult result)
    {
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                string.Join("; ",
                    result.Errors.Select(e => e.Description)));
        }
    }

    private async Task EnsureBranchManagerAccess(Branch branch)
    {
        RequireAuthenticated();

        if (IsAdmin)
            return;

        if (CurrentUserRole != Roles.BranchManager)
            throw new UserFriendlyException(ErrorMessages.Forbidden);

        var workerId = await GetCurrentBranchWorkerId();

        if (!workerId.HasValue ||
            branch.BranchManagerId != workerId.Value)
        {
            throw new UserFriendlyException(ErrorMessages.Forbidden);
        }
    }

    #region CHANGE BRANCH STATUS

    public async Task<ApiResponse<BranchResponse>> ChangeBranchStatus(
        Guid branchId,
        ChangeBranchStatusRequest request)
    {
        RequireAdmin();

        var branch = await db.Branches
            .Include(b => b.BranchManager)
            .FirstOrDefaultAsync(b =>
                b.Id == branchId &&
                !b.IsDeleted);

        if (branch == null)
            throw new UserFriendlyException(
                ErrorMessages.BranchNotFound);

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
        RequireAdmin();

        var branch = await db.Branches
            .FirstOrDefaultAsync(b =>
                b.Id == branchId &&
                !b.IsDeleted);

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

        var branchWorker = await db.BranchWorkers
            .FirstOrDefaultAsync(w =>
                w.UserId == user.Id &&
                !w.IsDeleted);

        if (branchWorker == null)
            throw new UserFriendlyException(
                ErrorMessages.EmployeeNotFound);

        if (!branchWorker.IsActive)
            throw new UserFriendlyException(
                ErrorMessages.Forbidden);

        if (branchWorker.BranchId.HasValue &&
            branchWorker.BranchId.Value != branchId)
        {
            throw new InvalidOperationException(
                "Branch worker belongs to another branch.");
        }

        var alreadyManager = await db.Branches
            .AnyAsync(b =>
                b.BranchManagerId == branchWorker.Id &&
                b.Id != branchId &&
                !b.IsDeleted);

        if (alreadyManager)
            throw new UserFriendlyException(
                ErrorMessages.ManagerAlreadyAssigned);

        var previousManagerId = branch.BranchManagerId;

        await using var transaction =
            await db.Database.BeginTransactionAsync();

        var currentRoles = await userManager.GetRolesAsync(user);

        var conflictingRoles = currentRoles
            .Where(r =>
                r == Roles.BranchWorker ||
                r == Roles.Cashier ||
                r == Roles.InventoryManager)
            .ToList();

        if (conflictingRoles.Count > 0)
        {
            var result = await userManager.RemoveFromRolesAsync(
                user, conflictingRoles);

            EnsureIdentitySuccess(result);
        }

        if (!currentRoles.Contains(Roles.BranchManager))
        {
            var result = await userManager.AddToRoleAsync(
                user, Roles.BranchManager);

            EnsureIdentitySuccess(result);
        }

        branchWorker.BranchId = branch.Id;
        branchWorker.UpdatedAt = DateTime.UtcNow;

        branch.BranchManagerId = branchWorker.Id;
        branch.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();

        if (previousManagerId.HasValue &&
            previousManagerId.Value != branchWorker.Id)
        {
            var previousManager = await db.BranchWorkers
                .Include(w => w.User)
                .FirstOrDefaultAsync(w =>
                    w.Id == previousManagerId.Value);

            if (previousManager?.User != null)
            {
                var previousUser = previousManager.User;

                var managesAnotherBranch = await db.Branches
                    .AnyAsync(b =>
                        b.BranchManagerId == previousManager.Id &&
                        !b.IsDeleted);

                if (!managesAnotherBranch)
                {
                    if (await userManager.IsInRoleAsync(
                        previousUser, Roles.BranchManager))
                    {
                        var result =
                            await userManager.RemoveFromRoleAsync(
                                previousUser,
                                Roles.BranchManager);

                        EnsureIdentitySuccess(result);
                    }

                    if (!await userManager.IsInRoleAsync(
                        previousUser, Roles.BranchWorker))
                    {
                        var result =
                            await userManager.AddToRoleAsync(
                                previousUser,
                                Roles.BranchWorker);

                        EnsureIdentitySuccess(result);
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
            Message = "Branch manager assigned successfully."
        };
    }

    #endregion

    #region CREATE BRANCH

    public async Task<ApiResponse<BranchResponse>> CreateBranch(
        CreateBranchRequest request)
    {
        RequireAdmin();

        var existingBranch = await db.Branches
            .AnyAsync(b =>
                b.Name == request.Name &&
                b.City == request.City &&
                b.Address == request.Address &&
                !b.IsDeleted);

        if (existingBranch)
            throw new UserFriendlyException(
                ErrorMessages.BranchAlreadyExists);

        var branch = mapper.Map<Branch>(request);

        branch.Code =
            BranchHelper.GenerateBranchCode(request.Name);

        branch.CreatedAt = DateTime.UtcNow;
        branch.UpdatedAt = null;
        branch.BranchManagerId = null;

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
            // Email failure should not undo branch creation.
            // Add ILogger<BranchService> for production logging.
        }

        return new ApiResponse<BranchResponse>
        {
            Data = MapBranch(branch, null),
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
                .ThenInclude(w => w.User)
            .Include(b => b.Workers)
            .FirstOrDefaultAsync(b =>
                b.Code == code &&
                !b.IsDeleted);

        if (branch == null)
            throw new UserFriendlyException(
                ErrorMessages.BranchNotFound);

        var currentWorkerId = await GetCurrentBranchWorkerId();

        return new ApiResponse<BranchResponse>
        {
            Data = MapBranch(branch, currentWorkerId),
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
            .Include(b => b.BranchManager)
                .ThenInclude(w => w.User)
            .Include(b => b.Workers)
            .FirstOrDefaultAsync(b =>
                b.Id == id &&
                !b.IsDeleted);

        if (branch == null)
            throw new UserFriendlyException(
                ErrorMessages.BranchNotFound);

        var currentWorkerId = await GetCurrentBranchWorkerId();

        return new ApiResponse<BranchResponse>
        {
            Data = MapBranch(branch, currentWorkerId),
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

        var query = db.Branches
            .AsNoTracking()
            .Include(b => b.BranchManager)
                .ThenInclude(w => w.User)
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

        var currentWorkerId = await GetCurrentBranchWorkerId();

        var responses = branches
            .Select(b => MapBranch(b, currentWorkerId))
            .ToList();

        return new PagedApiResponse<BranchResponse>(
            responses,
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
        RequireAuthenticated();

        var branch = await db.Branches
            .Include(b => b.BranchManager)
            .FirstOrDefaultAsync(b =>
                b.Id == branchId &&
                !b.IsDeleted);

        if (branch == null)
            throw new UserFriendlyException(
                ErrorMessages.BranchNotFound);

        await EnsureBranchManagerAccess(branch);

        var duplicate = await db.Branches.AnyAsync(b =>
            b.Id != branchId &&
            b.Name == request.Name &&
            b.City == request.City &&
            b.Address == request.Address &&
            !b.IsDeleted);

        if (duplicate)
            throw new UserFriendlyException(
                ErrorMessages.BranchAlreadyExists);

        mapper.Map(request, branch);

        branch.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();

        var currentWorkerId = await GetCurrentBranchWorkerId();

        return new ApiResponse<BranchResponse>
        {
            Data = MapBranch(branch, currentWorkerId),
            IsSuccess = true,
            Message = "Branch Updated"
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

        if (pageNumber <= 0 || pageSize <= 0)
            throw new UserFriendlyException(
                ErrorMessages.InvalidPagination);

        var branch = await db.Branches
            .AsNoTracking()
            .FirstOrDefaultAsync(b =>
                b.Id == branchId &&
                !b.IsDeleted);

        if (branch == null)
            throw new UserFriendlyException(
                ErrorMessages.BranchNotFound);

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
            throw new UserFriendlyException(
                ErrorMessages.BranchNotFound);

        await EnsureBranchManagerAccess(branch);

        var user = await userManager.FindByIdAsync(request.UserId);

        if (user == null)
            throw new UserFriendlyException(
                ErrorMessages.UserNotFound);

        if (await userManager.IsInRoleAsync(user, Roles.Admin))
            throw new UserFriendlyException(ErrorMessages.Forbidden);

        // A user cannot be both a warehouse worker/manager
        var warehouseWorkerExists = await db.WarehouseWorkers
            .AnyAsync(w =>
                w.UserId == user.Id &&
                w.IsActive &&
                !w.IsDeleted);

        var warehouseManagerExists = await db.WarehouseManagers
            .AnyAsync(wm => wm.UserId == user.Id);

        if (warehouseWorkerExists || warehouseManagerExists)
            throw new UserFriendlyException(ErrorMessages.Forbidden);

        var branchWorker = await db.BranchWorkers
            .FirstOrDefaultAsync(w => w.UserId == user.Id);

        if (branchWorker != null && !branchWorker.IsDeleted)
            throw new UserFriendlyException(
                ErrorMessages.EmployeeAlreadyAssigned);

        await using var transaction =
            await db.Database.BeginTransactionAsync();

        if (branchWorker == null)
        {
            branchWorker = new BranchWorker
            {
                Id = Guid.CreateVersion7(),
                UserId = user.Id,
                FirstName = user.UserName ?? user.Email ?? string.Empty,
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
            // Reactivate an existing soft-deleted record.
            branchWorker.BranchId = branch.Id;
            branchWorker.IsActive = true;
            branchWorker.IsDeleted = false;
            branchWorker.HireDate = DateTime.UtcNow;
            branchWorker.UpdatedAt = DateTime.UtcNow;
        }

        if (!await userManager.IsInRoleAsync(user, Roles.BranchWorker))
        {
            var result = await userManager.AddToRoleAsync(
                user, Roles.BranchWorker);

            EnsureIdentitySuccess(result);
        }

        await db.SaveChangesAsync();
        await transaction.CommitAsync();

        return new ApiResponse<BranchWorkerResponse>
        {
            Data = mapper.Map<BranchWorkerResponse>(branchWorker),
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
            throw new UserFriendlyException(
                ErrorMessages.BranchNotFound);

        var user = await userManager.FindByIdAsync(request.UserId);

        if (user == null)
            throw new UserFriendlyException(
                ErrorMessages.UserNotFound);

        var branchWorker = await db.BranchWorkers
            .FirstOrDefaultAsync(w =>
                w.UserId == user.Id &&
                w.BranchId == branchId &&
                !w.IsDeleted);

        if (branchWorker == null)
            throw new UserFriendlyException(
                ErrorMessages.EmployeeNotFound);

        if (branch.BranchManagerId == branchWorker.Id)
            throw new UserFriendlyException(
                ErrorMessages.Forbidden);

        await using var transaction =
            await db.Database.BeginTransactionAsync();

        var currentRoles = await userManager.GetRolesAsync(user);

        var branchRoles = currentRoles
            .Where(r =>
                r == Roles.BranchWorker ||
                r == Roles.Cashier ||
                r == Roles.InventoryManager ||
                r == Roles.BranchManager)
            .ToList();

        if (branchRoles.Count > 0)
        {
            var result = await userManager.RemoveFromRolesAsync(
                user, branchRoles);

            EnsureIdentitySuccess(result);
        }

        if (!await userManager.IsInRoleAsync(user, Roles.User))
        {
            var result = await userManager.AddToRoleAsync(
                user, Roles.User);

            EnsureIdentitySuccess(result);
        }

        branchWorker.IsActive = false;
        branchWorker.IsDeleted = true;
        branchWorker.UpdatedAt = DateTime.UtcNow;

        branch.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        await transaction.CommitAsync();

        return new ApiResponse<BranchWorkerResponse>
        {
            Data = mapper.Map<BranchWorkerResponse>(branchWorker),
            IsSuccess = true,
            Message = "Branch worker unassigned successfully."
        };
    }

    #endregion

    #region STATISTICS

    public Task<PagedApiResponse<BranchResponse>> GetStatistics(
        Guid branchId,
        int pageNumber = 1,
        int pageSize = 10)
    {
        throw new NotImplementedException();
    }

    #endregion
}
