using AutoMapper;
using DotNetApiTemplate.DB;
using DotNetApiTemplate.Helpers;
using DotNetApiTemplate.Models.Response;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SupermarketERP.Constants;
using SupermarketERP.Exceptions;
using SupermarketERP.Interfaces;
using SupermarketERP.Models.Data;
using SupermarketERP.Models.Request.Sales;
using SupermarketERP.Models.Response.Sales;
using SupermarketERP.Models.Response.Stock;
using System.Net.ServerSentEvents;

namespace SupermarketERP.Services
{
    public class SaleService(ApplicationDbContext db, AuthHelper authHelper, IMapper mapper) : ISaleService
    {
        private readonly string currentUserId = authHelper.UserId;
        private readonly string currentUserRole = authHelper.UserRole;

        private void RequireAdmin()
        {
            var isAdmin = currentUserRole == Roles.Admin;

            if (!isAdmin)
            {
                throw new UserFriendlyException(ErrorMessages.Forbidden);
            }
        }

        private async Task HasAccess(Guid branchId)
        {
            // if role is not admin, cashier or branch manager deny access
            if (currentUserRole != Roles.Admin
                && currentUserRole != Roles.Cashier
                && currentUserRole != Roles.BranchManager)
            {
                throw new UserFriendlyException(ErrorMessages.Forbidden);
            }

            if (currentUserRole == Roles.Admin)
            {
                return;
            }
            else if (currentUserRole == Roles.Cashier)
            {

                var isBranchEmployee = await db.BranchWorkers
                    .AsNoTracking()
                    .AnyAsync(bw => bw.UserId == currentUserId && bw.BranchId == branchId);

                if (!isBranchEmployee)
                {
                    throw new UserFriendlyException(ErrorMessages.Forbidden);
                }
            }
            else if (currentUserRole == Roles.BranchManager)
            {
                var isBranchManager = await db.BranchManagers
                    .AsNoTracking()
                    .AnyAsync(bm => bm.UserId == currentUserId && bm.BranchId == branchId);

                if (!isBranchManager)
                {
                    throw new UserFriendlyException(ErrorMessages.Forbidden);
                }
            }
        }
        public async Task<ApiResponse<SaleResponse>> CreateSaleDraft(Guid branchId)
        {
            if (branchId == Guid.Empty)
            {
                throw new UserFriendlyException(ErrorMessages.BranchIdsIsRequired);
            }

            await HasAccess(branchId);

            var newSalesDraft = new Sale()
            {
                Status = Enums.SaleStatus.Draft,
                SaleNumber = SaleNumberGenerator.GenerateSaleNumber(),
                BranchId = branchId,
                CashierId = currentUserId,
                SubTotal = 0,
                TotalAmount = 0,
                DiscountAmount = 0
            };

            await db.Sales.AddAsync(newSalesDraft);
            await db.SaveChangesAsync();

            var mappedResponse = mapper.Map<SaleResponse>(newSalesDraft);

            return new ApiResponse<SaleResponse>
            {
                Data = mappedResponse,
                IsSuccess = true,
                Message = "Sale draft created"
            };
        }

