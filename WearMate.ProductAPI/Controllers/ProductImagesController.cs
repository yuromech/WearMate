using Microsoft.AspNetCore.Mvc;
using WearMate.ProductAPI.Services;
using WearMate.Shared.DTOs.Products;
using WearMate.Shared.DTOs.Common;

namespace WearMate.ProductAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductImagesController : ControllerBase
{
    private readonly ProductService _productService;
    private readonly ILogger<ProductImagesController> _logger;

    public ProductImagesController(ProductService productService, ILogger<ProductImagesController> logger)
    {
        _productService = productService;
        _logger = logger;
    }

    [HttpGet("by-product/{productId:guid}")]
    public async Task<ActionResult<ApiResponse<List<ProductImageDto>>>> GetByProduct(Guid productId)
    {
        try
        {
            var imgs = await _productService.GetImagesByProductAsync(productId);
            return Ok(ApiResponse<List<ProductImageDto>>.SuccessResponse(imgs));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting images for product {ProductId}", productId);
            return StatusCode(500, ApiResponse<List<ProductImageDto>>.ErrorResponse("Error", new List<string> { ex.Message }));
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(Guid id)
    {
        try
        {
            var ok = await _productService.DeleteImageAsync(id);
            return Ok(ApiResponse<bool>.SuccessResponse(ok));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting image {ImageId}", id);
            return StatusCode(500, ApiResponse<bool>.ErrorResponse("Error", new List<string> { ex.Message }));
        }
    }

    public class ReorderRequest { public Guid ProductId { get; set; } public List<Guid> OrderedIds { get; set; } = new(); }

    [HttpPost("reorder")]
    public async Task<ActionResult<ApiResponse<bool>>> Reorder([FromBody] ReorderRequest req)
    {
        try
        {
            var ok = await _productService.ReorderImagesAsync(req.ProductId, req.OrderedIds);
            return Ok(ApiResponse<bool>.SuccessResponse(ok));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reordering images for product {ProductId}", req.ProductId);
            return StatusCode(500, ApiResponse<bool>.ErrorResponse("Error", new List<string> { ex.Message }));
        }
    }

    [HttpPost("add")]
    public async Task<ActionResult<ApiResponse<ProductImageDto>>> AddImage([FromForm] Guid productId, [FromForm] IFormFile file)
    {
        try
        {
            var result = await _productService.AddProductImageAsync(productId, file);
            return Ok(ApiResponse<ProductImageDto>.SuccessResponse(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding image to product {ProductId}", productId);
            return StatusCode(500, ApiResponse<ProductImageDto>.ErrorResponse("Error", new List<string> { ex.Message }));
        }
    }

    [HttpPut("thumbnail")]
    public async Task<ActionResult<ApiResponse<bool>>> UpdateThumbnail([FromBody] UpdateThumbnailRequest req)
    {
        try
        {
            var ok = await _productService.UpdateProductThumbnailAsync(req.ProductId, req.ImageId);
            return Ok(ApiResponse<bool>.SuccessResponse(ok));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating thumbnail for product {ProductId}", req.ProductId);
            return StatusCode(500, ApiResponse<bool>.ErrorResponse("Error", new List<string> { ex.Message }));
        }
    }

    [HttpPost("{id:guid}/replace")]
    public async Task<ActionResult<ApiResponse<ProductImageDto>>> ReplaceImage(Guid id, [FromForm] IFormFile file)
    {
        try
        {
            var result = await _productService.ReplaceProductImageAsync(id, file);
            return Ok(ApiResponse<ProductImageDto>.SuccessResponse(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error replacing image {ImageId}", id);
            return StatusCode(500, ApiResponse<ProductImageDto>.ErrorResponse("Error", new List<string> { ex.Message }));
        }
    }
}
