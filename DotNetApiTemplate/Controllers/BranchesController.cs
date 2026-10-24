using DotNetApiTemplate.Models.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SupermarketERP.Constants;
using SupermarketERP.Interfaces;
using SupermarketERP.Models.Request;
using SupermarketERP.Models.Request.Branch;
using SupermarketERP.Models.Response;
using SupermarketERP.Security;

namespace SupermarketERP.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class BranchesController(
        IBranchService branchService) : ControllerBase
    {
        [HttpGet]
        [AllowAnonymous]
        [EnableRateLimiting(BranchRateLimitPolicy.Get)]
        public async Task<PagedApiResponse<BranchResponse>> GetBranches(
            [FromQuery] string? city,
            [FromQuery] string? searchTerm,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            return await branchService.GetBranches(
                city, searchTerm, pageNumber, pageSize);
        }

        [HttpPost]
        [Authorize(Roles = Roles.Admin)]
        [EnableRateLimiting(BranchRateLimitPolicy.Create)]
        public async Task<ApiResponse<BranchResponse>> CreateBranch(
            [FromBody] CreateBranchRequest request)
        {
            return await branchService.CreateBranch(request);
        }

        [HttpGet("{id:guid}")]
        [AllowAnonymous]
        [EnableRateLimiting(BranchRateLimitPolicy.Get)]
        public async Task<ApiResponse<BranchResponse>> GetBranchById(
            [FromRoute] Guid id)
        {
            return await branchService.GetBranchById(id);
        }

        [HttpGet("code/{code}")]
        [AllowAnonymous]
        [EnableRateLimiting(BranchRateLimitPolicy.Get)]
        public async Task<ApiResponse<BranchResponse>> GetBranchByCode(
            [FromRoute] string code)
        {
            return await branchService.GetBranchByCode(code);
        }

        [HttpPatch("{id:guid}")]
        [Authorize(Roles = $"{Roles.Admin},{Roles.BranchManager}")]
        [EnableRateLimiting(BranchRateLimitPolicy.Update)]
        public async Task<ApiResponse<BranchResponse>> UpdateBranch(
            [FromRoute] Guid id,
            [FromBody] UpdateBranchRequest request)
        {
            return await branchService.UpdateBranch(id, request);
        }

        [HttpPost("{id:guid}/manager")]
        [Authorize(Roles = Roles.Admin)]
        [EnableRateLimiting(BranchRateLimitPolicy.AssignManager)]
        public async Task<ApiResponse<BranchResponse>> AssignManager(
            [FromRoute] Guid id,
            [FromBody] AssignBranchManagerRequest request)
        {
            return await branchService.AssignBranchManager(id, request);
        }

        [HttpPatch("{id:guid}/status")]
        [Authorize(Roles = Roles.Admin)]
        [EnableRateLimiting(BranchRateLimitPolicy.ChangeStatus)]
        public async Task<ApiResponse<BranchResponse>> ChangeBranchStatus(
            [FromRoute] Guid id,
            [FromBody] ChangeBranchStatusRequest request)
        {
            return await branchService.ChangeBranchStatus(id, request);
        }

        [HttpGet("{id:guid}/employees")]
        [Authorize(Roles = $"{Roles.Admin},{Roles.BranchManager}")]
        [EnableRateLimiting(BranchRateLimitPolicy.Get)]
        public async Task<PagedApiResponse<BranchWorkerResponse>> GetAssignedEmployees(
            [FromRoute] Guid id,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            return await branchService.GetAssignedBranchWorkers(
                id, pageNumber, pageSize);
        }

        [HttpPost("{id:guid}/employees/{userId:guid}")]
        [Authorize(Roles = $"{Roles.Admin},{Roles.BranchManager}")]
        [EnableRateLimiting(BranchRateLimitPolicy.AssignEmployee)]
        public async Task<ApiResponse<BranchWorkerResponse>> AssignEmployeeToBranch(
            [FromRoute] Guid id,
            [FromRoute] Guid userId)
        {
            var request = new AssignBranchWorkerRequest
            {
                UserId = userId.ToString()
            };

            return await branchService.AssignBranchWorkerToBranch(
                id, request);
        }

        [HttpDelete("{id:guid}/employees/{userId:guid}")]
        [Authorize(Roles = $"{Roles.Admin},{Roles.BranchManager}")]
        [EnableRateLimiting(BranchRateLimitPolicy.UnassignEmployee)]
        public async Task<ApiResponse<BranchWorkerResponse>> UnassignEmployeeFromBranch(
            [FromRoute] Guid id,
            [FromRoute] Guid userId)
        {
            var request = new UnassignBranchWorkerRequest
            {
                UserId = userId.ToString()
            };

            return await branchService.UnassignBranchWorkerFromBranch(
                id, request);
        }
    }
}