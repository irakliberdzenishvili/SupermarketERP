using DotNetApiTemplate.Models.Response;
using SupermarketERP.Models.Request.Stock;
using SupermarketERP.Models.Response.Stock;

namespace SupermarketERP.Interfaces
{
    public interface IStockTransferService
    {
        Task<ApiResponse<StockTransferResponse>> CreateStockTransfer(CreateStockTransferRequest request);
    }
}
