using SupermarketERP.Models.Response;
using SupermarketERP.Models.Response.BranchManagerResponse;

public class BranchDetailsResponse : BranchResponse
{
    public List<BranchWorkerResponse> Workers { get; set; } = [];
    public BranchManagerResponse? Manager { get; set; }
}