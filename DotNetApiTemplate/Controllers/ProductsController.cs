using DotNetApiTemplate.Models.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SupermarketERP.Constants;
using SupermarketERP.Interfaces;
using SupermarketERP.Models.Request.Product;
using SupermarketERP.Models.Response.Product;

namespace SupermarketERP.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ProductsController(IProductService productService) : ControllerBase
    {

        [HttpGet]
        public async Task<PagedApiResponse<ProductResponse>> GetProducts(
            [FromQuery] string? search,
            [FromQuery] string? category,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            return await productService.GetProducts(search, category, pageNumber, pageSize);
        }

        [HttpGet("{productId:guid}")]
        public async Task<ApiResponse<ProductResponse>> GetProductById([FromRoute] Guid productId)
        {
            return await productService.GetProductById(productId);
        }

        [HttpPost]
        [Authorize(Roles = Roles.Admin)]
        public async Task<ApiResponse<ProductResponse>> CreateProduct([FromBody] CreateProductRequest request)
        {
            return await productService.CreateProduct(request);
        }
    }
}
