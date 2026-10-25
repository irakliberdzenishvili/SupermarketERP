using AutoMapper;
using DotNetApiTemplate.DB;
using DotNetApiTemplate.Models.Data;
using DotNetApiTemplate.Models.Response;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SupermarketERP.Constants;
using SupermarketERP.Exceptions;
using SupermarketERP.Models.Data;
using SupermarketERP.Models.Request.Employee;
using SupermarketERP.Models.Response;
using SupermarketERP.Models.Response.Employee;

namespace SupermarketERP.Helpers;

public class EmployeeHelper
{
    private readonly ApplicationDbContext db;
    private readonly UserManager<User> userManager;
    private readonly IMapper mapper;

    private static readonly HashSet<string> BranchRoles =
    [
        Roles.Cashier,
        Roles.BranchWorker,
        Roles.BranchManager,
        Roles.InventoryManager
    ];

    private static readonly HashSet<string> WarehouseRoles =
    [
        Roles.WarehouseWorker,
        Roles.WarehouseManager
    ];

    public EmployeeHelper(
        ApplicationDbContext db,
        UserManager<User> userManager,
        IMapper mapper)
    {
        this.db = db;
        this.userManager = userManager;
        this.mapper = mapper;
    }

    #region GET USER

    public async Task<User> GetUserForProvisioning(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new UserFriendlyException(
                ErrorMessages.UserNotFound);
        }

        var user = await userManager.FindByIdAsync(userId);

        if (user == null)
        {
            throw new UserFriendlyException(
                ErrorMessages.UserNotFound);
        }

