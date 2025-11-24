using Microsoft.AspNetCore.Mvc;
using WearMate.ProductAPI.Services;
using WearMate.Shared.DTOs.Common;
using WearMate.Shared.DTOs.Products;

namespace WearMate.ProductAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BrandsController : ControllerBase
{
    private readonly BrandService _brandService;
    private readonly ILogger<BrandsController> _logger;

    public BrandsController(BrandService brandService, ILogger<BrandsController> logger)
    {
        _brandService = brandService;
        _logger = logger;
    }

    /// <summary>
    /// GET /api/brands - Get all brands
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<BrandDto>>>> GetBrands()
    {
        try
        {
            var brands = await _brandService.GetAllBrandsAsync();
            return Ok(ApiResponse<List<BrandDto>>.SuccessResponse(brands));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting brands");
            return StatusCode(500, ApiResponse<List<BrandDto>>.ErrorResponse(
                "Error retrieving brands", new List<string> { ex.Message }));
        }
    }

    /// <summary>
    /// GET /api/brands/{id} - Get brand by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<BrandDto>>> GetBrand(Guid id)
    {
        try
        {
            var brand = await _brandService.GetBrandByIdAsync(id);

            if (brand == null)
                return NotFound(ApiResponse<BrandDto>.ErrorResponse("Brand not found"));

            return Ok(ApiResponse<BrandDto>.SuccessResponse(brand));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting brand {BrandId}", id);
            return StatusCode(500, ApiResponse<BrandDto>.ErrorResponse(
                "Error retrieving brand", new List<string> { ex.Message }));
        }
    }

    /// <summary>
    /// GET /api/brands/slug/{slug} - Get brand by slug
    /// </summary>
    [HttpGet("slug/{slug}")]
    public async Task<ActionResult<ApiResponse<BrandDto>>> GetBrandBySlug(string slug)
    {
        try
        {
            var brand = await _brandService.GetBrandBySlugAsync(slug);

            if (brand == null)
                return NotFound(ApiResponse<BrandDto>.ErrorResponse("Brand not found"));

            return Ok(ApiResponse<BrandDto>.SuccessResponse(brand));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting brand by slug {Slug}", slug);
            return StatusCode(500, ApiResponse<BrandDto>.ErrorResponse(
                "Error retrieving brand", new List<string> { ex.Message }));
        }
    }

    /// <summary>
    /// POST /api/brands - Create brand
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<BrandDto>>> CreateBrand(
        [FromBody] CreateBrandDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<BrandDto>.ErrorResponse("Invalid data"));

            var brand = await _brandService.CreateBrandAsync(dto);

            if (brand == null)
                return StatusCode(500, ApiResponse<BrandDto>.ErrorResponse("Failed to create brand"));

            return CreatedAtAction(nameof(GetBrand), new { id = brand.Id },
                ApiResponse<BrandDto>.SuccessResponse(brand, "Brand created successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating brand");
            return StatusCode(500, ApiResponse<BrandDto>.ErrorResponse(
                "Error creating brand", new List<string> { ex.Message }));
        }
    }

    /// <summary>
    /// PUT /api/brands/{id} - Update brand
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<BrandDto>>> UpdateBrand(
        Guid id, [FromBody] CreateBrandDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<BrandDto>.ErrorResponse("Invalid data"));

            var brand = await _brandService.UpdateBrandAsync(id, dto);

            if (brand == null)
                return NotFound(ApiResponse<BrandDto>.ErrorResponse("Brand not found"));

            return Ok(ApiResponse<BrandDto>.SuccessResponse(brand, "Brand updated successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating brand {BrandId}", id);
            return StatusCode(500, ApiResponse<BrandDto>.ErrorResponse(
                "Error updating brand", new List<string> { ex.Message }));
        }
    }

    /// <summary>
    /// DELETE /api/brands/{id} - Delete brand
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteBrand(Guid id)
    {
        try
        {
            var result = await _brandService.DeleteBrandAsync(id);

            if (!result)
                return NotFound(ApiResponse<bool>.ErrorResponse("Brand not found"));

            return Ok(ApiResponse<bool>.SuccessResponse(true, "Brand deleted successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting brand {BrandId}", id);
            return StatusCode(500, ApiResponse<bool>.ErrorResponse(
                "Error deleting brand", new List<string> { ex.Message }));
        }
    }
}