        public async Task<ApiResponse<SaleResponse>> CreateSaleItem(Guid saleId, AddSaleItemRequest request)
        {
            if (saleId == Guid.Empty || request.ProductId == Guid.Empty)
            {
                throw new UserFriendlyException(ErrorMessages.IdsAreRequired);
            }

            if (request.Quantity <= 0)
            {
                throw new UserFriendlyException(ErrorMessages.MustBeGreaterThanZero);
            }

            var saleDraft = await db.Sales
                .Include(s => s.Items)
                .Include(s => s.Payments)
                .FirstOrDefaultAsync(s => s.Id == saleId);

            if (saleDraft == null)
            {
                throw new UserFriendlyException(ErrorMessages.SaleDraftNotFound);
            }

            if (saleDraft.Status != Enums.SaleStatus.Draft)
            {
                throw new UserFriendlyException(ErrorMessages.SaleMustBeDraft);
            }

            var product = await db.Products.FirstOrDefaultAsync(p => p.Id == request.ProductId && p.IsActive);

            if (product == null)
            {
                throw new UserFriendlyException(ErrorMessages.ProductNotFound);
            }


            await HasAccess(saleDraft.BranchId);

            var saleItem = saleDraft.Items.FirstOrDefault(s => s.ProductId == request.ProductId);

            if (saleItem == null)
            {
                var newSaleItem = new SaleItem() {

                SaleId = saleId,
                ProductId = product.Id,
                Quantity = request.Quantity,
                UnitPrice = product.SellingPrice,
                DiscountAmount = 0

                };

                saleDraft.Items.Add(newSaleItem);
                saleItem = newSaleItem;
            }
            else
            {
                saleItem.Quantity += request.Quantity;
            }

            saleItem.UnitPrice = product.SellingPrice;
            saleItem.TotalPrice = (saleItem.Quantity * saleItem.UnitPrice) - saleItem.DiscountAmount;

            saleDraft.SubTotal = saleDraft.Items.Sum(si => si.TotalPrice);
            saleDraft.TotalAmount = saleDraft.SubTotal - saleDraft.DiscountAmount;

            await db.SaveChangesAsync();

            var mappedResponse = mapper.Map<SaleResponse>(saleDraft);


            return new ApiResponse<SaleResponse>
            {
                Data = mappedResponse,
                IsSuccess = true,
                Message = "Sale item added"
            };
        }

        public async Task<ApiResponse<SaleResponse>> UpdateSaleItem(Guid saleId, Guid itemId, UpdateSaleItemRequest request)
        {
            if (request.Quantity <= 0)
            {
                throw new UserFriendlyException(ErrorMessages.MustBeGreaterThanZero);
            }

            if (saleId == Guid.Empty || itemId == Guid.Empty)
            {
                throw new UserFriendlyException(ErrorMessages.IdsAreRequired);
            }

            var sale = await db.Sales
                .Include(s => s.Items)
                .Include(p => p.Payments)
                .FirstOrDefaultAsync(s => s.Id == saleId && s.Status == Enums.SaleStatus.Draft);

            if (sale == null)
            {
                throw new UserFriendlyException(ErrorMessages.SaleDraftNotFound);
            }

            await HasAccess(sale.BranchId);

            var saleItem = sale.Items.FirstOrDefault(s => s.Id == itemId);

            if (saleItem == null)
            {
                throw new UserFriendlyException(ErrorMessages.SaleItemNotFound);
            }


            saleItem.Quantity = request.Quantity;
            saleItem.TotalPrice = (saleItem.Quantity * saleItem.UnitPrice) - saleItem.DiscountAmount;

            sale.SubTotal = sale.Items.Sum(s => s.TotalPrice);
            sale.TotalAmount = sale.SubTotal - sale.DiscountAmount;

            await db.SaveChangesAsync();

            var mappedResponse = mapper.Map<SaleResponse>(sale);

            return new ApiResponse<SaleResponse>
            {
                Data = mappedResponse,
                IsSuccess = true,
                Message = "Sale item updated"
            };
        }

        public async Task<ApiResponse<SaleResponse>> DeleteSaleItem(Guid saleId, Guid itemId)
        {

            if (saleId == Guid.Empty || itemId == Guid.Empty)
            {
                throw new UserFriendlyException(ErrorMessages.IdsAreRequired);
            }

            var sale = await db.Sales
                .Include(s => s.Items)
                .Include(p => p.Payments)
                .FirstOrDefaultAsync(s => s.Id == saleId && s.Status == Enums.SaleStatus.Draft);

            if (sale == null)
            {
                throw new UserFriendlyException(ErrorMessages.SaleDraftNotFound);
            }

            await HasAccess(sale.BranchId);

            var saleItem = sale.Items.FirstOrDefault(s => s.Id == itemId);

            if (saleItem == null)
            {
                throw new UserFriendlyException(ErrorMessages.SaleItemNotFound);
            }

            saleItem.IsDeleted = true;
           
            await db.SaveChangesAsync();

            return new ApiResponse<SaleResponse>
            {
                Data = null,
                IsSuccess = true,
                Message = "Sale item deleted"
            };
        }

