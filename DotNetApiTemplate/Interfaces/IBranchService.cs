using DotNetApiTemplate.Models.Response;
using SupermarketERP.Models.Request.Branch;
using SupermarketERP.Models.Response.Branch;

namespace SupermarketERP.Interfaces
{
    public interface IBranchService
    {
        Task<PagedApiResponse<BranchResponse>> GetBranches(string? city,
            string? searchTerm,
            int pageNumber = 1,
            int pageSize = 10);

        Task<ApiResponse<BranchResponse>> CreateBranch(CreateBranchRequest request);

        Task<ApiResponse<BranchResponse>> GetBranchById(Guid id);
        Task<ApiResponse<BranchResponse>> GetBranchByCode(string code);

        Task<ApiResponse<BranchResponse>> UpdateBranch(Guid branchId, UpdateBranchRequest request);

        Task<ApiResponse<BranchResponse>> AssignBranchManager(Guid branchId, AssignBranchManagerRequest request);

        Task<ApiResponse<BranchResponse>> ChangeBranchStatus(Guid branchId, ActivateBranchRequest request);

    }
}
