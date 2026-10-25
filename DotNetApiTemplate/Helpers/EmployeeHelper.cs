
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

    // GET EXISTING USER

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

    // VALIDATE EMPLOYEE REQUEST

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

            var branch = await db.Branches
                .AsNoTracking()
                .FirstOrDefaultAsync(b =>
                    b.Id == request.BranchId.Value &&
                    b.IsActive &&
                    !b.IsDeleted);

            if (branch == null)
            {
                throw new UserFriendlyException(
                    ErrorMessages.BranchNotFound);
            }

            if (request.Role == Roles.BranchManager &&
                branch.BranchManagerId.HasValue)
            {
                throw new UserFriendlyException(
                    ErrorMessages.ManagerAlreadyAssigned);
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
                    .IgnoreQueryFilters()
                    .AnyAsync(wm =>
                        wm.WarehouseId == request.WarehouseId.Value);

                if (managerExists)
                {
                    throw new UserFriendlyException(
                        ErrorMessages.WarehouseManagerAlreadyManagesWarehouse);
                }
            }
        }
        else if (isAdminRole)
        {
            if (request.BranchId.HasValue ||
                request.WarehouseId.HasValue)
            {
                throw new UserFriendlyException(
                    ErrorMessages.InvalidRequest);
            }
        }
    }

    // VALIDATE EXISTING USER ELIGIBILITY

    public async Task ValidateUserEligibility(User user)
    {
        var existingRoles = await userManager.GetRolesAsync(user);

        if (existingRoles.Count > 0)
        {
            throw new UserFriendlyException(
                ErrorMessages.UserAlreadyHasRole);
        }

        // Include soft-deleted records because the existing
        // unique indexes may still prevent inserting new records.

        var hasBranchAssignment = await db.BranchWorkers
            .IgnoreQueryFilters()
            .AnyAsync(bw => bw.UserId == user.Id);

        if (hasBranchAssignment)
        {
            throw new UserFriendlyException(
                ErrorMessages.UserAlreadyAssignedToBranch);
        }

        var hasWarehouseAssignment = await db.WarehouseWorkers
            .IgnoreQueryFilters()
            .AnyAsync(ww => ww.UserId == user.Id);

        if (hasWarehouseAssignment)
        {
            throw new UserFriendlyException(
                ErrorMessages.UserAlreadyAssignedToWarehouse);
        }

        var hasWarehouseManagerAssignment = await db.WarehouseManagers
            .IgnoreQueryFilters()
            .AnyAsync(wm => wm.UserId == user.Id);

        if (hasWarehouseManagerAssignment)
        {
            throw new UserFriendlyException(
                ErrorMessages.WarehouseManagerAlreadyAssigned);
        }
    }

    // ASSIGN IDENTITY ROLE

    public async Task AssignEmployeeRole(User user, string role)
    {
        var existingRoles = await userManager.GetRolesAsync(user);

        if (existingRoles.Count > 0)
        {
            throw new UserFriendlyException(
                ErrorMessages.UserCannotHaveMultipleRoles);
        }

        var result = await userManager.AddToRoleAsync(user, role);

        if (!result.Succeeded)
        {
            throw new UserFriendlyException(
                ErrorMessages.FailedToAssignRole);
        }
    }

    // ASSIGN WORKPLACE

    public async Task AssignWorkplace(
        User user,
        CreateEmployeeRequest request)
    {
        if (BranchRoles.Contains(request.Role))
        {
            await AssignBranchWorker(user, request);
        }
        else if (WarehouseRoles.Contains(request.Role))
        {
            await AssignWarehouseEmployee(user, request);
        }
    }

    // ASSIGN BRANCH WORKER OR MANAGER

    private async Task AssignBranchWorker(
        User user,
        CreateEmployeeRequest request)
    {
        var branchId = request.BranchId!.Value;

        // Defensive check: do not allow a warehouse employee
        // to receive a branch assignment.

        var hasWarehouseAssignment =
            await db.WarehouseWorkers
                .IgnoreQueryFilters()
                .AnyAsync(ww => ww.UserId == user.Id)
            ||
            await db.WarehouseManagers
                .IgnoreQueryFilters()
                .AnyAsync(wm => wm.UserId == user.Id);

        if (hasWarehouseAssignment)
        {
            throw new UserFriendlyException(
                ErrorMessages.UserAlreadyAssignedToWarehouse);
        }

        var existingBranchWorker = await db.BranchWorkers
            .IgnoreQueryFilters()
            .AnyAsync(bw => bw.UserId == user.Id);

        if (existingBranchWorker)
        {
            throw new UserFriendlyException(
                ErrorMessages.UserAlreadyAssignedToBranch);
        }

        var worker = new BranchWorker
        {
            UserId = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            BranchId = branchId
        };

        db.BranchWorkers.Add(worker);

        if (request.Role == Roles.BranchManager)
        {
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

            if (branch.BranchManagerId.HasValue)
            {
                throw new UserFriendlyException(
                    ErrorMessages.ManagerAlreadyAssigned);
            }

            branch.BranchManagerId = worker.Id;
        }
    }

    // ASSIGN WAREHOUSE WORKER OR MANAGER

    private async Task AssignWarehouseEmployee(
        User user,
        CreateEmployeeRequest request)
    {
        var warehouseId = request.WarehouseId!.Value;

        // Prevent users from working at both branch and warehouse.

        var hasBranchAssignment = await db.BranchWorkers
            .IgnoreQueryFilters()
            .AnyAsync(bw => bw.UserId == user.Id);

        if (hasBranchAssignment)
        {
            throw new UserFriendlyException(
                ErrorMessages.UserAlreadyAssignedToBranch);
        }

        var existingWarehouseWorker = await db.WarehouseWorkers
            .IgnoreQueryFilters()
            .AnyAsync(ww => ww.UserId == user.Id);

        if (existingWarehouseWorker)
        {
            throw new UserFriendlyException(
                ErrorMessages.UserAlreadyAssignedToWarehouse);
        }

        var existingWarehouseManager = await db.WarehouseManagers
            .IgnoreQueryFilters()
            .AnyAsync(wm => wm.UserId == user.Id);

        if (existingWarehouseManager)
        {
            throw new UserFriendlyException(
                ErrorMessages.WarehouseManagerAlreadyAssigned);
        }

        if (request.Role == Roles.WarehouseWorker)
        {
            db.WarehouseWorkers.Add(new WarehouseWorker
            {
                UserId = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                WarehouseId = warehouseId
            });
        }
        else if (request.Role == Roles.WarehouseManager)
        {
            var managerExists = await db.WarehouseManagers
                .IgnoreQueryFilters()
                .AnyAsync(wm => wm.WarehouseId == warehouseId);

            if (managerExists)
            {
                throw new UserFriendlyException(
                    ErrorMessages.WarehouseManagerAlreadyManagesWarehouse);
            }

            db.WarehouseManagers.Add(new WarehouseManager
            {
                UserId = user.Id,
                WarehouseId = warehouseId
            });
        }
    }

    // GET EMPLOYEE RESPONSE

    public async Task<ApiResponse<EmployeeResponse>> GetEmployeeResponse(
        string userId)
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
            BranchWorkers = mapper.Map<List<BranchWorkerResponse>>(
                branchWorkers),

            WarehouseWorkers = mapper.Map<List<WarehouseWorkerResponse>>(
                warehouseWorkers)
        };

        return new ApiResponse<EmployeeResponse>
        {
            Data = response,
            IsSuccess = true,
            Message = "Employee retrieved successfully."
        };
    }
}
