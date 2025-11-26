using Microsoft.AspNetCore.Http;
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
    public async Task<ActionResult<ApiResponse<List<BrandDto>>>> GetBrands([FromQuery] bool includeInactive = false, [FromQuery] string? search = null)
    {
        try
        {
            var brands = await _brandService.GetAllBrandsAsync(includeInactive, search);
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
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<BrandDto>.ErrorResponse(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ApiResponse<BrandDto>.ErrorResponse(ex.Message));
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
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<BrandDto>.ErrorResponse(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ApiResponse<BrandDto>.ErrorResponse(ex.Message));
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
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<bool>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting brand {BrandId}", id);
            return StatusCode(500, ApiResponse<bool>.ErrorResponse(
                "Error deleting brand", new List<string> { ex.Message }));
        }
    }

    /// <summary>
    /// PATCH /api/brands/{id}/reactivate - Reactivate brand
    /// </summary>
    [HttpPatch("{id:guid}/reactivate")]
    public async Task<ActionResult<ApiResponse<bool>>> ReactivateBrand(Guid id)
    {
        try
        {
            var result = await _brandService.ReactivateBrandAsync(id);
            if (!result)
                return NotFound(ApiResponse<bool>.ErrorResponse("Brand not found"));

            return Ok(ApiResponse<bool>.SuccessResponse(true, "Brand reactivated successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reactivating brand {BrandId}", id);
            return StatusCode(500, ApiResponse<bool>.ErrorResponse(
                "Error reactivating brand", new List<string> { ex.Message }));
        }
    }

    /// <summary>
    /// PATCH /api/brands/{id}/deactivate - Deactivate brand
    /// </summary>
    [HttpPatch("{id:guid}/deactivate")]
    public async Task<ActionResult<ApiResponse<bool>>> DeactivateBrand(Guid id)
    {
        try
        {
            var result = await _brandService.DeactivateBrandAsync(id);
            if (!result)
                return NotFound(ApiResponse<bool>.ErrorResponse("Brand not found"));

            return Ok(ApiResponse<bool>.SuccessResponse(true, "Brand deactivated successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating brand {BrandId}", id);
            return StatusCode(500, ApiResponse<bool>.ErrorResponse(
                "Error deactivating brand", new List<string> { ex.Message }));
        }
    }

    /// <summary>
    /// POST /api/brands/upload-logo - upload logo and return URL
    /// </summary>
    [HttpPost("upload-logo")]
    public async Task<ActionResult<ApiResponse<string>>> UploadLogo([FromForm] IFormFile file, [FromForm] string? currentLogoUrl = null)
    {
        try
        {
            if (file == null)
                return BadRequest(ApiResponse<string>.ErrorResponse("No file uploaded"));

            var url = await _brandService.UploadLogoAsync(file, currentLogoUrl);
            return Ok(ApiResponse<string>.SuccessResponse(url ?? string.Empty, "Uploaded brand logo successfully"));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<string>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading brand logo");
            return StatusCode(500, ApiResponse<string>.ErrorResponse(
                "Error uploading logo", new List<string> { ex.Message }));
        }
    }
}
