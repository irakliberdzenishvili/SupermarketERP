using AutoMapper;
using DotNetApiTemplate.DB;
using DotNetApiTemplate.Helpers;
using DotNetApiTemplate.Models.Response;
using Microsoft.EntityFrameworkCore;
using SupermarketERP.Constants;
using SupermarketERP.Exceptions;
using SupermarketERP.Interfaces;
using SupermarketERP.Models.Data;
using SupermarketERP.Models.Request;
using SupermarketERP.Models.Request.Inventory;
using SupermarketERP.Models.Response;

namespace SupermarketERP.Services
{
    public class InventoryService(ApplicationDbContext db, AuthHelper authHelper, IMapper mapper) : IInventoryService
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

        public async Task<PagedApiResponse<InventoryResponse>> GetInventories(int pageNumber = 1, int pageSize = 10)
        {
            RequireAdmin();

            if (pageNumber <= 0 || pageSize <= 0 || pageSize > 100)
                throw new UserFriendlyException(ErrorMessages.InvalidPagination);

            var query = db.Inventories.AsNoTracking();

            var pagedResponse = await query
                .OrderBy(p => p.Id)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var totalCount = await query.CountAsync();

            var mappedResponse = mapper.Map<List<InventoryResponse>>(pagedResponse);

            return new PagedApiResponse<InventoryResponse>(
                mappedResponse, totalCount, pageSize, pageNumber)
            {
                IsSuccess = true,
                Message = "Inventories Retrieved"
            };
        }

        public async Task<PagedApiResponse<InventoryResponse>> GetInventoriesByWarehouse(Guid warehouseId, int pageNumber = 1, int pageSize = 10)
        {
            await RequireWarehouseAccess(warehouseId);

            if (pageNumber <= 0 || pageSize <= 0 || pageSize > 100)
               throw new UserFriendlyException(ErrorMessages.InvalidPagination);

            var warehouseExists = await db.Warehouses
                .AsNoTracking()
                .AnyAsync(w => w.Id == warehouseId);

            if (!warehouseExists)
                throw new UserFriendlyException(ErrorMessages.WarehouseNotFound);

            var query = db.Inventories
                .AsNoTracking()
                .Where(i => i.WarehouseId == warehouseId);

            var totalCount = await query.CountAsync();

            var pagedResponse = await query
                .OrderBy(i => i.Id)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var mappedResponse = mapper.Map<List<InventoryResponse>>(pagedResponse);

            return new PagedApiResponse<InventoryResponse>(
                mappedResponse, totalCount, pageSize, pageNumber)
            {
                IsSuccess = true,
                Message = "Warehouse Inventories Retrieved"
            };
        }

        public async Task<PagedApiResponse<InventoryResponse>> GetInventoriesByBranch(Guid branchId, int pageNumber = 1, int pageSize = 10)
        {
            await RequireBranchAccess(branchId);

            if (pageNumber <= 0 || pageSize <= 0 || pageSize > 100)
                throw new UserFriendlyException(ErrorMessages.InvalidPagination);

            var branchExists = await db.Branches
                .AsNoTracking()
                .AnyAsync(w => w.Id == branchId);

            if (!branchExists)
                throw new UserFriendlyException(ErrorMessages.BranchNotFound);

            var query =  db.Inventories
                .AsNoTracking()
                .Where(i => i.BranchId == branchId);

            var totalCount = await query.CountAsync();

            var pagedResponse = await query
                .OrderBy(i => i.Id)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var mappedResponse = mapper.Map<List<InventoryResponse>>(pagedResponse);

            return new PagedApiResponse<InventoryResponse>(
                mappedResponse, totalCount, pageSize, pageNumber)
            {
                IsSuccess = true,
                Message = "Branch Inventories Retrieved"
            };
        }

        public async Task<ApiResponse<InventoryResponse>> GetInventoryById(Guid inventoryId)
        {
            RequireAdmin();

            var inventory = await db.Inventories
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.Id == inventoryId);