        public async Task<ApiResponse<SaleResponse>> CreatePayment(Guid saleId, CreatePaymentRequest request)
        {
            if (request.AmountReceived <= 0)
            {
                throw new UserFriendlyException(ErrorMessages.MustBeGreaterThanZero);
            }

            if (saleId == Guid.Empty)
            {
                throw new UserFriendlyException(ErrorMessages.IdsAreRequired);
            }


            var sale = await db.Sales
                .Include(s => s.Items)
                .Include(p => p.Payments)
                .FirstOrDefaultAsync(s => s.Id == saleId && s.Status == Enums.SaleStatus.Draft);

            if (sale == null)
            {
                throw new UserFriendlyException(ErrorMessages.SaleNotFound);
            }

            var SaleItem = sale.Items.FirstOrDefault(s => s.SaleId == saleId);

            if (SaleItem == null)
            {
                throw new UserFriendlyException(ErrorMessages.SaleItemNotFound);
            }

            await HasAccess(sale.BranchId);

            var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);

            try
            {

                if (request.AmountReceived < sale.TotalAmount)
                {
                     throw new UserFriendlyException(ErrorMessages.NotEnoughMoney);
                }


                var productIds = sale.Items
                    .Select(i => i.ProductId)
                    .Distinct()
                    .ToList();

                var matchingInventoryRecords = await db.Inventories
                        .Where(i => i.BranchId == sale.BranchId && productIds.Contains(i.ProductId))
                        .ToListAsync();

                var aggregatedItems = sale.Items
               .GroupBy(i => i.ProductId)
               .Select(g => new
         {
           ProductId = g.Key,
           Quantity = g.Sum(i => i.Quantity)
         })
               .ToList();

                foreach (var item in aggregatedItems)
                {
                    var matchingRecord = matchingInventoryRecords
                        .FirstOrDefault(i => i.ProductId == item.ProductId);

                    if (matchingRecord == null)
                        throw new UserFriendlyException(ErrorMessages.InventoryNotFound);

                    if (matchingRecord.Quantity < item.Quantity)
                        throw new UserFriendlyException(ErrorMessages.NotEnoughInStock);
                }

                foreach (var item in aggregatedItems)
                {
                    var matchingRecord = matchingInventoryRecords
                        .First(i => i.ProductId == item.ProductId);

                    matchingRecord.Quantity -= item.Quantity;
                }

                var newPayment = new Payment()
                {
                    SaleId = saleId,
                    Amount = request.AmountReceived,
                    Status = Enums.PaymentStatus.Completed,
                    Method = Enums.PaymentMethod.Cash
                };

                sale.Payments.Add(newPayment);

                sale.Status = Enums.SaleStatus.Completed;
                sale.CompletedAt = DateTime.UtcNow;

                await db.SaveChangesAsync();
                await transaction.CommitAsync();

            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }

            var mappedResponse = mapper.Map<SaleResponse>(sale);

            mappedResponse.Change = request.AmountReceived - sale.TotalAmount;

            return new ApiResponse<SaleResponse>
            {
                Data = mappedResponse,
                IsSuccess = true,
                Message = "Payment recorded"
            };
        }

        public async Task<PagedApiResponse<SaleResponse>> GetSales(GetSalesRequest request, int pageNumber = 1, int pageSize = 10)
        {
            RequireAdmin(); // this endpoint is for admin only

            if (pageNumber <= 0 || pageSize <= 0 || pageSize > 100)
                throw new UserFriendlyException(ErrorMessages.InvalidPagination);

            var query = db.Sales
                .Include(s => s.Payments)
                .Include(s => s.Items)
                .AsNoTracking();
                

            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                query = query.Where(q => q.SaleNumber.Contains(request.Search));
            }

            if (!string.IsNullOrWhiteSpace(request.CashierId))
            {
                query = query.Where(q => q.CashierId == request.CashierId);
            }

            if (request.BranchId.HasValue)
            {
                query = query.Where(q => q.BranchId == request.BranchId.Value);
            }

            if (request.Status != null)
            {
                query = query.Where(q => q.Status == request.Status);
            }

            if (request.FromDate.HasValue)
            {
                query = query.Where(q => q.CreatedAt >= request.FromDate.Value);
            }

            if (request.ToDate.HasValue)
            {
                query = query.Where(q => q.CreatedAt < request.ToDate.Value);
            }

            var totalCount = await query.CountAsync();

