
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
using SupermarketERP.Models.Request.Branch;
using SupermarketERP.Models.Response.Branch;
using System.Security.Claims;

namespace SupermarketERP.Services
{
    public class BranchService(
        AuthHelper authHelper,
        ApplicationDbContext db,
        IMapper mapper,
        IEmailService emailService,
        UserManager<User> userManager,
        IHttpContextAccessor httpContextAccessor
    ) : IBranchService
    {
        private string? CurrentUserId => authHelper.UserId;
            

        private string CurrentUserRole => authHelper.UserRole;

        private string GetRequiredUserId()
        {
            return CurrentUserId
                ?? throw new UserFriendlyException(ErrorMessages.Unauthorized);
        }

        private void EnsureAdmin()
        {
            GetRequiredUserId();

            if (CurrentUserRole != Roles.Admin)
            {
                throw new UserFriendlyException(ErrorMessages.Forbidden);
            }
        }

        private BranchResponse MapBranch(Branch branch)
        {
            var response = mapper.Map<BranchResponse>(branch);

            response.IsManager =
                !string.IsNullOrWhiteSpace(CurrentUserId) &&
                CurrentUserRole == Roles.BranchManager &&
                branch.BranchManagerId == CurrentUserId;

            return response;
        }

        public async Task<ApiResponse<BranchResponse>> ChangeBranchStatus(
            Guid branchId,
            ActivateBranchRequest request)
        {
            EnsureAdmin();

            var branch = await db.Branches
                .FirstOrDefaultAsync(b => b.Id == branchId);

            if (branch == null)
            {
                throw new UserFriendlyException(ErrorMessages.BranchNotFound);
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

            var alreadyAssigned = await db.Branches.AnyAsync(
                b => b.BranchManagerId == user.Id &&
                     b.Id != branchId);

            if (alreadyAssigned)
                throw new UserFriendlyException(
                    ErrorMessages.ManagerAlreadyAssigned);

            var previousManagerId = branch.BranchManagerId;

            await using var transaction =
                await db.Database.BeginTransactionAsync();

            // Replace all existing roles with BranchManager.
            var currentRoles =
                await userManager.GetRolesAsync(user);

            if (currentRoles.Count > 0 &&
                (currentRoles.Count != 1 ||
                 currentRoles[0] != Roles.BranchManager))
            {
                var removeResult =
                    await userManager.RemoveFromRolesAsync(
                        user, currentRoles);

                if (!removeResult.Succeeded)
                {
                    throw new InvalidOperationException(
                        string.Join("; ",
                            removeResult.Errors.Select(
                                e => e.Description)));
                }
            }

            if (!await userManager.IsInRoleAsync(
                user, Roles.BranchManager))
            {
                var addResult =
                    await userManager.AddToRoleAsync(
                        user, Roles.BranchManager);

                if (!addResult.Succeeded)
                {
                    throw new InvalidOperationException(
                        string.Join("; ",
                            addResult.Errors.Select(
                                e => e.Description)));
                }
            }

            branch.BranchManagerId = user.Id;
            branch.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();

            // Remove BranchManager role from the previous manager
            // if they no longer manage any branch.
            if (!string.IsNullOrWhiteSpace(previousManagerId) &&
                previousManagerId != user.Id)
            {
                var stillManagesBranch = await db.Branches
                    .AnyAsync(b =>
                        b.BranchManagerId == previousManagerId);

                if (!stillManagesBranch)
                {
                    var previousManager =
                        await userManager.FindByIdAsync(
                            previousManagerId);

                    if (previousManager != null &&
                        await userManager.IsInRoleAsync(
                            previousManager, Roles.BranchManager))
                    {
                        var removeResult =
                            await userManager.RemoveFromRoleAsync(
                                previousManager, Roles.BranchManager);

                        if (!removeResult.Succeeded)
                        {
                            throw new InvalidOperationException(
                                string.Join("; ",
                                    removeResult.Errors.Select(
                                        e => e.Description)));
                        }
                    }
                }
            }

            await transaction.CommitAsync();

            return new ApiResponse<BranchResponse>
            {
                Data = MapBranch(branch),
                IsSuccess = true,
                Message = "Manager assigned successfully."
            };
        }


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
            {
                throw new UserFriendlyException(
                    ErrorMessages.BranchAlreadyExists);
            }

            var newBranch = mapper.Map<Branch>(request);

            newBranch.Code = BranchHelper.GenerateBranchCode(request.Name);
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
                // Log this exception in production.
            }

            return new ApiResponse<BranchResponse>
            {
                Data = MapBranch(newBranch),
                IsSuccess = true,
                Message = "Branch Created"
            };
        }

        public async Task<ApiResponse<BranchResponse>> GetBranchByCode(
            string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                throw new UserFriendlyException(
                    ErrorMessages.BranchCodeIsRequired);
            }

            var branch = await db.Branches
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.Code == code);

            if (branch == null)
            {
                throw new UserFriendlyException(ErrorMessages.BranchNotFound);
            }

            return new ApiResponse<BranchResponse>
            {
                Data = MapBranch(branch),
                IsSuccess = true,
                Message = "Branch Found"
            };
        }

        public async Task<ApiResponse<BranchResponse>> GetBranchById(
            Guid id)
        {
            var branch = await db.Branches
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == id);

            if (branch == null)
            {
                throw new UserFriendlyException(ErrorMessages.BranchNotFound);
            }

            return new ApiResponse<BranchResponse>
            {
                Data = MapBranch(branch),
                IsSuccess = true,
                Message = "Branch Found"
            };
        }

        public async Task<PagedApiResponse<BranchResponse>> GetBranches(
            string? city,
            string? searchTerm,
            int pageNumber = 1,
            int pageSize = 10)
        {
            if (pageNumber <= 0 || pageSize <= 0)
            {
                return new PagedApiResponse<BranchResponse>(
                    Enumerable.Empty<BranchResponse>(),
                    0,
                    pageSize,
                    pageNumber);
            }

            var branches = db.Branches
                .AsNoTracking()
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

        public async Task<ApiResponse<BranchResponse>> UpdateBranch(
            Guid branchId,
            UpdateBranchRequest request)
        {
            var currentUserId = GetRequiredUserId();

            var branch = await db.Branches
                .FirstOrDefaultAsync(b => b.Id == branchId);

            if (branch == null)
            {
                throw new UserFriendlyException(ErrorMessages.BranchNotFound);
            }

            var isAdmin = CurrentUserRole == Roles.Admin;

            var isBranchManager =
                CurrentUserRole == Roles.BranchManager &&
                branch.BranchManagerId == currentUserId;

            if (!isAdmin && !isBranchManager)
            {
                throw new UserFriendlyException(ErrorMessages.Forbidden);
            }

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
    }
}