        return user;
    }

    #endregion

    #region VALIDATE EMPLOYEE REQUEST

    public async Task ValidateEmployeeRequest(
        CreateEmployeeRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.UserId))
        {
            throw new UserFriendlyException(
                ErrorMessages.UserNotFound);
        }

        if (string.IsNullOrWhiteSpace(request.Role))
        {
            throw new UserFriendlyException(
                ErrorMessages.InvalidRole);
        }

        var isBranchRole = BranchRoles.Contains(request.Role);
        var isWarehouseRole = WarehouseRoles.Contains(request.Role);
        var isAdminRole = request.Role == Roles.Admin;

        if (!isBranchRole && !isWarehouseRole && !isAdminRole)
        {
            throw new UserFriendlyException(
                ErrorMessages.InvalidRole);
        }

        if (isBranchRole)
        {
            if (!request.BranchId.HasValue ||
                request.WarehouseId.HasValue)
            {
                throw new UserFriendlyException(
                    ErrorMessages.InvalidRequest);
            }

            var branchExists = await db.Branches
                .AsNoTracking()
                .AnyAsync(b =>
                    b.Id == request.BranchId.Value &&
                    b.IsActive &&
                    !b.IsDeleted);

            if (!branchExists)
            {
                throw new UserFriendlyException(
                    ErrorMessages.BranchNotFound);
            }

            if (request.Role == Roles.BranchManager)
            {
                var managerExists = await db.BranchManagers
                    .AnyAsync(bm =>
                        bm.BranchId == request.BranchId.Value);

                if (managerExists)
                {
                    throw new UserFriendlyException(
                        ErrorMessages.ManagerAlreadyAssigned);
                }
            }
        }
        else if (isWarehouseRole)
        {
            if (!request.WarehouseId.HasValue ||
                request.BranchId.HasValue)
            {
                throw new UserFriendlyException(
                    ErrorMessages.InvalidRequest);
            }

            var warehouseExists = await db.Warehouses
                .AsNoTracking()
                .AnyAsync(w =>
                    w.Id == request.WarehouseId.Value &&
                    w.IsActive &&
                    !w.IsDeleted);

            if (!warehouseExists)
            {
                throw new UserFriendlyException(
                    ErrorMessages.WarehouseNotFound);
            }

            if (request.Role == Roles.WarehouseManager)
            {
                var managerExists = await db.WarehouseManagers
                    .AnyAsync(wm =>
                        wm.WarehouseId == request.WarehouseId.Value);

                if (managerExists)
                {
                    throw new UserFriendlyException(
                        ErrorMessages.WarehouseManagerAlreadyManagesWarehouse);
                }
            }
        }
        else
        {
            if (request.BranchId.HasValue ||
                request.WarehouseId.HasValue)
            {
                throw new UserFriendlyException(
                    ErrorMessages.InvalidRequest);
            }
        }
    }

    #endregion

    #region VALIDATE USER ELIGIBILITY

    public async Task ValidateUserEligibility(User user)
    {
        var existingRoles = await userManager.GetRolesAsync(user);

        // Allow only a basic User role before provisioning.
        if (existingRoles.Any(r => r != Roles.User))
        {
            throw new UserFriendlyException(
                ErrorMessages.UserAlreadyHasRole);
        }

        var hasBranchWorker = await db.BranchWorkers
            .IgnoreQueryFilters()
            .AnyAsync(bw => bw.UserId == user.Id);

        if (hasBranchWorker)
        {
            throw new UserFriendlyException(
                ErrorMessages.UserAlreadyAssignedToBranch);
        }

        var hasBranchManager = await db.BranchManagers
            .AnyAsync(bm => bm.UserId == user.Id);

        if (hasBranchManager)
        {
            throw new UserFriendlyException(
                ErrorMessages.ManagerAlreadyAssigned);
        }

        var hasWarehouseWorker = await db.WarehouseWorkers
            .IgnoreQueryFilters()
            .AnyAsync(ww => ww.UserId == user.Id);

        if (hasWarehouseWorker)
        {
            throw new UserFriendlyException(
                ErrorMessages.UserAlreadyAssignedToWarehouse);
        }

        var hasWarehouseManager = await db.WarehouseManagers
            .AnyAsync(wm => wm.UserId == user.Id);

        if (hasWarehouseManager)
        {
            throw new UserFriendlyException(
                ErrorMessages.WarehouseManagerAlreadyAssigned);
        }
    }

    #endregion

    #region ASSIGN IDENTITY ROLE

    public async Task AssignEmployeeRole(
        User user,
        string role)
    {
        var existingRoles = await userManager.GetRolesAsync(user);

        if (existingRoles.Any(r => r != Roles.User))
        {
            throw new UserFriendlyException(
                ErrorMessages.UserCannotHaveMultipleRoles);
        }

        if (existingRoles.Contains(Roles.User))
        {
            var removeResult = await userManager.RemoveFromRoleAsync(
                user,
                Roles.User);

            if (!removeResult.Succeeded)
            {
                throw new UserFriendlyException(
                    ErrorMessages.FailedToAssignRole);
            }
        }

        var result = await userManager.AddToRoleAsync(user, role);

        if (!result.Succeeded)
        {
            throw new UserFriendlyException(
                ErrorMessages.FailedToAssignRole);
        }
    }

    #endregion

    #region ASSIGN WORKPLACE

    public async Task AssignWorkplace(
        User user,
        CreateEmployeeRequest request)
    {
        switch (request.Role)
        {
            case Roles.BranchManager:
                await AssignBranchManager(user, request);
                break;

            case Roles.BranchWorker:
            case Roles.Cashier:
            case Roles.InventoryManager:
                await AssignBranchWorker(user, request);
                break;

            case Roles.WarehouseManager:
                await AssignWarehouseManager(user, request);
                break;

            case Roles.WarehouseWorker:
                await AssignWarehouseWorker(user, request);
                break;

            case Roles.Admin:
                // Admin does not require a workplace.
                break;

            default:
                throw new UserFriendlyException(
                    ErrorMessages.InvalidRole);
        }
    }

    #endregion

    #region ASSIGN BRANCH MANAGER

    private async Task AssignBranchManager(
        User user,
        CreateEmployeeRequest request)
    {
        var branchId = request.BranchId!.Value;

        var branchExists = await db.Branches
            .AnyAsync(b =>
                b.Id == branchId &&
                b.IsActive &&
                !b.IsDeleted);

        if (!branchExists)
        {
            throw new UserFriendlyException(
                ErrorMessages.BranchNotFound);
        }

        var managerExists = await db.BranchManagers
            .AnyAsync(bm => bm.BranchId == branchId);

        if (managerExists)
        {
            throw new UserFriendlyException(
                ErrorMessages.ManagerAlreadyAssigned);
        }

        var userAlreadyManager = await db.BranchManagers
            .AnyAsync(bm => bm.UserId == user.Id);

        if (userAlreadyManager)
        {
            throw new UserFriendlyException(
                ErrorMessages.ManagerAlreadyAssigned);
        }

        await EnsureNoExistingWorkplace(user.Id);

        var manager = new BranchManager
        {
            Id = Guid.CreateVersion7(),
            UserId = user.Id,
            BranchId = branchId,
            AssignedAt = DateTime.UtcNow
        };

        await db.BranchManagers.AddAsync(manager);
    }

    #endregion

    #region ASSIGN BRANCH WORKER

    private async Task AssignBranchWorker(
        User user,
        CreateEmployeeRequest request)
    {
        await EnsureNoExistingWorkplace(user.Id);

        var worker = new BranchWorker
        {
            Id = Guid.CreateVersion7(),
            UserId = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            BranchId = request.BranchId!.Value,
            HireDate = DateTime.UtcNow,
            IsActive = true,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow
        };

        await db.BranchWorkers.AddAsync(worker);
    }

    #endregion

    #region ASSIGN WAREHOUSE MANAGER

    private async Task AssignWarehouseManager(
        User user,
        CreateEmployeeRequest request)
    {
        var warehouseId = request.WarehouseId!.Value;

        var managerExists = await db.WarehouseManagers
            .AnyAsync(wm => wm.WarehouseId == warehouseId);

        if (managerExists)
        {
            throw new UserFriendlyException(
                ErrorMessages.WarehouseManagerAlreadyManagesWarehouse);
        }

        await EnsureNoExistingWorkplace(user.Id);

        var manager = new WarehouseManager
        {
            Id = Guid.CreateVersion7(),
            UserId = user.Id,
            WarehouseId = warehouseId
        };

        await db.WarehouseManagers.AddAsync(manager);
    }

    #endregion

    #region ASSIGN WAREHOUSE WORKER

    private async Task AssignWarehouseWorker(
        User user,
        CreateEmployeeRequest request)
    {
        await EnsureNoExistingWorkplace(user.Id);

        var worker = new WarehouseWorker
        {
            Id = Guid.CreateVersion7(),
            UserId = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            WarehouseId = request.WarehouseId!.Value
        };

        await db.WarehouseWorkers.AddAsync(worker);
    }

    #endregion

    #region CHECK EXISTING WORKPLACE

    private async Task EnsureNoExistingWorkplace(string userId)
    {
        var hasBranchWorker = await db.BranchWorkers
            .IgnoreQueryFilters()
            .AnyAsync(w => w.UserId == userId);

        if (hasBranchWorker)
        {
            throw new UserFriendlyException(
                ErrorMessages.UserAlreadyAssignedToBranch);
        }

        var hasBranchManager = await db.BranchManagers
            .AnyAsync(m => m.UserId == userId);

        if (hasBranchManager)
        {
            throw new UserFriendlyException(
                ErrorMessages.ManagerAlreadyAssigned);
        }

        var hasWarehouseWorker = await db.WarehouseWorkers
            .IgnoreQueryFilters()
            .AnyAsync(w => w.UserId == userId);

        if (hasWarehouseWorker)
        {
            throw new UserFriendlyException(
                ErrorMessages.UserAlreadyAssignedToWarehouse);
        }

        var hasWarehouseManager = await db.WarehouseManagers
            .AnyAsync(m => m.UserId == userId);

        if (hasWarehouseManager)
        {
            throw new UserFriendlyException(
                ErrorMessages.WarehouseManagerAlreadyAssigned);
        }
    }

    #endregion

    #region GET EMPLOYEE RESPONSE

    public async Task<ApiResponse<EmployeeResponse>>
        GetEmployeeResponse(string userId)
    {
        var user = await GetUserForProvisioning(userId);

        var branchWorkers = await db.BranchWorkers
            .AsNoTracking()
            .Where(bw =>
                bw.UserId == user.Id &&
                !bw.IsDeleted)
            .ToListAsync();

        var warehouseWorkers = await db.WarehouseWorkers
            .AsNoTracking()
            .Where(ww =>
                ww.UserId == user.Id &&
                !ww.IsDeleted)
            .ToListAsync();

        var response = new EmployeeResponse
        {
            BranchWorkers =
                mapper.Map<List<BranchWorkerResponse>>(
                    branchWorkers),

            WarehouseWorkers =
                mapper.Map<List<WarehouseWorkerResponse>>(
                    warehouseWorkers)
        };

        return new ApiResponse<EmployeeResponse>
        {
            Data = response,
            IsSuccess = true,
            Message = "Employee retrieved successfully."
        };
    }

    #endregion
}