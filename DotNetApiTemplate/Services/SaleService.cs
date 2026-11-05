using AutoMapper;
using DotNetApiTemplate.DB;
using DotNetApiTemplate.Helpers;
using DotNetApiTemplate.Models.Response;
using Microsoft.EntityFrameworkCore;
using SupermarketERP.Constants;
using SupermarketERP.Exceptions;
using SupermarketERP.Interfaces;
using SupermarketERP.Models.Data;
using SupermarketERP.Models.Request.Sales;
using SupermarketERP.Models.Response.Sales;

namespace SupermarketERP.Services
{
    public class SaleService(ApplicationDbContext db, AuthHelper authHelper, IMapper mapper) : ISaleService
    {
        private readonly string currentUserId = authHelper.UserId;
        private readonly string currentUserRole = authHelper.UserRole;


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
    }
}
