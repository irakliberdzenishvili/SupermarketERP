using DotNetApiTemplate.Models.Response;
using DotNetApiTemplate.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
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
        [HttpGet]
        [Authorize(Roles = $"{Roles.Admin},{Roles.BranchManager},{Roles.WarehouseManager}")]
        [EnableRateLimiting(StockRateLimitPolicy.GetStockTransfers)]
        public async Task<PagedApiResponse<StockTransferResponse>> GetStockTransfers([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            return await stockTransferService.GetStockTransfers(pageNumber, pageSize);
        }

        [HttpPost]
        [Authorize(Roles = $"{Roles.Admin},{Roles.BranchManager}")]
        [EnableRateLimiting(StockRateLimitPolicy.CreateStockTransfer)]
        public async Task<ApiResponse<StockTransferResponse>> CreateStockTransfer([FromBody] CreateStockTransferRequest request)
        {
            return await stockTransferService.CreateStockTransfer(request);
        }

        [HttpPatch("{transferId:guid}/approve")]
        [Authorize(Roles = $"{Roles.Admin},{Roles.WarehouseManager}")]
        [EnableRateLimiting(StockRateLimitPolicy.ApproveStockTransfer)]
        public async Task<ApiResponse<StockTransferResponse>> ApproveStockTransfer([FromRoute] Guid transferId)
        {
            return await stockTransferService.ApproveStockTransfer(transferId);
        }

        [HttpPatch("{transferId:guid}/reject")]
        [Authorize(Roles = $"{Roles.Admin},{Roles.WarehouseManager}")]
        [EnableRateLimiting(StockRateLimitPolicy.RejectStockTransfer)]
        public async Task<ApiResponse<StockTransferResponse>> RejectStockTransfer([FromRoute] Guid transferId)
        {
            return await stockTransferService.RejectStockTransfer(transferId);
        }

        [HttpPatch("{transferId:guid}/cancel")]
        [Authorize(Roles = $"{Roles.Admin},{Roles.BranchManager}")]
        [EnableRateLimiting(StockRateLimitPolicy.CancelStockTransfer)]
        public async Task<ApiResponse<StockTransferResponse>> CancelStockTransfer([FromRoute] Guid transferId)
        {
            return await stockTransferService.CancelStockTransfer(transferId);
        }

        [HttpPatch("{transferId:guid}/dispatch")]
        [Authorize(Roles = $"{Roles.Admin},{Roles.WarehouseManager}")]
        [EnableRateLimiting(StockRateLimitPolicy.DispatchStockTransfer)]
        public async Task<ApiResponse<StockTransferResponse>> DispatchStockTransfer([FromRoute] Guid transferId)
        {
            return await stockTransferService.DispatchStockTransfer(transferId);
        }

        [HttpPatch("{transferId:guid}/receive")]
        [Authorize(Roles = $"{Roles.Admin},{Roles.BranchManager}")]
        [EnableRateLimiting(StockRateLimitPolicy.ReceiveStockTransfer)]
        public async Task<ApiResponse<StockTransferResponse>> ReceiveStockTransfer([FromRoute] Guid transferId)
        {
            return await stockTransferService.ReceiveStockTransfer(transferId);
        }

        [HttpGet("{transferId:guid}")]
        [Authorize(Roles = $"{Roles.Admin},{Roles.BranchManager},{Roles.WarehouseManager}")]
        [EnableRateLimiting(StockRateLimitPolicy.GetStockTransferById)]
        public async Task<ApiResponse<StockTransferResponse>> GetStockTransferById([FromRoute] Guid transferId)
        {
            return await stockTransferService.GetStockTransferById(transferId);
        }
    }
}