            if (inventory == null)
            {
                throw new UserFriendlyException(ErrorMessages.InventoryNotFound);
            }

            var mappedResponse = mapper.Map<InventoryResponse>(inventory);

            return new ApiResponse<InventoryResponse>
            {
                Data = mappedResponse,
                IsSuccess = true,
                Message = "Inventory Found"
            };
        }

        public async Task<ApiResponse<InventoryResponse>> GetWarehouseInventoryById(Guid warehouseId, Guid inventoryId)
        {
            await RequireWarehouseAccess(warehouseId);

            var warehouseExists = await db.Warehouses
                .AsNoTracking()
                .AnyAsync(w => w.Id == warehouseId);

            if (!warehouseExists)
            {
                throw new UserFriendlyException(ErrorMessages.WarehouseNotFound);
            }

            var inventory = await db.Inventories
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.Id == inventoryId && i.WarehouseId == warehouseId);

            if (inventory == null)
            {
                throw new UserFriendlyException(ErrorMessages.InventoryNotFound);
            }

            var mappedResponse = mapper.Map<InventoryResponse>(inventory);

            return new ApiResponse<InventoryResponse>
            {
                Data = mappedResponse,
                IsSuccess = true,
                Message = "Warehouse Inventory Found"
            };
        }

        public async Task<ApiResponse<InventoryResponse>> GetBranchInventoryById(Guid branchId, Guid inventoryId)
        {
            await RequireBranchAccess(branchId);

            var warehouseExists = await db.Branches
                .AsNoTracking()
                .AnyAsync(w => w.Id == branchId);

            if (!warehouseExists)
            {
                throw new UserFriendlyException(ErrorMessages.BranchNotFound);
            }

            var inventory = await db.Inventories
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.Id == inventoryId && i.BranchId == branchId);

            if (inventory == null)
            {
                throw new UserFriendlyException(ErrorMessages.InventoryNotFound);
            }

            var mappedResponse = mapper.Map<InventoryResponse>(inventory);

            return new ApiResponse<InventoryResponse>
            {
                Data = mappedResponse,
                IsSuccess = true,
                Message = "Branch Inventory Found"
            };
        }

        public async Task<ApiResponse<InventoryResponse>> CreateInventory(Guid warehouseId, CreateInventoryRequest request)
        {
            await RequireWarehouseAccess(warehouseId);

            if (request.Quantity < 0 || request.MinimumQuantity < 0)
            {
                throw new UserFriendlyException(ErrorMessages.MustBeGreaterThanZero);
            }

            var warehouseExists = await db.Warehouses
                .AsNoTracking()
                .AnyAsync(w => w.Id == warehouseId);

            if (!warehouseExists)
            {
                throw new UserFriendlyException(ErrorMessages.WarehouseNotFound);
            }

            var productExists = await db.Products
                .AsNoTracking()
                .AnyAsync(p => p.Id == request.ProductId);

            if (!productExists)
            {
                throw new UserFriendlyException(ErrorMessages.ProductNotFound);
            }

            var inventoryProductExists = await db.Inventories
                .AnyAsync(i => i.ProductId == request.ProductId && i.WarehouseId == warehouseId);

            if (inventoryProductExists)
            {
                throw new UserFriendlyException(ErrorMessages.ProductInventoryAlreadyExists);
            }


            var newInventory = mapper.Map<Inventory>(request);

            newInventory.WarehouseId = warehouseId;

            await db.Inventories.AddAsync(newInventory);
            await db.SaveChangesAsync();

            var mappedResponse = mapper.Map<InventoryResponse>(newInventory);

            return new ApiResponse<InventoryResponse>
            {
                Data = mappedResponse,
                IsSuccess = true,
                Message = "Warehouse Inventory Created"
            };
        }

        public async Task<ApiResponse<InventoryResponse>> AdjustWarehouseInventory(Guid warehouseId, Guid inventoryId, AdjustInventoryRequest request)
        {
            await RequireWarehouseAccess(warehouseId);

            if (request.QuantityChange == 0)
            {
                throw new UserFriendlyException(ErrorMessages.MustBeNegativeOrPositive);
            }


            if (string.IsNullOrWhiteSpace(request.Reason))
            {
                throw new UserFriendlyException(ErrorMessages.ReasonIsRequiredForAdjustment);
            }

            var warehouseExists = await db.Warehouses
                .AsNoTracking()
                .AnyAsync(w => w.Id == warehouseId);

            if (!warehouseExists)
            {
                throw new UserFriendlyException(ErrorMessages.WarehouseNotFound);
            }

            var inventory = await db.Inventories
                .FirstOrDefaultAsync(i => i.Id == inventoryId && i.WarehouseId == warehouseId);

            if (inventory == null)
            {
                throw new UserFriendlyException(ErrorMessages.InventoryNotFound);
            }

            var adjustedInventoryHistory = mapper.Map<InventoryAdjustment>(request);

            var newQuantity = 0;

            try
            {

            checked
            {

                newQuantity = inventory.Quantity + request.QuantityChange;

                if (newQuantity < 0)
                {
                    throw new UserFriendlyException(ErrorMessages.AfterAdjustingQuantityIsNegativeItMustBePositiveOrZero);
                }
            }

            }
            catch (OverflowException ex)
            {
                throw new UserFriendlyException(ErrorMessages.IntegerOverflow);
            }
            

            adjustedInventoryHistory.InventoryId = inventoryId;
            adjustedInventoryHistory.PerformedByUserId = CurrentUserId;

            inventory.Quantity = newQuantity;
            inventory.UpdatedAt = DateTime.UtcNow;

            await db.InventoryAdjustments.AddAsync(adjustedInventoryHistory);
            await db.SaveChangesAsync();

            var mappedResponse = mapper.Map<InventoryResponse>(inventory);

            return new ApiResponse<InventoryResponse>
            {
                Data = mappedResponse,
                IsSuccess = true,
                Message = "Inventory Adjusted"
            };
        }

        public async Task<PagedApiResponse<InventoryAdjustmentResponse>> GetWarehouseInventoryAdjustments(Guid warehouseId, Guid inventoryId, int pageNumber = 1, int pageSize = 10)
        {
            await RequireWarehouseAccess(warehouseId);

            if (pageNumber <= 0 || pageSize <= 0 || pageSize > 100)
                throw new UserFriendlyException(ErrorMessages.InvalidPagination);

            var warehouseExists = await db.Warehouses
                .AsNoTracking()
                .AnyAsync(w => w.Id == warehouseId);

            if (!warehouseExists)
                throw new UserFriendlyException(ErrorMessages.WarehouseNotFound);

            var inventoryExists = await db.Inventories
                .AsNoTracking()
                .AnyAsync(i => i.Id == inventoryId);

            if (!inventoryExists)
            {
                throw new UserFriendlyException(ErrorMessages.InventoryNotFound);
            }

            var query = db.InventoryAdjustments
                .AsNoTracking()
                .Where(ia => ia.InventoryId == inventoryId);

            var totalCount = await query.CountAsync();

            var pagedQuery = await query
                .OrderBy(ia => ia.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var mappedResponse = mapper.Map<List<InventoryAdjustmentResponse>>(pagedQuery);

            return new PagedApiResponse<InventoryAdjustmentResponse>(
                mappedResponse, totalCount, pageSize, pageNumber)
            {
                IsSuccess = true,
                Message = "Branch Inventories Retrieved"
            };
        }

        public async Task<PagedApiResponse<InventoryResponse>> GetLowStockInventories(int pageNumber = 1, int pageSize = 10)
        {
            RequireAdmin();

            var lowStockInventories = db.Inventories
                .AsNoTracking()
                .Where(i => i.Quantity <= i.MinimumQuantity);

            if (lowStockInventories == null)
            {
                throw new UserFriendlyException(ErrorMessages.lowStockInventoriesNotFound);
            }

            var totalCount = await lowStockInventories.CountAsync();

            var pagedQuery = await lowStockInventories
                .AsNoTracking()
                .OrderBy(i => i.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var mappedResponse = mapper.Map<List<InventoryResponse>>(pagedQuery);

            return new PagedApiResponse<InventoryResponse>(
                mappedResponse, totalCount, pageSize, pageNumber)
            {
                IsSuccess = true,
                Message = "Low-stock inventories retrieved"
            };
        }

        public async Task<PagedApiResponse<InventoryResponse>> GetWarehouseLowStockInventories(Guid warehouseId, int pageNumber = 1, int pageSize = 10)
        {
            await RequireWarehouseAccess(warehouseId);

            if (pageNumber <= 0 || pageSize <= 0 || pageSize > 100)
                throw new UserFriendlyException(ErrorMessages.InvalidPagination);

            var warehouseExists = await db.Warehouses
                .AsNoTracking()
                .AnyAsync(w => w.Id == warehouseId);

            if (!warehouseExists)
            {
                throw new UserFriendlyException(ErrorMessages.WarehouseNotFound);
            }

            var lowStockWarehouseInventories = db.Inventories
                .AsNoTracking()
                .Where(i => i.Quantity <= i.MinimumQuantity && i.WarehouseId == warehouseId);

            if (lowStockWarehouseInventories == null)
            {
                throw new UserFriendlyException(ErrorMessages.lowStockInventoriesNotFound);
            }

            var totalCount = await lowStockWarehouseInventories.CountAsync();

            var pagedQuery = await lowStockWarehouseInventories
                .AsNoTracking()
                .OrderBy(i => i.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var mappedResponse = mapper.Map<List<InventoryResponse>>(pagedQuery);

            return new PagedApiResponse<InventoryResponse>(
                mappedResponse, totalCount, pageSize, pageNumber)
            {
                IsSuccess = true,
                Message = "Warehouse Low-stock inventories retrieved"
            };
        }

        public async Task<PagedApiResponse<InventoryResponse>> GetBranchLowStockInventories(Guid branchId, int pageNumber = 1, int pageSize = 10)
        {
            await RequireBranchAccess(branchId);

            if (pageNumber <= 0 || pageSize <= 0 || pageSize > 100)
                throw new UserFriendlyException(ErrorMessages.InvalidPagination);

            var warehouseExists = await db.Warehouses
                .AsNoTracking()
                .AnyAsync(w => w.Id == branchId);

            if (!warehouseExists)
            {
                throw new UserFriendlyException(ErrorMessages.WarehouseNotFound);
            }

            var lowStockWarehouseInventories = db.Inventories
                .AsNoTracking()
                .Where(i => i.Quantity <= i.MinimumQuantity && i.BranchId == branchId);

            if (lowStockWarehouseInventories == null)
            {
                throw new UserFriendlyException(ErrorMessages.lowStockInventoriesNotFound);
            }

            var totalCount = await lowStockWarehouseInventories.CountAsync();

            var pagedQuery = await lowStockWarehouseInventories
                .AsNoTracking()
                .OrderBy(i => i.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var mappedResponse = mapper.Map<List<InventoryResponse>>(pagedQuery);

            return new PagedApiResponse<InventoryResponse>(
                mappedResponse, totalCount, pageSize, pageNumber)
            {
                IsSuccess = true,
                Message = "Branch Low-stock inventories retrieved"
            };
        }

        public async Task<PagedApiResponse<InventoryResponse>> GetInventoriesByProduct(Guid productId, int pageNumber = 1, int pageSize = 10)
        {
            RequireAdmin();

            if (pageNumber <= 0 || pageSize <= 0 || pageSize > 100)
                throw new UserFriendlyException(ErrorMessages.InvalidPagination);

            var productExists = await db.Products.AnyAsync(p => p.Id == productId);

            if (!productExists)
            {
                throw new UserFriendlyException(ErrorMessages.ProductNotFound);
            }

            var query = db.Inventories.Where(i => i.ProductId == productId);

            var totalCount = await query.CountAsync();

            var pageResponse = await query
                .AsNoTracking()
                .OrderBy(i => i.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var mappedResponse = mapper.Map<List<InventoryResponse>>(pageResponse);

            return new PagedApiResponse<InventoryResponse>(
                mappedResponse, totalCount, pageSize, pageNumber)
            {
                IsSuccess = true,
                Message = "Inventories by product retrieved"
            };
        }

        public async Task<ApiResponse<InventoryResponse>> AdjustBranchInventory(Guid branchId, Guid inventoryId, AdjustInventoryRequest request)
        {
            await RequireBranchAccess(branchId);

            if (request.QuantityChange == 0)
            {
                throw new UserFriendlyException(ErrorMessages.MustBeNegativeOrPositive);
            }


            if (string.IsNullOrWhiteSpace(request.Reason))
            {
                throw new UserFriendlyException(ErrorMessages.ReasonIsRequiredForAdjustment);
            }

            var branchExists = await db.Branches
                .AsNoTracking()
                .AnyAsync(w => w.Id == branchId);

            if (!branchExists)
            {
                throw new UserFriendlyException(ErrorMessages.BranchNotFound);
            }

            var inventory = await db.Inventories
                .FirstOrDefaultAsync(i => i.Id == inventoryId && i.BranchId == branchId);

            if (inventory == null)
            {
                throw new UserFriendlyException(ErrorMessages.InventoryNotFound);
            }

            var adjustedInventoryHistory = mapper.Map<InventoryAdjustment>(request);

            var newQuantity = 0;

            try
            {

                checked
                {
                    newQuantity = inventory.Quantity + request.QuantityChange;

                    if (newQuantity < 0)
                    {
                        throw new UserFriendlyException(ErrorMessages.AfterAdjustingQuantityIsNegativeItMustBePositiveOrZero);
                    }
                }

            }
            catch (OverflowException ex)
            {
                throw new UserFriendlyException(ErrorMessages.IntegerOverflow);
            }


            adjustedInventoryHistory.InventoryId = inventoryId;
            adjustedInventoryHistory.PerformedByUserId = CurrentUserId;

            inventory.Quantity = newQuantity;
            inventory.UpdatedAt = DateTime.UtcNow;

            await db.InventoryAdjustments.AddAsync(adjustedInventoryHistory);
            await db.SaveChangesAsync();

            var mappedResponse = mapper.Map<InventoryResponse>(inventory);

            return new ApiResponse<InventoryResponse>
            {
                Data = mappedResponse,
                IsSuccess = true,
                Message = "Inventory Adjusted"
            };
        }

        public async Task<PagedApiResponse<InventoryAdjustmentResponse>> GetBranchInventoryAdjustments(Guid branchId, Guid inventoryId, int pageNumber = 1, int pageSize = 10)
        {
            await RequireBranchAccess(branchId);

            if (pageNumber <= 0 || pageSize <= 0 || pageSize > 100)
                throw new UserFriendlyException(ErrorMessages.InvalidPagination);

            var branchExists = await db.Warehouses
                .AsNoTracking()
                .AnyAsync(w => w.Id == branchId);

            if (!branchExists)
                throw new UserFriendlyException(ErrorMessages.BranchNotFound);

            var inventoryExists = await db.Inventories
                .AsNoTracking()
                .AnyAsync(i => i.Id == inventoryId);

            if (!inventoryExists)
            {
                throw new UserFriendlyException(ErrorMessages.InventoryNotFound);
            }

            var query = db.InventoryAdjustments
                .AsNoTracking()
                .Where(ia => ia.InventoryId == inventoryId);

            var totalCount = await query.CountAsync();

            var pagedQuery = await query
                .AsNoTracking()
                .OrderBy(ia => ia.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var mappedResponse = mapper.Map<List<InventoryAdjustmentResponse>>(pagedQuery);

            return new PagedApiResponse<InventoryAdjustmentResponse>(
                mappedResponse, totalCount, pageSize, pageNumber)
            {
                IsSuccess = true,
                Message = "Branch inventory adjustments retrieved"
            };
        }
    }
}
