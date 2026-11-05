using DotNetApiTemplate.Models.Response;
using SupermarketERP.Models.Request.Sales;
using SupermarketERP.Models.Response.Sales;

namespace SupermarketERP.Interfaces
{
    public interface ISaleService
    {
        Task<ApiResponse<SaleResponse>> CreateSaleDraft(Guid branchId);

        Task<ApiResponse<SaleResponse>> CreateSaleItem(Guid saleId, AddSaleItemRequest request);
    }
}
