
using DotNetApiTemplate.Models.Response;
using SupermarketERP.Models.Request;
using SupermarketERP.Models.Request.Branch;
using SupermarketERP.Models.Response;

namespace SupermarketERP.Interfaces;

public interface IBranchService
{
    Task<ApiResponse<BranchResponse>> CreateBranch(
        CreateBranchRequest request);

    Task<ApiResponse<BranchResponse>> UpdateBranch(
        Guid branchId,
        UpdateBranchRequest request);

    Task<ApiResponse<BranchResponse>> ChangeBranchStatus(
        Guid branchId,
        ChangeBranchStatusRequest request);

    Task<ApiResponse<BranchResponse>> AssignBranchManager(
        Guid branchId,
        AssignBranchManagerRequest request);

    Task<ApiResponse<BranchDetailsResponse>> GetBranchById(Guid id);

    Task<ApiResponse<BranchDetailsResponse>> GetBranchByCode(string code);

    Task<PagedApiResponse<BranchResponse>> GetBranches(
        string? city,
        string? searchTerm,
        int pageNumber = 1,
        int pageSize = 10);

    Task<PagedApiResponse<BranchWorkerResponse>>
        GetAssignedBranchWorkers(
            Guid branchId,
            int pageNumber = 1,
            int pageSize = 10);

    Task<ApiResponse<BranchWorkerResponse>>
        AssignBranchWorkerToBranch(
            Guid branchId,
            AssignBranchWorkerRequest request);

    Task<ApiResponse<BranchWorkerResponse>>
        UnassignBranchWorkerFromBranch(
            Guid branchId,
            UnassignBranchWorkerRequest request);

    Task<PagedApiResponse<BranchResponse>> GetStatistics(
        Guid branchId,
        int pageNumber = 1,
        int pageSize = 10);
}
