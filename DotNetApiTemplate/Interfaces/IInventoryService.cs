using DotNetApiTemplate.Models.Response;
using SupermarketERP.Models.Request;
using SupermarketERP.Models.Request.Inventory;
using SupermarketERP.Models.Response;

namespace SupermarketERP.Interfaces
{
    public interface IInventoryService
    {
        Task<PagedApiResponse<InventoryResponse>> GetInventories(int pageNumber = 1, int pageSize = 10);

        Task<PagedApiResponse<InventoryResponse>> GetInventoriesByWarehouse(Guid warehouseId, int pageNumber = 1, int pageSize = 10);

        Task<PagedApiResponse<InventoryResponse>> GetInventoriesByBranch(Guid branchId, int pageNumber = 1, int pageSize = 10);

        Task<ApiResponse<InventoryResponse>> GetInventoryById(Guid inventoryId);

        Task<ApiResponse<InventoryResponse>> GetWarehouseInventoryById(Guid warehouseId, Guid inventoryId);

        Task<ApiResponse<InventoryResponse>> GetBranchInventoryById(Guid branchId, Guid inventoryId);

        Task<ApiResponse<InventoryResponse>> CreateInventory(Guid warehouseId, CreateInventoryRequest request);

        Task<ApiResponse<InventoryResponse>> AdjustWarehouseInventory(Guid warehouseId,Guid inventoryId, AdjustInventoryRequest request);

        Task<PagedApiResponse<InventoryAdjustmentResponse>> GetWarehouseInventoryAdjustments(Guid warehouseId, Guid inventoryId, int pageNumber = 1, int pageSize = 10);
        Task<PagedApiResponse<InventoryResponse>> GetLowStockInventories(int pageNumber = 1, int pageSize = 10);
        Task<PagedApiResponse<InventoryResponse>> GetWarehouseLowStockInventories(Guid warehouseId, int pageNumber = 1, int pageSize = 10);
        Task<PagedApiResponse<InventoryResponse>> GetBranchLowStockInventories(Guid branchId, int pageNumber = 1, int pageSize = 10);

        Task<PagedApiResponse<InventoryResponse>> GetInventoriesByProduct(Guid productId, int pageNumber = 1, int pageSize = 10);

        Task<ApiResponse<InventoryResponse>> AdjustBranchInventory(Guid branchId, Guid inventoryId, AdjustInventoryRequest request);

        Task<PagedApiResponse<InventoryAdjustmentResponse>> GetBranchInventoryAdjustments(Guid warehouseId, Guid inventoryId, int pageNumber = 1, int pageSize = 10);



    }
}
