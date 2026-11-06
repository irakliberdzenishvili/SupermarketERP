using DotNetApiTemplate.Models.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SupermarketERP.Constants;
using SupermarketERP.Interfaces;
using SupermarketERP.Models.Request.Sales;
using SupermarketERP.Models.Response.Sales;
using SupermarketERP.Security;

namespace SupermarketERP.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class SalesController(ISaleService saleService) : ControllerBase
    {
        [HttpPost("{branchId:guid}")]
        [Authorize(Roles = $"{Roles.Admin},{Roles.BranchManager},{Roles.Cashier}")]
        [EnableRateLimiting(SalesRateLimitPolicy.Create)]
        public async Task<ApiResponse<SaleResponse>> CreateSaleDraft([FromRoute] Guid branchId)
        {
            return await saleService.CreateSaleDraft(branchId);
        }

        [HttpPost("{saleId:guid}/items")]
        [Authorize(Roles = $"{Roles.Admin},{Roles.BranchManager},{Roles.Cashier}")]
        [EnableRateLimiting(SalesRateLimitPolicy.AddItem)]
        public async Task<ApiResponse<SaleResponse>> CreateSaleItem(
            [FromRoute] Guid saleId,
            [FromBody] AddSaleItemRequest request)
        {
            return await saleService.CreateSaleItem(saleId, request);
        }

        [HttpPut("{saleId:guid}/items/{itemId:guid}")]
        [Authorize(Roles = $"{Roles.Admin},{Roles.BranchManager},{Roles.Cashier}")]
        [EnableRateLimiting(SalesRateLimitPolicy.UpdateItem)]
        public async Task<ApiResponse<SaleResponse>> UpdateSaleItem(
            [FromRoute] Guid saleId,
            [FromRoute] Guid itemId,
            [FromBody] UpdateSaleItemRequest request)
        {
            return await saleService.UpdateSaleItem(saleId, itemId, request);
        }

        [HttpDelete("{saleId:guid}/items/{itemId:guid}")]
        [Authorize(Roles = $"{Roles.Admin},{Roles.BranchManager},{Roles.Cashier}")]
        [EnableRateLimiting(SalesRateLimitPolicy.DeleteItem)]
        public async Task<ApiResponse<SaleResponse>> DeleteSaleItem(
            [FromRoute] Guid saleId,
            [FromRoute] Guid itemId)
        {
            return await saleService.DeleteSaleItem(saleId, itemId);
        }

        [HttpPost("{saleId:guid}/payments")]
        [Authorize(Roles = $"{Roles.Admin},{Roles.BranchManager},{Roles.Cashier}")]
        [EnableRateLimiting(SalesRateLimitPolicy.Payment)]
        public async Task<ApiResponse<SaleResponse>> CreatePayment(
            [FromRoute] Guid saleId,
            [FromBody] CreatePaymentRequest request)
        {
            return await saleService.CreatePayment(saleId, request);
        }

        [HttpGet]
        [Authorize(Roles = Roles.Admin)]
        [EnableRateLimiting(SalesRateLimitPolicy.Get)]
        public async Task<PagedApiResponse<SaleResponse>> GetSales(
            [FromQuery] GetSalesRequest request,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            return await saleService.GetSales(request, pageNumber, pageSize);
        }

        [HttpGet("{saleId:guid}")]
        [Authorize(Roles = $"{Roles.Admin},{Roles.BranchManager},{Roles.Cashier}")]
        [EnableRateLimiting(SalesRateLimitPolicy.GetById)]
        public async Task<ApiResponse<SaleResponse>> GetSaleById([FromRoute] Guid saleId)
        {
            return await saleService.GetSaleById(saleId);
        }

        [HttpPatch("{saleId:guid}/cancel")]
        [Authorize(Roles = $"{Roles.Admin},{Roles.BranchManager},{Roles.Cashier}")]
        [EnableRateLimiting(SalesRateLimitPolicy.Cancel)]
        public async Task<ApiResponse<SaleResponse>> CancelSale([FromRoute] Guid saleId)
        {
            return await saleService.CancelSale(saleId);
        }

        [HttpGet("{saleId:guid}/receipt")]
        [Authorize(Roles = $"{Roles.Admin},{Roles.BranchManager},{Roles.Cashier}")]
        [EnableRateLimiting(SalesRateLimitPolicy.Receipt)]
        public async Task<ApiResponse<SaleResponse>> GetReceipt([FromRoute] Guid saleId)
        {
            return await saleService.GetReceipt(saleId);
        }
    }
}