            var pagedQuery = await query
                      .OrderByDescending(q => q.CreatedAt)
                      .ThenByDescending(q => q.Id)
                      .Skip((pageNumber - 1) * pageSize)
                      .Take(pageSize)
                      .ToListAsync();


            var mappedResponse = mapper.Map<List<SaleResponse>>(pagedQuery);

            return new PagedApiResponse<SaleResponse>(
                mappedResponse, totalCount, pageSize, pageNumber)
            {
                IsSuccess = true,
                Message = "Stock transfers retrieved"
            };
        }

        public async Task<ApiResponse<SaleResponse>> GetSaleById(Guid saleId)
        {
            if (saleId == Guid.Empty)
            {
                throw new UserFriendlyException(ErrorMessages.IdsAreRequired);
            }

            var sale = await db.Sales
                .AsNoTracking()
                .Include(s => s.Items)
                .Include(p => p.Payments)
                .Include(u => u.Cashier)
                .Include(b => b.Branch)
                .FirstOrDefaultAsync(s => s.Id == saleId);

            if (sale == null)
            {
                throw new UserFriendlyException(ErrorMessages.SaleNotFound);
            }

            await HasAccess(sale.BranchId);

            var mappedResponse = mapper.Map<SaleResponse>(sale);

            mappedResponse.CashierName = sale.Cashier.Email!;
            mappedResponse.CashierId = sale.CashierId;
            mappedResponse.BranchName = sale.Branch.Name;
            mappedResponse.BranchId = sale.BranchId;
            
            return new ApiResponse<SaleResponse>
            {
                Data = mappedResponse,
                IsSuccess = true,
                Message = "Sale found"
            };
        }

        public async Task<ApiResponse<SaleResponse>> CancelSale(Guid saleId)
        {
            if (saleId == Guid.Empty)
            {
                throw new UserFriendlyException(ErrorMessages.IdsAreRequired);
            }

            var sale = await db.Sales
                .Include(s => s.Items)
                .Include(p => p.Payments)
                .Include(u => u.Cashier)
                .Include(b => b.Branch)
                .FirstOrDefaultAsync(s => s.Id == saleId);

            if (sale == null)
            {
                throw new UserFriendlyException(ErrorMessages.SaleNotFound);
            }

            await HasAccess(sale.BranchId);

            if (sale.Status != Enums.SaleStatus.Draft)
            {
                throw new UserFriendlyException(ErrorMessages.SaleMustBeDraft);
            }

            sale.Status = Enums.SaleStatus.Cancelled;

            await db.SaveChangesAsync();

            var mappedResponse = mapper.Map<SaleResponse>(sale);

            return new ApiResponse<SaleResponse>
            {
                Data = mappedResponse,
                IsSuccess = true,
                Message = "Sale cancelled"
            };
        }

        public async Task<ApiResponse<SaleResponse>> GetReceipt(Guid saleId)
        {
            if (saleId == Guid.Empty)
            {
                throw new UserFriendlyException(ErrorMessages.IdsAreRequired);
            }

            var sale = await db.Sales
                .AsNoTracking()
                .Include(s => s.Items)
                .Include(p => p.Payments)
                .Include(u => u.Cashier)
                .Include(b => b.Branch)
                .FirstOrDefaultAsync(s => s.Id == saleId && s.Status == Enums.SaleStatus.Completed);

            if (sale == null)
            {
                throw new UserFriendlyException(ErrorMessages.SaleNotFound);
            }

            await HasAccess(sale.BranchId);

            var payment = sale.Payments.FirstOrDefault(s => s.SaleId == saleId && s.Status == Enums.PaymentStatus.Completed);

            if (payment == null)
            {
                throw new UserFriendlyException(ErrorMessages.PaymentNotFound);
            }

            var mappedResponse = mapper.Map<SaleResponse>(sale);

            mappedResponse.CashierName = sale.Cashier.Email!;
            mappedResponse.CashierId = sale.CashierId;
            mappedResponse.BranchName = sale.Branch.Name;
            mappedResponse.BranchId = sale.BranchId;

            mappedResponse.Change = payment.Amount - sale.TotalAmount;

            return new ApiResponse<SaleResponse>
            {
                Data = mappedResponse,
                IsSuccess = true,
                Message = "Sale receipt found"
            };
        }
    }
}
