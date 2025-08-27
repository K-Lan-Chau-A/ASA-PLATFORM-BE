using ASA_PLATFORM_REPO.Models;
using ASA_PLATFORM_SERVICE.DTOs.Request;
using ASA_PLATFORM_SERVICE.DTOs.Response;
using ASA_PLATFORM_SERVICE.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ASA_PLATFORM_BE.Controllers
{
    [Route("api/products")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly IProductService _productService;
        public ProductController(IProductService productService)
        {
            _productService = productService;
        }
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Product>>> GetFiltered([FromQuery] ProductGetRequest requestDto, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var result = await _productService.GetFilteredProductsAsync(requestDto, page, pageSize);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
        [HttpPost]
        public async Task<ActionResult<ProductResponse>> Create([FromBody] ProductRequest request)
        {
            var result = await _productService.CreateAsync(request);
            return Ok(result);
        }
        [HttpPut("{id}")]
        public async Task<ActionResult<ProductResponse>> Update(long id, [FromBody] ProductRequest request)
        {
            var result = await _productService.UpdateAsync(id, request);
            return Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<bool>> Delete(long id)
        {
            var result = await _productService.DeleteAsync(id);
            return Ok(result);
        }
    }
}
