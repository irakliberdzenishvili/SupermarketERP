using DotNetApiTemplate.Models.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SupermarketERP.Constants;
using SupermarketERP.DB.Seed;
using SupermarketERP.Interfaces;
using SupermarketERP.Models.Request.AssignEmployeeRequest;
using SupermarketERP.Models.Request.Branch;
using SupermarketERP.Models.Request.Employee;
using SupermarketERP.Models.Response.EmployeeResponse;
using SupermarketERP.Security;

namespace SupermarketERP.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [EnableRateLimiting(BranchRateLimitPolicy.PolicyName)]
    [Authorize]
    public class BranchesController(IBranchService branchService) : ControllerBase
    {

        [HttpGet]
        [AllowAnonymous]
        public async Task<PagedApiResponse<BranchResponse>> GetBranches(
            [FromQuery] string? city,
            [FromQuery] string? searchTerm,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
          var branches = await branchService.GetBranches(city, searchTerm, pageNumber, pageSize);

          return branches;
        }

        [HttpPost]
        [Authorize(Roles = Roles.Admin)]
        public async Task<ApiResponse<BranchResponse>> CreateBranch([FromBody] CreateBranchRequest request)
        {
            var branch = await branchService.CreateBranch(request);

            return branch;
        }

        [HttpGet("{id:guid}")]
        [AllowAnonymous]
        public async Task<ApiResponse<BranchResponse>> GetBranchById([FromRoute] Guid id)
        {
           var branch =  await branchService.GetBranchById(id);
           return branch;
        }

        [HttpGet("code/{code}")]
        [AllowAnonymous]
        public async Task<ApiResponse<BranchResponse>> GetBranchByCode([FromRoute] string code)
        {
            var branch = await branchService.GetBranchByCode(code);
            return branch;
        }

        [HttpPatch("{id:guid}")]
        [Authorize(Roles = $"{Roles.Admin},{Roles.BranchManager}")]
        public async Task<ApiResponse<BranchResponse>> UpdateBranch([FromRoute] Guid id, [FromBody] UpdateBranchRequest request)
        {
            var branch = await branchService.UpdateBranch(id, request);
            return branch;
        }

        [HttpPost("{id:guid}/manager")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<ApiResponse<BranchResponse>> AssignManager([FromRoute] Guid id, [FromBody] AssignBranchManagerRequest request)
        {
           var result =  await branchService.AssignBranchManager(id, request);
            return result;
        }

        [HttpPost("{id:guid}/activate")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<ApiResponse<BranchResponse>> ChangeBranchStatus([FromRoute] Guid id, [FromBody] ActivateBranchRequest request)
        {
            var result = await branchService.ChangeBranchStatus(id, request);
            return result;
        }

        [HttpGet("{id:guid}/employees")]
        [Authorize(Roles = $"{Roles.Admin},{Roles.BranchManager}")]
        public async Task<PagedApiResponse<EmployeeResponse>> GetAssignedEmployees([FromRoute] Guid id, int pageNumber = 1, int pageSize = 10)
        {
            var result = await branchService.GetAssignedEmployees(id, pageNumber, pageSize);
            return result;
        }

        [HttpPost("{id:guid}/employees/{userId:guid}")]
        [Authorize(Roles = $"{Roles.Admin},{Roles.BranchManager}")]
        public async Task<ApiResponse<EmployeeResponse>> AssignEmployeeToBranch(
    [FromRoute] Guid id,
    [FromRoute] Guid userId)
        {
            var request = new AssignEmployeeRequest
            {
                UserId = userId.ToString()
            };

            return await branchService.AssignEmployeeToBranch(id, request);
        }

        [HttpDelete("{id:guid}/employees/{userId:guid}")]
        [Authorize(Roles = $"{Roles.Admin},{Roles.BranchManager}")]
        public async Task<ApiResponse<EmployeeResponse>> UnassignEmployeeFrojBranch(
    [FromRoute] Guid id,
    [FromRoute] Guid userId)
        {
            var request = new UnassignEmployeeRequest
            {
                UserId = userId.ToString()
            };

            return await branchService.UnassignEmployeeFromBranch(id, request);
        }

    }
}
