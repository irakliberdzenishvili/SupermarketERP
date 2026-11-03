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
        Task<ApiResponse<StockTransferResponse>> DispatchStockTransfer(Guid transferId);
        Task<ApiResponse<StockTransferResponse>> ReceiveStockTransfer(Guid transferId);
        Task<ApiResponse<StockTransferResponse>> CancelStockTransfer(Guid transferId);
        Task<ApiResponse<StockTransferResponse>> GetStockTransferById(Guid transferId);
        Task<PagedApiResponse<StockTransferResponse>> GetStockTransfers(int pageNumber = 1, int pageSize = 10);
    }
}