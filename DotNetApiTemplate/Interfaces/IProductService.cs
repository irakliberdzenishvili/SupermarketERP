using DotNetApiTemplate.Models.Response;
using SupermarketERP.Models.Request.Product;
using SupermarketERP.Models.Response.Product;

namespace SupermarketERP.Interfaces
{
    public interface IProductService
    {
        Task<PagedApiResponse<ProductResponse>> GetProducts(string? search, string? category, int pageNumber = 1, int pageSize = 10);

        Task<ApiResponse<ProductResponse>> GetProductById(Guid productId);

        Task<ApiResponse<ProductResponse>> CreateProduct(CreateProductRequest request);

        Task<ApiResponse<ProductResponse>> UpdateProduct(Guid productId, UpdateProductRequest request);

        Task<ApiResponse<ProductResponse>> ChangeProductStatus(Guid productId, ChangeProductStatusRequest request);

        Task<ApiResponse<ProductResponse>> DeleteProduct(Guid productId);

        Task<ApiResponse<ProductResponse>> GetProductByBarcode(string barcode);
    }
}
