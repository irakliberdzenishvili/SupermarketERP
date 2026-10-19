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

namespace SupermarketERP.Services
{
    public class BranchService(AuthHelper authHelper,
        ApplicationDbContext db,
        IMapper mapper,
        IEmailService emailService,
        UserManager<User> userManager) : IBranchService
    {
        public async Task<ApiResponse<BranchResponse>> AssignBranchManager(
     Guid branchId,
     AssignBranchManagerRequest request)
        {
            var currentUserId = authHelper.UserId;
            var currentUserRole = authHelper.UserRole;

            if (currentUserId == 0)
            {
                throw new UserFriendlyException(ErrorMessages.Unauthorized);
            }

            if (currentUserRole != Roles.Admin)
            {
                throw new UserFriendlyException(ErrorMessages.Forbidden);
            }

            var branch = await db.Branches
                .FirstOrDefaultAsync(b => b.Id == branchId);

            if (branch == null)
            {
                throw new UserFriendlyException(ErrorMessages.BranchNotFound);
            }

            var user = await userManager.FindByIdAsync(request.UserId);

            if (user == null)
            {
                throw new UserFriendlyException(ErrorMessages.UserNotFound);
            }

            if (await userManager.IsInRoleAsync(user, Roles.Admin))
            {
                throw new UserFriendlyException(ErrorMessages.Forbidden);
            }

            // Check whether this user manages another branch.
            var alreadyAssigned = await db.Branches
                .AnyAsync(b =>
                    b.BranchManagerId == user.Id &&
                    b.Id != branchId);

            if (alreadyAssigned)
            {
                throw new UserFriendlyException(
                    ErrorMessages.ManagerAlreadyAssigned);
            }

            // Same manager: nothing needs to change.
            if (branch.BranchManagerId == user.Id)
            {
                var existingResponse = mapper.Map<BranchResponse>(branch);
                existingResponse.IsManager = await userManager.IsInRoleAsync(
                    user, Roles.BranchManager);

                return new ApiResponse<BranchResponse>(existingResponse)
                {
                    IsSuccess = true,
                    Message = "Manager is already assigned to this branch"
                };
            }

            // Remember the previous manager before replacing them.
            var previousManagerId = branch.BranchManagerId;

            await using var transaction =
                await db.Database.BeginTransactionAsync();

            // Promote the new manager if necessary.
            var isManager = await userManager.IsInRoleAsync(
                user, Roles.BranchManager);

            if (!isManager)
            {
                var addRoleResult = await userManager.AddToRoleAsync(
                    user, Roles.BranchManager);

                if (!addRoleResult.Succeeded)
                {
                    throw new UserFriendlyException(
                        ErrorMessages.FailedToAssignRole);
                }
            }

            // Assign the new manager.
            branch.BranchManagerId = user.Id;
            branch.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();

            // Remove the previous manager's role if they no longer
            // manage any branch.
            if (!string.IsNullOrWhiteSpace(previousManagerId))
            {
                var previousManager = await userManager.FindByIdAsync(
                    previousManagerId);

                if (previousManager != null)
                {
                    var managesAnotherBranch = await db.Branches
                        .AnyAsync(b =>
                            b.BranchManagerId == previousManagerId &&
                            b.Id != branchId);

                    if (!managesAnotherBranch)
                    {
                        var previousManagerHasRole =
                            await userManager.IsInRoleAsync(
                                previousManager, Roles.BranchManager);

                        if (previousManagerHasRole)
                        {
                            var removeRoleResult =
                                await userManager.RemoveFromRoleAsync(
                                    previousManager, Roles.BranchManager);

                            if (!removeRoleResult.Succeeded)
                            {
                                throw new UserFriendlyException(
                                    ErrorMessages.FailedToRemoveRole);
                            }
                        }
                    }
                }
            }

            await transaction.CommitAsync();

            var mappedResponse = mapper.Map<BranchResponse>(branch);
            mappedResponse.IsManager = true;

            return new ApiResponse<BranchResponse>(mappedResponse)
            {
                IsSuccess = true,
                Message = "Manager assigned successfully"
            };
        }

        public async Task<ApiResponse<BranchResponse>> CreateBranch(CreateBranchRequest request)
        {

            var currentUserId = authHelper.UserId;
            var CurrentUserRole = authHelper.UserRole;

            if (currentUserId == 0)
            {
                throw new UserFriendlyException(ErrorMessages.Unauthorized);
            }

            if (CurrentUserRole != Roles.Admin)
            {
                throw new UserFriendlyException(ErrorMessages.Forbidden);
            }

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
             await emailService.SendEmailAsync(
                    newBranch.Email,
                    "New Branch Created",
                    htmlBody);
            }
            catch (Exception)
            {

            }

                var response = mapper.Map<BranchResponse>(newBranch);


                return new ApiResponse<BranchResponse>
                {
                    IsSuccess = true,
                    Data = response,
                    Message = "Branch Created"
                };
        }

