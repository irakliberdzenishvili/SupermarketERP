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


    }
}
