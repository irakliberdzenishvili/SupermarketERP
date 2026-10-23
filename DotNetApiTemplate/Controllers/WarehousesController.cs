using DotNetApiTemplate.Models.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SupermarketERP.Constants;
using SupermarketERP.Interfaces;
using SupermarketERP.Models.Data;
using SupermarketERP.Models.Request.Warehouse;
using SupermarketERP.Models.Response.Warehouse;
using SupermarketERP.Security;

namespace SupermarketERP.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class WarehousesController(
    IWarehouseService warehouseService) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = Roles.Admin)]
    [EnableRateLimiting(WarehouseRateLimitPolicy.Get)]
    public async Task<PagedApiResponse<WarehouseResponse>> GetWarehouses(
        [FromQuery] string? searchTerm,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        return await warehouseService.GetWarehouses(
            searchTerm, pageNumber, pageSize);
    }

    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    [EnableRateLimiting(WarehouseRateLimitPolicy.Create)]
    public async Task<ApiResponse<WarehouseResponse>> AddWarehouse(
        [FromBody] CreateWarehouseRequest request)
    {
        return await warehouseService.CreateWarehouse(request);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.WarehouseManager}")]
    [EnableRateLimiting(WarehouseRateLimitPolicy.Get)]
    public async Task<ApiResponse<WarehouseResponse>> GetWarehouseById(
    [FromRoute] Guid id)
    {
        return await warehouseService.GetWarehouseById(id);
    }

    [HttpGet("{code}")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.WarehouseManager}")]
    [EnableRateLimiting(WarehouseRateLimitPolicy.Get)]
    public async Task<ApiResponse<WarehouseResponse>> GetWarehouseByCode(
    [FromRoute] string code)
    {
        return await warehouseService.GetWarehouseByCode(code);
    }

    [HttpPatch("{id:guid}/status")]
    [EnableRateLimiting(WarehouseRateLimitPolicy.Update)]
    public async Task<ApiResponse<WarehouseResponse>> ChangeWarehouseStatus(
    [FromRoute] Guid id,
    [FromBody] ChangeWarehouseStatusRequest request)
    {
        return await warehouseService.ChangeWarehouseStatus(id, request);
    }
}