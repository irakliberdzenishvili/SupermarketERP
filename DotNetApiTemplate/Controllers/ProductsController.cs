using DotNetApiTemplate.Models.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SupermarketERP.Constants;
using SupermarketERP.Interfaces;
using SupermarketERP.Models.Request.Product;
using SupermarketERP.Models.Response.Product;
using SupermarketERP.Security;

namespace SupermarketERP.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ProductsController(IProductService productService) : ControllerBase
    {

        [HttpGet]
        [EnableRateLimiting(ProductRateLimitPolicy.Get)]
        public async Task<PagedApiResponse<ProductResponse>> GetProducts(
            [FromQuery] string? search,
            [FromQuery] string? category,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            return await productService.GetProducts(search, category, pageNumber, pageSize);
        }

        [HttpGet("{productId:guid}")]
        [EnableRateLimiting(ProductRateLimitPolicy.GetById)]
        public async Task<ApiResponse<ProductResponse>> GetProductById([FromRoute] Guid productId)
        {
            return await productService.GetProductById(productId);
        }

        [HttpPost]
        [Authorize(Roles = Roles.Admin)]
        [EnableRateLimiting(ProductRateLimitPolicy.Create)]
        public async Task<ApiResponse<ProductResponse>> CreateProduct([FromBody] CreateProductRequest request)
        {
            return await productService.CreateProduct(request);
        }

        [HttpPut("{productId:guid}")]
        [Authorize(Roles = Roles.Admin)]
        [EnableRateLimiting(ProductRateLimitPolicy.Update)]
        public async Task<ApiResponse<ProductResponse>> UpdateProduct([FromRoute] Guid productId, [FromBody] UpdateProductRequest request)
        {
            return await productService.UpdateProduct(productId, request);
        }

        [HttpPatch("{productId:guid}/status")]
        [Authorize(Roles = Roles.Admin)]
        [EnableRateLimiting(ProductRateLimitPolicy.Update)]
        public async Task<ApiResponse<ProductResponse>> ChangeProductStatus([FromRoute] Guid productId, [FromBody] ChangeProductStatusRequest request)
        {
            return await productService.ChangeProductStatus(productId, request);
        }

        [HttpDelete("{productId:guid}")]
        [Authorize(Roles = Roles.Admin)]
        [EnableRateLimiting(ProductRateLimitPolicy.Delete)]
        public async Task<ApiResponse<ProductResponse>> DeleteProduct([FromRoute] Guid productId)
        {
            return await productService.DeleteProduct(productId);
        }

        [HttpGet("{barcode}")]
        [EnableRateLimiting(ProductRateLimitPolicy.GetByBarcode)]
        public async Task<ApiResponse<ProductResponse>> GetProductByBarcode([FromRoute] string barcode)
        {
            return await productService.GetProductByBarcode(barcode);
        }
    }
}
