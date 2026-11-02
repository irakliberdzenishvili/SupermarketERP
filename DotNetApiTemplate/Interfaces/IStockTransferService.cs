using DotNetApiTemplate.Models.Response;
using SupermarketERP.Models.Request.Stock;
using SupermarketERP.Models.Response.Stock;

namespace SupermarketERP.Interfaces
{
    public interface IStockTransferService
    {
        Task<ApiResponse<StockTransferResponse>> CreateStockTransfer(CreateStockTransferRequest request);
        Task<ApiResponse<StockTransferResponse>> ApproveStockTransfer(Guid transferId);
        Task<ApiResponse<StockTransferResponse>> RejectStockTransfer(Guid transferId);
        Task<ApiResponse<StockTransferResponse>> CancelStockTransfer(Guid transferId);

    }
}
