using DotNetApiTemplate.Models.Response;
using SupermarketERP.Models.Request;
using SupermarketERP.Models.Response;

namespace SupermarketERP.Interfaces
{
    public interface IWarehouseService
    {
        Task<PagedApiResponse<WarehouseResponse>> GetWarehouses(string? searchTerm, int pageNumber = 1, int pageSize = 10);

        Task<ApiResponse<WarehouseResponse>> CreateWarehouse(CreateWarehouseRequest request);

        Task<ApiResponse<WarehouseResponse>> GetWarehouseById(Guid id);

        Task<ApiResponse<WarehouseResponse>> GetWarehouseByCode(string code);

        Task<ApiResponse<WarehouseResponse>> ChangeWarehouseStatus(Guid warehouseId, ChangeWarehouseStatusRequest request);

    }
}
