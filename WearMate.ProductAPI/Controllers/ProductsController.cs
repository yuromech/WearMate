using Microsoft.AspNetCore.Mvc;
using WearMate.ProductAPI.Services;
using WearMate.Shared.DTOs.Common;
using WearMate.Shared.DTOs.Products;

namespace WearMate.ProductAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly ProductService _productService;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(ProductService productService, ILogger<ProductsController> logger)
    {
        _productService = productService;
        _logger = logger;
    }

    /// <summary>
    /// GET /api/products - Get paginated products
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginatedResult<ProductDto>>>> GetProducts(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? categoryId = null,
        [FromQuery] Guid? brandId = null,
        [FromQuery] string? category = null,
        [FromQuery] string? brand = null,
        [FromQuery] string? search = null,
        [FromQuery] bool? isFeatured = null,
        [FromQuery] decimal? minPrice = null,
        [FromQuery] decimal? maxPrice = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool? isActive = true)
    {
        try
        {
            var result = await _productService.GetProductsAsync(
                page, pageSize, categoryId, brandId, category, brand, search, isFeatured, minPrice, maxPrice, sortBy, isActive);
            return Ok(ApiResponse<PaginatedResult<ProductDto>>.SuccessResponse(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting products");
            return StatusCode(500, ApiResponse<PaginatedResult<ProductDto>>.ErrorResponse(
                "Error retrieving products", new List<string> { ex.Message }));
        }
    }

    /// <summary>
    /// GET /api/products/{id} - Get product by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ProductDto>>> GetProduct(Guid id)
    {
        try
        {
            var product = await _productService.GetProductByIdAsync(id);

            if (product == null)
                return NotFound(ApiResponse<ProductDto>.ErrorResponse("Product not found"));

            return Ok(ApiResponse<ProductDto>.SuccessResponse(product));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting product {ProductId}", id);
            return StatusCode(500, ApiResponse<ProductDto>.ErrorResponse(
                "Error retrieving product", new List<string> { ex.Message }));
        }
    }

    /// <summary>
    /// GET /api/products/slug/{slug} - Get product by slug
    /// </summary>
    [HttpGet("slug/{slug}")]
    public async Task<ActionResult<ApiResponse<ProductDto>>> GetProductBySlug(string slug)
    {
        try
        {
            var product = await _productService.GetProductBySlugAsync(slug);

            if (product == null)
                return NotFound(ApiResponse<ProductDto>.ErrorResponse("Product not found"));

            return Ok(ApiResponse<ProductDto>.SuccessResponse(product));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting product by slug {Slug}", slug);
            return StatusCode(500, ApiResponse<ProductDto>.ErrorResponse(
                "Error retrieving product", new List<string> { ex.Message }));
        }
    }

    /// <summary>
    /// GET /api/products/featured - Get featured products
    /// </summary>
    [HttpGet("featured")]
    public async Task<ActionResult<ApiResponse<List<ProductDto>>>> GetFeaturedProducts(
        [FromQuery] int limit = 10)
    {
        try
        {
            var products = await _productService.GetFeaturedProductsAsync(limit);
            return Ok(ApiResponse<List<ProductDto>>.SuccessResponse(products));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting featured products");
            return StatusCode(500, ApiResponse<List<ProductDto>>.ErrorResponse(
                "Error retrieving featured products", new List<string> { ex.Message }));
        }
    }

    /// <summary>
    /// GET /api/products/new-arrivals - Get new arrivals
    /// </summary>
    [HttpGet("new-arrivals")]
    public async Task<ActionResult<ApiResponse<List<ProductDto>>>> GetNewArrivals(
        [FromQuery] int limit = 10)
    {
        try
        {
            var products = await _productService.GetNewArrivalsAsync(limit);
            return Ok(ApiResponse<List<ProductDto>>.SuccessResponse(products));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting new arrivals");
            return StatusCode(500, ApiResponse<List<ProductDto>>.ErrorResponse(
                "Error retrieving new arrivals", new List<string> { ex.Message }));
        }
    }

    /// <summary>
    /// POST /api/products - Create new product (supports images list)
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<ProductDto>>> CreateProduct(
        [FromBody] CreateProductWithImagesDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<ProductDto>.ErrorResponse("Invalid data"));

            var product = await _productService.CreateProductAsync(dto);

            if (product == null)
                return StatusCode(500, ApiResponse<ProductDto>.ErrorResponse("Failed to create product"));

            return CreatedAtAction(nameof(GetProduct), new { id = product.Id },
                ApiResponse<ProductDto>.SuccessResponse(product, "Product created successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating product");
            return StatusCode(500, ApiResponse<ProductDto>.ErrorResponse(
                "Error creating product", new List<string> { ex.Message }));
        }
    }

    /// <summary>
    /// PUT /api/products/{id} - Update product
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ProductDto>>> UpdateProduct(
        Guid id, [FromBody] CreateProductDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<ProductDto>.ErrorResponse("Invalid data"));

            var product = await _productService.UpdateProductAsync(id, dto);

            if (product == null)
                return NotFound(ApiResponse<ProductDto>.ErrorResponse("Product not found"));

            return Ok(ApiResponse<ProductDto>.SuccessResponse(product, "Product updated successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating product {ProductId}", id);
            return StatusCode(500, ApiResponse<ProductDto>.ErrorResponse(
                "Error updating product", new List<string> { ex.Message }));
        }
    }

    /// <summary>
    /// DELETE /api/products/{id} - Delete product
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteProduct(Guid id)
    {
        try
        {
            var result = await _productService.DeleteProductAsync(id);

            if (!result)
                return NotFound(ApiResponse<bool>.ErrorResponse("Product not found"));

            return Ok(ApiResponse<bool>.SuccessResponse(true, "Product deleted successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting product {ProductId}", id);
            return StatusCode(500, ApiResponse<bool>.ErrorResponse(
                "Error deleting product", new List<string> { ex.Message }));
        }
    }

    /// <summary>
    /// POST /api/products/upload-images - upload images and return public URLs + thumbnails
    /// </summary>
    [HttpPost("upload-images")]
    public async Task<ActionResult<ApiResponse<List<ImageUploadResult>>>> UploadImages()
    {
        try
        {
            var files = Request.Form.Files;
            if (files == null || files.Count == 0)
                return BadRequest(ApiResponse<List<ImageUploadResult>>.ErrorResponse("No files uploaded"));

            var prefix = Request.Form["prefix"].FirstOrDefault();
            var uploads = await _productService.UploadImagesAsync(files, prefix);
            return Ok(ApiResponse<List<ImageUploadResult>>.SuccessResponse(uploads));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading images");
            return StatusCode(500, ApiResponse<List<ImageUploadResult>>.ErrorResponse("Upload failed", new List<string> { ex.Message }));
        }
    }
}