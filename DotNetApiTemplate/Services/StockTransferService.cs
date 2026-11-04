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
using System.Security.Cryptography.Xml;

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

        private void DenyAccess()
        {
            if (CurrentUserRole != Roles.Admin &&
                CurrentUserRole != Roles.BranchManager &&
                CurrentUserRole != Roles.WarehouseManager)
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

        public async Task<PagedApiResponse<StockTransferResponse>> GetStockTransfers(int pageNumber = 1, int pageSize = 10)
        {
            DenyAccess();

            if (pageNumber <= 0 || pageSize <= 0 || pageSize > 100)
                throw new UserFriendlyException(ErrorMessages.InvalidPagination);

            var query = db.StockTransfers.AsNoTracking();


            // admin has access to all stock transfers

            if (CurrentUserRole == Roles.BranchManager)
            {
                var BranchManager = await db.BranchManagers.FirstOrDefaultAsync(b => b.UserId == CurrentUserId);

                if (BranchManager == null)
                {
                    throw new UserFriendlyException(ErrorMessages.BranchManagerNotFound);
                }

                query = query.Where(q => q.ToBranchId == BranchManager.BranchId);
            }

            if (CurrentUserRole == Roles.WarehouseManager)
            {
                var WarehouseManager = await db.WarehouseManagers.FirstOrDefaultAsync(b => b.UserId == CurrentUserId);

                if (WarehouseManager == null)
                {
                    throw new UserFriendlyException(ErrorMessages.WarehouseManagerNotFound);
                }

                query = query.Where(q => q.FromWarehouseId == WarehouseManager.WarehouseId);
            }

            var totalCount = await query.CountAsync();

            var pagedQuery = await query
                            .OrderByDescending(q => q.CreatedAt)
                            .ThenByDescending(q => q.Id)
                            .Skip((pageNumber - 1) * pageSize)
                            .Take(pageSize)
                            .ToListAsync();

            var mappedResponse = mapper.Map<List<StockTransferResponse>>(pagedQuery);

            return new PagedApiResponse<StockTransferResponse>(
                mappedResponse, totalCount, pageSize, pageNumber)
            {
                IsSuccess = true,
                Message = "Stock transfers retrieved"
            };
        }

        public async Task<ApiResponse<StockTransferResponse>> DispatchStockTransfer(Guid transferId)
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

            if (transfer.Status != Enums.StockTransferStatus.Approved)
            {
                throw new UserFriendlyException(ErrorMessages.TransferMustBeApproved);
            }

            var inventoryWarehouse = await db.Inventories
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.WarehouseId == transfer.FromWarehouseId && i.ProductId == transfer.ProductId);

            if (inventoryWarehouse == null)
            {
                throw new UserFriendlyException(ErrorMessages.InventoryNotFound);
            }


            var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);

            try
            {

                var inventoryWarehouseAffectedRows = await db.Inventories
                       .Where(i => i.Id == inventoryWarehouse.Id && i.Quantity >= transfer.Quantity)
                       .ExecuteUpdateAsync(setters => setters
                       .SetProperty(p => p.Quantity, i => i.Quantity - transfer.Quantity)
                       );

                if (inventoryWarehouseAffectedRows == 0)
                {
                    throw new UserFriendlyException(ErrorMessages.NotEnoughMaterialForTransfer);
                }

                var stockTransferAffectedRows = await db.StockTransfers
               .Where(t => t.Id == transferId &&
                t.Status == StockTransferStatus.Approved)
                    .ExecuteUpdateAsync(setter => setter
                    .SetProperty(t => t.Status, Enums.StockTransferStatus.Dispatched)
                    .SetProperty(t => t.DispatchedByUserId, CurrentUserId)
                    .SetProperty(t => t.DispatchedAt, DateTime.UtcNow));

            if (stockTransferAffectedRows == 0)
            {
                throw new UserFriendlyException(ErrorMessages.TransferMustBeApproved);
            }

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
                Message = "Stock transfer dispatched successfully"
            };
        }

        public async Task<ApiResponse<StockTransferResponse>> ReceiveStockTransfer(Guid transferId)
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

            if (transfer.Status != Enums.StockTransferStatus.Dispatched)
            {
                throw new UserFriendlyException(ErrorMessages.TransferMustBeDispatched);
            }

            var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);


            try 
            { 

            var inventoryBranch = await db.Inventories
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.BranchId == transfer.ToBranchId && i.ProductId == transfer.ProductId);


            // if branch inventory is not available, create a new one
            if (inventoryBranch == null)
            {
                var inventoryBranchRecord = new Inventory()
                {
                    BranchId = transfer.ToBranchId,
                    ProductId = transfer.ProductId,
                    Quantity = transfer.Quantity,
                    MinimumQuantity = 20,
                    
                };

                await db.AddAsync(inventoryBranchRecord);
                await db.SaveChangesAsync();

                inventoryBranch = inventoryBranchRecord;
            }
            

                var inventoryBranchAffectedRows = await db.Inventories
                       .Where(i => i.Id == inventoryBranch.Id)
                       .ExecuteUpdateAsync(setters => setters
                       .SetProperty(p => p.Quantity, i => i.Quantity + transfer.Quantity)
                       );

                if (inventoryBranchAffectedRows == 0)
                {
                    throw new UserFriendlyException(ErrorMessages.FailedToUpdateInventory);
                }


                var stockTransferAffectedRows = await db.StockTransfers
               .Where(t => t.Id == transferId &&
                t.Status == StockTransferStatus.Dispatched)
                    .ExecuteUpdateAsync(setter => setter
                    .SetProperty(t => t.Status, Enums.StockTransferStatus.Received)
                    .SetProperty(t => t.ReceivedByUserId, CurrentUserId)
                    .SetProperty(t => t.ReceivedAt, DateTime.UtcNow));

                if (stockTransferAffectedRows == 0)
                {
                    throw new UserFriendlyException(ErrorMessages.TransferMustBeDispatched);
                }

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
                Message = "Stock transfer recieved successfully"
            };
        }

        public async Task<ApiResponse<StockTransferResponse>> GetStockTransferById(Guid transferId)
        {

            if (transferId == Guid.Empty)
            {
                throw new UserFriendlyException(ErrorMessages.IdsAreRequired);
            }

            DenyAccess();


            var query = db.StockTransfers.AsNoTracking().Where(t => t.Id == transferId);

            // admin has access to all stock transfers

            if (CurrentUserRole == Roles.BranchManager)
            {
                var BranchManager = await db.BranchManagers.FirstOrDefaultAsync(b => b.UserId == CurrentUserId);

                if (BranchManager == null)
                {
                    throw new UserFriendlyException(ErrorMessages.BranchManagerNotFound);
                }

                query = query.Where(q => q.ToBranchId == BranchManager.BranchId);
            }

            if (CurrentUserRole == Roles.WarehouseManager)
            {
                var WarehouseManager = await db.WarehouseManagers.FirstOrDefaultAsync(b => b.UserId == CurrentUserId);

                if (WarehouseManager == null)
                {
                    throw new UserFriendlyException(ErrorMessages.WarehouseManagerNotFound);
                }

                query = query.Where(q => q.FromWarehouseId == WarehouseManager.WarehouseId);
            }

            var result = await query.FirstOrDefaultAsync();

            if (result == null)
            {
                throw new UserFriendlyException(ErrorMessages.TransferNotFound);
            }

            var mappedResponse = mapper.Map<StockTransferResponse>(result);

            return new ApiResponse<StockTransferResponse>
            {
                Data = mappedResponse,
                IsSuccess = true,
                Message = "Stock transfer retrieved successfully"
            };
        }
    }
}
