
using AutoMapper;
using DotNetApiTemplate.DB;
using DotNetApiTemplate.Helpers;
using DotNetApiTemplate.Interfaces;
using DotNetApiTemplate.Models.Response;
using Microsoft.EntityFrameworkCore;
using SupermarketERP.Constants;
using SupermarketERP.Exceptions;
using SupermarketERP.Interfaces;
using SupermarketERP.Models.Data;
using SupermarketERP.Models.Request.Product;
using SupermarketERP.Models.Response.Product;

namespace SupermarketERP.Services
{
    public class ProductService(
        ApplicationDbContext db,
        AuthHelper authHelper,
        IEmailService emailService,
        IMapper mapper) : IProductService
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

        public async Task<ApiResponse<ProductResponse>> CreateProduct(CreateProductRequest request)
        {
            RequireAdmin();

            var nameExists = await db.Products
                .IgnoreQueryFilters()
                .AnyAsync(p => p.Name == request.Name);

            if (nameExists)
            {
                throw new UserFriendlyException(ErrorMessages.ProductAlreadyExists);
            }

            if (!string.IsNullOrWhiteSpace(request.Barcode))
            {
                var barcodeExists = await db.Products
                    .IgnoreQueryFilters()
                    .AnyAsync(p => p.Barcode == request.Barcode);

                if (barcodeExists)
                {
                    throw new UserFriendlyException(ErrorMessages.ProductAlreadyExists);
                }
            }

            var newProduct = mapper.Map<Product>(request);

            await db.Products.AddAsync(newProduct);
            await db.SaveChangesAsync();

            var mappedProduct = mapper.Map<ProductResponse>(newProduct);

            return new ApiResponse<ProductResponse>
            {
                Data = mappedProduct,
                IsSuccess = true,
                Message = "Product created successfully"
            };
        }

        public async Task<ApiResponse<ProductResponse>> GetProductById(Guid productId)
        {
            if (productId == Guid.Empty)
            {
                throw new UserFriendlyException(ErrorMessages.ProductIdIsRequired);
            }

            var product = await db.Products
                .FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null)
            {
                throw new UserFriendlyException(ErrorMessages.ProductNotFound);
            }

            var mappedResponse = mapper.Map<ProductResponse>(product);

            return new ApiResponse<ProductResponse>
            {
                Data = mappedResponse,
                IsSuccess = true,
                Message = "Product found"
            };
        }

        public async Task<PagedApiResponse<ProductResponse>> GetProducts(
            string? search,
            string? category,
            int pageNumber = 1,
            int pageSize = 10)
        {
            if (pageNumber <= 0 || pageSize > 100 || pageSize <= 0)
            {
                throw new UserFriendlyException(ErrorMessages.InvalidPagination);
            }

            var query = db.Products.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(
                    p => p.Name.Contains(search) || p.Description.Contains(search));
            }

            if (!string.IsNullOrWhiteSpace(category))
            {
                query = query.Where(p => p.Category.Contains(category));
            }

            var totalCount = await query.CountAsync();

            var paginatedQuery = await query
                .OrderBy(p => p.SKU)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var mappedResponse = mapper.Map<List<ProductResponse>>(paginatedQuery);

            return new PagedApiResponse<ProductResponse>(
                mappedResponse, totalCount, pageSize, pageNumber)
            {
                IsSuccess = true,
                Message = "Products Retrieved"
            };
        }

        public async Task<ApiResponse<ProductResponse>> UpdateProduct(
            Guid productId,
            UpdateProductRequest request)
        {
            RequireAdmin();

            var productToUpdate = await db.Products
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.Id == productId && !p.IsDeleted);

            if (productToUpdate == null)
            {
                throw new UserFriendlyException(ErrorMessages.ProductNotFound);
            }

            var nameExists = await db.Products
                .IgnoreQueryFilters()
                .AnyAsync(p => p.Id != productId && p.Name == request.Name);

            if (nameExists)
            {
                throw new UserFriendlyException(ErrorMessages.ProductAlreadyExists);
            }

            if (!string.IsNullOrWhiteSpace(request.Barcode))
            {
                var barcodeExists = await db.Products
                    .IgnoreQueryFilters()
                    .AnyAsync(p => p.Id != productId && p.Barcode == request.Barcode);

                if (barcodeExists)
                {
                    throw new UserFriendlyException(ErrorMessages.ProductAlreadyExists);
                }
            }

            mapper.Map(request, productToUpdate);

            productToUpdate.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();

            var mappedResponse = mapper.Map<ProductResponse>(productToUpdate);

            return new ApiResponse<ProductResponse>
            {
                Data = mappedResponse,
                IsSuccess = true,
                Message = "Product Updated"
            };
        }

        public async Task<ApiResponse<ProductResponse>> ChangeProductStatus(
            Guid productId,
            ChangeProductStatusRequest request)
        {
            RequireAdmin();

            var productToUpdate = await db.Products
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.Id == productId && !p.IsDeleted);

            if (productToUpdate == null)
            {
                throw new UserFriendlyException(ErrorMessages.ProductNotFound);
            }

            mapper.Map(request, productToUpdate);

            productToUpdate.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();

            var mappedResponse = mapper.Map<ProductResponse>(productToUpdate);

            return new ApiResponse<ProductResponse>
            {
                Data = mappedResponse,
                IsSuccess = true,
                Message = "Product Status Updated"
            };
        }

        public async Task<ApiResponse<ProductResponse>> DeleteProduct(Guid productId)
        {
            RequireAdmin();

            var productToDelete = await db.Products
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(p => p.Id == productId && !p.IsDeleted);

            if (productToDelete == null)
            {
                throw new UserFriendlyException(ErrorMessages.ProductNotFound);
            }

            productToDelete.IsDeleted = true;
            productToDelete.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();

            return new ApiResponse<ProductResponse>
            {
                IsSuccess = true,
                Message = "Product Deleted"
            };
        }

        public async Task<ApiResponse<ProductResponse>> GetProductByBarcode(string barcode)
        {
            var product = await db.Products
                .FirstOrDefaultAsync(p => p.Barcode == barcode);

            if (product == null)
            {
                throw new UserFriendlyException(ErrorMessages.ProductNotFound);
            }

            var mappedResponse = mapper.Map<ProductResponse>(product);

            return new ApiResponse<ProductResponse>
            {
                Data = mappedResponse,
                IsSuccess = true,
                Message = "Product Retrieved"
            };
        }
    }
}
