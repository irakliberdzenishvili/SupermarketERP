
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using DotNetApiTemplate.Models.Response;
using SupermarketERP.Constants;
using SupermarketERP.Interfaces;
using SupermarketERP.Models.Request.Employee;
using SupermarketERP.Models.Response.Employee;
using SupermarketERP.Security;

namespace SupermarketERP.Controllers;

[Route("api/employees")]
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

    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    [EnableRateLimiting(EmployeeRateLimitPolicy.Create)]
    public async Task<ApiResponse<EmployeeResponse>> CreateEmployee(
        [FromBody] CreateEmployeeRequest request)
    {
        return await employeeService.CreateEmployee(request);
    }

    [HttpGet("{resourceId:guid}")]
    [EnableRateLimiting(EmployeeRateLimitPolicy.GetById)]
    public async Task<ApiResponse<EmployeeResponse>> GetEmployeeById(
     [FromRoute] Guid resourceId)
    {
        return await employeeService.GetEmployeeByResourceId(resourceId);
    }
}
