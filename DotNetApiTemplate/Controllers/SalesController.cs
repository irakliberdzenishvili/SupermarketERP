using DotNetApiTemplate.Models.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SupermarketERP.Constants;
using SupermarketERP.Interfaces;
using SupermarketERP.Models.Request.Sales;
using SupermarketERP.Models.Response.Sales;

namespace SupermarketERP.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class SalesController(ISaleService saleService) : ControllerBase
    {
        [HttpPost("{branchId:guid}")]
        [Authorize(Roles = $"{Roles.Admin},{Roles.BranchManager},{Roles.Cashier}")]
        public async Task<ApiResponse<SaleResponse>> CreateSaleDraft([FromRoute] Guid branchId)
        {
            return await saleService.CreateSaleDraft(branchId);
        }

        [HttpPost("{saleId:guid}/items")]
        [Authorize(Roles = $"{Roles.Admin},{Roles.BranchManager},{Roles.Cashier}")]
        public async Task<ApiResponse<SaleResponse>> CreateSaleItem([FromRoute] Guid saleId, [FromBody] AddSaleItemRequest request)
        {
            return await saleService.CreateSaleItem(saleId, request);
        }

        [HttpPut("{saleId:guid}/items/{itemId:guid}")]
        [Authorize(Roles = $"{Roles.Admin},{Roles.BranchManager},{Roles.Cashier}")]
        public async Task<ApiResponse<SaleResponse>> CreateSaleItem([FromRoute] Guid saleId, [FromRoute] Guid itemId, [FromBody] UpdateSaleItemRequest request)
        {
            return await saleService.UpdateSaleItem(saleId, itemId, request);
        }

        [HttpDelete("{saleId:guid}/items/{itemId:guid}")]
        [Authorize(Roles = $"{Roles.Admin},{Roles.BranchManager},{Roles.Cashier}")]
        public async Task<ApiResponse<SaleResponse>> DeleteSaleItem([FromRoute] Guid saleId, [FromRoute] Guid itemId)
        {
            return await saleService.DeleteSaleItem(saleId, itemId);
        }

        [HttpPost("{saleId:guid}/payments")]
        [Authorize(Roles = $"{Roles.Admin},{Roles.BranchManager},{Roles.Cashier}")]
        public async Task<ApiResponse<SaleResponse>> CreatePayment([FromRoute] Guid saleId, [FromBody] CreatePaymentRequest request)
        {
            return await saleService.CreatePayment(saleId, request);
        }

        [HttpGet]
        [Authorize(Roles = Roles.Admin)]
        public async Task<PagedApiResponse<SaleResponse>> CreatePayment([FromQuery] GetSalesRequest request, int pageNumber = 1, int pageSize = 10)
        {
            return await saleService.GetSales(request, pageNumber, pageSize);
        }
    }
}
