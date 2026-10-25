using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using DotNetApiTemplate.Models.Response;
using SupermarketERP.Constants;
using SupermarketERP.Interfaces;
using SupermarketERP.Models.Response.Employee;
using SupermarketERP.Security;

namespace SupermarketERP.Controllers;

[Route("api/v1/employees")]
[ApiController]
[Authorize(Roles = $"{Roles.Admin},{Roles.BranchManager},{Roles.WarehouseManager}")]
public class EmployeesController(IEmployeeService employeeService)
    : ControllerBase
{
    [HttpGet]
    [EnableRateLimiting(EmployeeRateLimitPolicy.Get)]
    public async Task<PagedApiResponse<EmployeeResponse>> GetEmployees(
        [FromQuery] Guid resourceId,
        [FromQuery] string? role = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        return await employeeService.GetEmployees(
            role,
            isActive,
            resourceId,
            pageNumber,
            pageSize);
    }
}