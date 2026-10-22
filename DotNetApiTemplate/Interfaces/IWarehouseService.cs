using DotNetApiTemplate.Models.Response;
using SupermarketERP.Models.Request.Warehouse;
using SupermarketERP.Models.Response.Warehouse;

namespace SupermarketERP.Interfaces
{
    public interface IWarehouseService
    {
        Task<PagedApiResponse<WarehouseResponse>> GetWarehouses(string? searchTerm, int pageNumber = 1, int pageSize = 10);

        Task<ApiResponse<WarehouseResponse>> CreateWarehouse(CreateWarehouseRequest request);

        Task<ApiResponse<WarehouseResponse>> GetWarehouseById(Guid id);

        Task<ApiResponse<WarehouseResponse>> GetWarehouseByCode(string code);

    }
}
