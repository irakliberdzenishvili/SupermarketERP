using AutoMapper;
using DotNetApiTemplate.DB;
using DotNetApiTemplate.Exceptions;
using DotNetApiTemplate.Helpers;
using DotNetApiTemplate.Interfaces;
using DotNetApiTemplate.Models.Response;
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
        IEmailService emailService) : IBranchService
    {
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
    }
}