        public async Task<ApiResponse<BranchResponse>> GetBranchByCode(string code)
        {
            //var currentUserId = authHelper.UserId;

            //if (currentUserId == 0)
            //{
            //    throw new UserFriendlyException(ErrorMessages.Unauthorized);
            //}

            if (string.IsNullOrWhiteSpace(code))
            {
                throw new UserFriendlyException(ErrorMessages.BranchCodeIsRequired);
            }

            var branch = await db.Branches.FirstOrDefaultAsync(b => b.Code == code);

            if (branch is null)
            {
                throw new UserFriendlyException(ErrorMessages.BranchNotFound);
            }

            var mappedResponse = mapper.Map<BranchResponse>(branch);

            return new ApiResponse<BranchResponse>
            {
                IsSuccess = true,
                Data = mappedResponse,
                Message = "Branch Found"
            };
                 
        }

        public async Task<ApiResponse<BranchResponse>> GetBranchById(Guid id)
        {
            //var currentUserId = authHelper.UserId;

            //if (currentUserId == 0)
            //{
            //    throw new UserFriendlyException(ErrorMessages.Unauthorized);
            //}

            var branch = await db.Branches.FirstOrDefaultAsync(b => b.Id == id);

            if (branch is null)
            {
                throw new UserFriendlyException(ErrorMessages.BranchNotFound);
            }

            var mappedResponse = mapper.Map<BranchResponse>(branch);

            return new ApiResponse<BranchResponse>
            {
                IsSuccess = true,
                Data = mappedResponse,
                Message = "Branch found"
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
                    pageNumber
                    );
            }

            //var currentUserId = authHelper.UserId;

            //if (currentUserId == 0)
            //{
            //    throw new UserFriendlyException(ErrorMessages.Unauthorized);
            //}

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

            var mappedBranches = mapper.Map<List<BranchResponse>>(pagedBranches);


            return new PagedApiResponse<BranchResponse>(
                mappedBranches,
                totalCount,
                pageSize,
                pageNumber
                );

        }

        public async Task<ApiResponse<BranchResponse>> UpdateBranch(Guid branchId, UpdateBranchRequest request)
        {
            var currentUserId = authHelper.UserId.ToString();
            var currentUserRole = authHelper.UserRole;

            if (string.IsNullOrWhiteSpace(currentUserId))
            {
                throw new UserFriendlyException(ErrorMessages.Unauthorized);
            }

            var branch = await db.Branches.FirstOrDefaultAsync(b => b.Id == branchId);

            if (branch == null)
            {
                throw new UserFriendlyException(ErrorMessages.BranchNotFound);
            }

            var isAdmin = currentUserRole == Roles.Admin;

            var isBranchManager = currentUserRole == Roles.BranchManager && branch.BranchManagerId == currentUserId;

            if (!isAdmin && !isBranchManager) {
                throw new UserFriendlyException(ErrorMessages.Forbidden);
            }

            mapper.Map(request, branch);

            branch.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();

            var mappedResponse = mapper.Map<BranchResponse>(branch);

            return new ApiResponse<BranchResponse> { Data = mappedResponse, IsSuccess = true, Message = "Branch Updated" };
        }
    }
}
