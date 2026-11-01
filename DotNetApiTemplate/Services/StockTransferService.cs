using AutoMapper;
using DotNetApiTemplate.DB;
using DotNetApiTemplate.Helpers;
using DotNetApiTemplate.Models.Response;
using Microsoft.EntityFrameworkCore;
using SupermarketERP.Constants;
using SupermarketERP.Exceptions;
using SupermarketERP.Interfaces;
using SupermarketERP.Models.Data;
using SupermarketERP.Models.Request.Stock;
using SupermarketERP.Models.Response;
using SupermarketERP.Models.Response.Stock;

namespace SupermarketERP.Services
{
    public class StockTransferService(ApplicationDbContext db, AuthHelper authHelper, IMapper mapper) : IStockTransferService
    {
        private readonly string CurrentUserRole = authHelper.UserRole;
        private readonly string CurrentUserId = authHelper.UserId;
        private void RequireAdmin()
        {
            var isAdmin = CurrentUserRole == Roles.Admin;

            if (!isAdmin)
            {
                throw new UserFriendlyException(ErrorMessages.Forbidden);
            }
        }

        private async Task RequireWarehouseAccess(Guid warehouseId)
        {
            if (CurrentUserRole == Roles.Admin)
                return;

            if (CurrentUserRole != Roles.WarehouseManager)
                throw new UserFriendlyException(ErrorMessages.Forbidden);

            var isAssigned = await db.WarehouseManagers
                .AsNoTracking()
                .AnyAsync(wm =>
                    wm.WarehouseId == warehouseId &&
                    wm.UserId == CurrentUserId);

            if (!isAssigned)
                throw new UserFriendlyException(ErrorMessages.Forbidden);
        }

        private async Task RequireBranchAccess(Guid branchId)
        {
            if (CurrentUserRole == Roles.Admin)
                return;

            if (CurrentUserRole != Roles.BranchManager)
                throw new UserFriendlyException(ErrorMessages.Forbidden);

            var isAssigned = await db.BranchManagers
                .AsNoTracking()
                .AnyAsync(wm =>
                    wm.BranchId == branchId &&
                    wm.UserId == CurrentUserId);

            if (!isAssigned)
                throw new UserFriendlyException(ErrorMessages.Forbidden);
        }
        public async Task<ApiResponse<StockTransferResponse>> CreateStockTransfer(CreateStockTransferRequest request)
        {
            await RequireBranchAccess(request.ToBranchId);

            if (request.FromWarehouseId == Guid.Empty || request.ToBranchId == Guid.Empty || request.ProductId == Guid.Empty)
            {
                throw new UserFriendlyException(ErrorMessages.IdsAreRequired);
            }

            if (!(request.Quantity > 0))
            {
                throw new UserFriendlyException(ErrorMessages.MustBeGreaterThanZero);
            }

            var warehouseExists = await db.Warehouses.AnyAsync(w => w.Id == request.FromWarehouseId);

            if (!warehouseExists)
            {
                throw new UserFriendlyException(ErrorMessages.WarehouseNotFound);
            }

            var branchExists = await db.Branches.AnyAsync(w => w.Id == request.ToBranchId);

            if (!branchExists)
            {
                throw new UserFriendlyException(ErrorMessages.BranchNotFound);
            }

            var isCorrectWarehouse = await db.Branches.AnyAsync(b => b.Id == request.ToBranchId && b.WarehouseId == request.FromWarehouseId);

            if (!isCorrectWarehouse)
            {
                throw new UserFriendlyException(ErrorMessages.WarehouseNotAssignedToBranch);
            }

            var inventoryToTransfer = await db.Inventories.FirstOrDefaultAsync(i => i.ProductId == request.ProductId && i.WarehouseId == request.FromWarehouseId);

            if (inventoryToTransfer == null)
            {
                throw new UserFriendlyException(ErrorMessages.InventoryNotFound);
            }

            var newStockTransfer = mapper.Map<StockTransfer>(request);

            newStockTransfer.FromWarehouseId = request.FromWarehouseId;
            newStockTransfer.ToBranchId = request.ToBranchId;
            newStockTransfer.Status = Enums.StockTransferStatus.Pending;
            newStockTransfer.RequestedByUserId = CurrentUserId;

            await db.StockTransfers.AddAsync(newStockTransfer);
            await db.SaveChangesAsync();

            var mappedResponse = mapper.Map<StockTransferResponse>(newStockTransfer);

            return new ApiResponse<StockTransferResponse>
            {
                Data = mappedResponse,
                IsSuccess = true,
                Message = "Stock transfer requested successfully"
            };
        }
    }
}
