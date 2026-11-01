using DotNetApiTemplate.Models.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SupermarketERP.Constants;
using SupermarketERP.Interfaces;
using SupermarketERP.Models.Request;
using SupermarketERP.Models.Request.Inventory;
using SupermarketERP.Models.Response;

namespace SupermarketERP.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class InventoryController(IInventoryService inventoryService) : ControllerBase
    {

        [HttpGet]
        [Authorize(Roles = Roles.Admin)]
        public async Task<PagedApiResponse<InventoryResponse>> GetInventories(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            return await inventoryService.GetInventories(pageNumber, pageSize);
        }

        [HttpGet("warehouses/{warehouseId:guid}")]
        [Authorize(Roles = $"{Roles.Admin},{Roles.WarehouseManager}")]
        public async Task<PagedApiResponse<InventoryResponse>> GetInventoriesByWarehouse(
            [FromRoute] Guid warehouseId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            return await inventoryService.GetInventoriesByWarehouse(warehouseId, pageNumber, pageSize);
        }

        [HttpGet("branches/{branchId:guid}")]
        [Authorize(Roles = $"{Roles.Admin},{Roles.BranchManager}")]
        public async Task<PagedApiResponse<InventoryResponse>> GetInventoriesByBranch(
            [FromRoute] Guid branchId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            return await inventoryService.GetInventoriesByBranch(branchId, pageNumber, pageSize);
        }

        [HttpGet("{inventoryId:guid}")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<ApiResponse<InventoryResponse>> GetInventoryById([FromRoute] Guid inventoryId)
        {
            return await inventoryService.GetInventoryById(inventoryId);
        }

        [HttpGet("warehouses/{warehouseId:guid}/{inventoryId:guid}")]
        [Authorize(Roles = $"{Roles.Admin},{Roles.WarehouseManager}")]
        public async Task<ApiResponse<InventoryResponse>> GetWarehouseInventoryById(
            [FromRoute] Guid warehouseId,
            [FromRoute] Guid inventoryId)
        {
            return await inventoryService.GetWarehouseInventoryById(warehouseId, inventoryId);
        }

        [HttpGet("branches/{branchId:guid}/{inventoryId:guid}")]
        [Authorize(Roles = $"{Roles.Admin},{Roles.BranchManager}")]
        public async Task<ApiResponse<InventoryResponse>> GetBranchInventoryById(
            [FromRoute] Guid branchId,
            [FromRoute] Guid inventoryId)
        {
            return await inventoryService.GetBranchInventoryById(branchId, inventoryId);
        }

        [HttpPost("warehouses/{warehouseId:guid}")]
        [Authorize(Roles = $"{Roles.Admin},{Roles.WarehouseManager}")]
        public async Task<ApiResponse<InventoryResponse>> CreateInventory(
            [FromRoute] Guid warehouseId,
            [FromBody] CreateInventoryRequest request)
        {
            return await inventoryService.CreateInventory(warehouseId, request);
        }

        [HttpPatch("warehouses/{warehouseId:guid}/{inventoryId:guid}/adjust")]
        [Authorize(Roles = $"{Roles.Admin},{Roles.WarehouseManager}")]
        public async Task<ApiResponse<InventoryResponse>> AdjustWarehouseInventory(
            [FromRoute] Guid warehouseId,
            [FromRoute] Guid inventoryId,
            [FromBody] AdjustInventoryRequest request)
        {
            return await inventoryService.AdjustWarehouseInventory(warehouseId, inventoryId, request);
        }

        [HttpGet("warehouses/{warehouseId:guid}/{inventoryId:guid}/adjustments")]
        [Authorize(Roles = $"{Roles.Admin},{Roles.WarehouseManager}")]
        public async Task<PagedApiResponse<InventoryAdjustmentResponse>> GetWarehouseInventoryAdjustments(
            [FromRoute] Guid warehouseId,
            [FromRoute] Guid inventoryId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            return await inventoryService.GetWarehouseInventoryAdjustments(warehouseId, inventoryId, pageNumber, pageSize);
        }


        [HttpGet("low-stock")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<PagedApiResponse<InventoryResponse>> GetLowStockInventories(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            return await inventoryService.GetLowStockInventories(pageNumber, pageSize);
        }

        [HttpGet("warehouses/{warehouseId:guid}/low-stock")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<PagedApiResponse<InventoryResponse>> GetWarehouseLowStockInventories(
            [FromRoute] Guid warehouseId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            return await inventoryService.GetWarehouseLowStockInventories(warehouseId, pageNumber, pageSize);
        }

        [HttpGet("branches/{branchId:guid}/low-stock")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<PagedApiResponse<InventoryResponse>> GetLowStockInventories(
            [FromRoute] Guid branchId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            return await inventoryService.GetBranchLowStockInventories(branchId, pageNumber, pageSize);
        }

        [HttpGet("products/{productId:guid}")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<PagedApiResponse<InventoryResponse>> GetInventoriesByProduct(
            [FromRoute] Guid productId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            return await inventoryService.GetInventoriesByProduct(productId, pageNumber, pageSize);
        }

        [HttpPatch("branches/{branchId:guid}/{inventoryId:guid}/adjust")]
        [Authorize(Roles = $"{Roles.Admin},{Roles.WarehouseManager}")]
        public async Task<ApiResponse<InventoryResponse>> AdjustBranchInventory(
            [FromRoute] Guid branchId,
            [FromRoute] Guid inventoryId,
            [FromBody] AdjustInventoryRequest request)
        {
            return await inventoryService.AdjustBranchInventory(branchId, inventoryId, request);
        }

        [HttpGet("branches/{branchId:guid}/{inventoryId:guid}/adjustments")]
        [Authorize(Roles = $"{Roles.Admin},{Roles.WarehouseManager}")]
        public async Task<PagedApiResponse<InventoryAdjustmentResponse>> GetBranchInventoryAdjustments(
            [FromRoute] Guid branchId,
            [FromRoute] Guid inventoryId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            return await inventoryService.GetBranchInventoryAdjustments(branchId, inventoryId, pageNumber, pageSize);
        }
    }
}
