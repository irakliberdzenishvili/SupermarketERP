using AutoMapper;
using DotNetApiTemplate.DB;
using DotNetApiTemplate.Helpers;
using DotNetApiTemplate.Models.Response;
using Microsoft.EntityFrameworkCore;
using SupermarketERP.Constants;
using SupermarketERP.Enums;
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
            if (request.FromWarehouseId == Guid.Empty || request.ToBranchId == Guid.Empty || request.ProductId == Guid.Empty)
            {
                throw new UserFriendlyException(ErrorMessages.IdsAreRequired);
            }

            await RequireBranchAccess(request.ToBranchId);


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

        public async Task<ApiResponse<StockTransferResponse>> ApproveStockTransfer(Guid transferId)
        {
            if (transferId == Guid.Empty) { 
                throw new UserFriendlyException(ErrorMessages.IdsAreRequired);
            }

            var transfer = await db.StockTransfers.FirstOrDefaultAsync(t => t.Id == transferId);

            if (transfer == null)
            {
                throw new UserFriendlyException(ErrorMessages.TransferNotFound);
            }

            await RequireWarehouseAccess(transfer.FromWarehouseId);

            if (transfer.Status != Enums.StockTransferStatus.Pending)
            {
                throw new UserFriendlyException(ErrorMessages.TransferMustBePending);
            }

            var warehouse = await db.Warehouses.FirstOrDefaultAsync(w => w.Id == transfer.FromWarehouseId);

            if (warehouse == null)
            {
                throw new UserFriendlyException(ErrorMessages.WarehouseNotFound);
            }
            var transaction = await db.Database.BeginTransactionAsync();

            try
            {

            var inventory = await db.Inventories.FirstOrDefaultAsync(i => i.BranchId == transfer.ToBranchId && i.ProductId == transfer.ProductId);

 
            // create a new inventory record for branch 
            if (inventory == null)
            {
                var inventoryRecordForBranch = new Inventory()
                {
                    BranchId = transfer.ToBranchId,
                    ProductId = transfer.ProductId,
                    Quantity = transfer.Quantity,
                    MinimumQuantity = 20 
                };

                await db.Inventories.AddAsync(inventoryRecordForBranch);
                inventory = inventoryRecordForBranch;
            }
            else
            {
                inventory.Quantity += transfer.Quantity;
            }
            

            var inventoryWarehouse = await db.Inventories.FirstOrDefaultAsync(w => w.WarehouseId == transfer.FromWarehouseId && w.ProductId == transfer.ProductId);

            if (inventoryWarehouse == null)
            {
                throw new UserFriendlyException(ErrorMessages.InventoryNotFound);
            }
            
              if (inventoryWarehouse.Quantity < transfer.Quantity)
              {
                throw new UserFriendlyException(ErrorMessages.NotEnoughMaterialForTransfer);
              }

              if ((inventoryWarehouse.Quantity - transfer.Quantity) < 0)
                {
                    throw new UserFriendlyException(ErrorMessages.AfterTransferingWarehouseQuantityIsBelowZero);
                }


                var affectedRows = await db.StockTransfers
               .Where(t => t.Id == transferId &&
                t.Status == StockTransferStatus.Pending)
                    .ExecuteUpdateAsync(setter => setter
                    .SetProperty(t => t.Status, Enums.StockTransferStatus.Approved)
                    .SetProperty(t => t.ApprovedByUserId, CurrentUserId)
                    .SetProperty(t => t.ApprovedAt, DateTime.UtcNow));

                if (affectedRows == 0)
                {
                    throw new UserFriendlyException(ErrorMessages.TransferMustBePending);
                }


            
            inventoryWarehouse.Quantity -= transfer.Quantity;

            await db.SaveChangesAsync();
            await transaction.CommitAsync();
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }

            await db.Entry(transfer).ReloadAsync();

            var mappedResponse = mapper.Map<StockTransferResponse>(transfer);

            return new ApiResponse<StockTransferResponse>
            {
                Data = mappedResponse,
                IsSuccess = true,
                Message = "Stock transfer approved successfully"
            };
        }

        public async Task<ApiResponse<StockTransferResponse>> RejectStockTransfer(Guid transferId)
        {
            if (transferId == Guid.Empty)
            {
                throw new UserFriendlyException(ErrorMessages.IdsAreRequired);
            }

            var transfer = await db.StockTransfers.FirstOrDefaultAsync(t => t.Id == transferId);

            if (transfer == null)
            {
                throw new UserFriendlyException(ErrorMessages.TransferNotFound);
            }

            await RequireWarehouseAccess(transfer.FromWarehouseId);

            if (transfer.Status != Enums.StockTransferStatus.Pending)
            {
                throw new UserFriendlyException(ErrorMessages.TransferMustBePending);
            }

            var warehouse = await db.Warehouses.FirstOrDefaultAsync(w => w.Id == transfer.FromWarehouseId);

            if (warehouse == null)
            {
                throw new UserFriendlyException(ErrorMessages.WarehouseNotFound);
            }

            var affectedRows = await db.StockTransfers
               .Where(t => t.Id == transferId &&
                t.Status == StockTransferStatus.Pending)
                    .ExecuteUpdateAsync(setter => setter
                    .SetProperty(t => t.Status, Enums.StockTransferStatus.Rejected)
                    .SetProperty(t => t.RejectedByUserId, CurrentUserId)
                    .SetProperty(t => t.RejectedAt, DateTime.UtcNow));

            if (affectedRows == 0)
            {
                throw new UserFriendlyException(ErrorMessages.TransferMustBePending);
            }

            await db.Entry(transfer).ReloadAsync();

            var mappedResponse = mapper.Map<StockTransferResponse>(transfer);

            return new ApiResponse<StockTransferResponse>
            {
                Data = mappedResponse,
                IsSuccess = true,
                Message = "Stock transfer rejected successfully"
            };
        }

        public async Task<ApiResponse<StockTransferResponse>> CancelStockTransfer(Guid transferId)
        {
            if (transferId == Guid.Empty)
            {
                throw new UserFriendlyException(ErrorMessages.IdsAreRequired);
            }

            var transfer = await db.StockTransfers.FirstOrDefaultAsync(t => t.Id == transferId);

            if (transfer == null)
            {
                throw new UserFriendlyException(ErrorMessages.TransferNotFound);
            }

            await RequireBranchAccess(transfer.ToBranchId);

            if (transfer.Status != Enums.StockTransferStatus.Pending)
            {
                throw new UserFriendlyException(ErrorMessages.TransferMustBePending);
            }

            var warehouse = await db.Warehouses.FirstOrDefaultAsync(w => w.Id == transfer.FromWarehouseId);

            if (warehouse == null)
            {
                throw new UserFriendlyException(ErrorMessages.WarehouseNotFound);
            }

            var affectedRows = await db.StockTransfers
               .Where(t => t.Id == transferId &&
                t.Status == StockTransferStatus.Pending)
                    .ExecuteUpdateAsync(setter => setter
                    .SetProperty(t => t.Status, Enums.StockTransferStatus.Cancelled)
                    .SetProperty(t => t.CancelledByUserId, CurrentUserId)
                    .SetProperty(t => t.CancelledAt, DateTime.UtcNow));

            if (affectedRows == 0)
            {
                throw new UserFriendlyException(ErrorMessages.TransferMustBePending);
            }

            await db.Entry(transfer).ReloadAsync();

            var mappedResponse = mapper.Map<StockTransferResponse>(transfer);

            return new ApiResponse<StockTransferResponse>
            {
                Data = mappedResponse,
                IsSuccess = true,
                Message = "Stock transfer cancelled successfully"
            };
        }
    }
}
