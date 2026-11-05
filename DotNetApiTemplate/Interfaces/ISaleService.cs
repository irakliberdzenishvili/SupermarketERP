using DotNetApiTemplate.Models.Response;
using SupermarketERP.Models.Data;
using SupermarketERP.Models.Request.Sales;
using SupermarketERP.Models.Response.Sales;

namespace SupermarketERP.Interfaces
{
    public interface ISaleService
    {
        Task<ApiResponse<SaleResponse>> CreateSaleDraft(Guid branchId);

        Task<ApiResponse<SaleResponse>> CreateSaleItem(Guid saleId, AddSaleItemRequest request);

        Task<ApiResponse<SaleResponse>> UpdateSaleItem(Guid saleId, Guid itemId, UpdateSaleItemRequest request);

        Task<ApiResponse<SaleResponse>> DeleteSaleItem(Guid saleId, Guid itemId);

        Task<ApiResponse<SaleResponse>> CreatePayment(Guid saleId, CreatePaymentRequest request);

        Task<PagedApiResponse<SaleResponse>> GetSales(GetSalesRequest request, int pageNumber = 1, int pageSize = 10);
    }
}
