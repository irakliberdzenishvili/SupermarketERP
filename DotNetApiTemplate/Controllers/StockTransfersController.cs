using DotNetApiTemplate.Models.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SupermarketERP.Constants;
using SupermarketERP.Interfaces;
using SupermarketERP.Models.Request.Stock;
using SupermarketERP.Models.Response.Stock;

namespace SupermarketERP.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class StockTransfersController(IStockTransferService stockTransferService) : ControllerBase
    {
        [HttpPost]
        [Authorize(Roles = $"{Roles.Admin},{Roles.BranchManager}")]
        public async Task<ApiResponse<StockTransferResponse>> CreateStockTransfer([FromBody] CreateStockTransferRequest request)
        {
            return await stockTransferService.CreateStockTransfer(request);
        }

        [HttpPatch("{transferId:guid}/approve")]
        [Authorize(Roles = $"{Roles.Admin},{Roles.WarehouseManager}")]
        public async Task<ApiResponse<StockTransferResponse>> CreateStockTransfer([FromRoute] Guid transferId)
        {
            return await stockTransferService.ApproveStockTransfer(transferId);
        }
    }
}
