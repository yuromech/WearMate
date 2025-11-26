using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WearMate.ProductAPI.Services;
using WearMate.Shared.DTOs.Common;
using WearMate.Shared.DTOs.Products;

namespace WearMate.ProductAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly CategoryService _categoryService;
    private readonly ILogger<CategoriesController> _logger;

    public CategoriesController(CategoryService categoryService, ILogger<CategoriesController> logger)
    {
        _categoryService = categoryService;
        _logger = logger;
    }

    /// <summary>
    /// GET /api/categories - Get all categories
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<CategoryDto>>>> GetCategories([FromQuery] bool includeInactive = false, [FromQuery] string? search = null)
    {
        try
        {
            var categories = await _categoryService.GetAllCategoriesAsync(includeInactive, search);
            return Ok(ApiResponse<List<CategoryDto>>.SuccessResponse(categories));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting categories");
            return StatusCode(500, ApiResponse<List<CategoryDto>>.ErrorResponse(
                "Error retrieving categories", new List<string> { ex.Message }));
        }
    }

    /// <summary>
    /// GET /api/categories/root - Get root categories
    /// </summary>
    [HttpGet("root")]
    public async Task<ActionResult<ApiResponse<List<CategoryDto>>>> GetRootCategories()
    {
        try
        {
            var categories = await _categoryService.GetRootCategoriesAsync();
            return Ok(ApiResponse<List<CategoryDto>>.SuccessResponse(categories));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting root categories");
            return StatusCode(500, ApiResponse<List<CategoryDto>>.ErrorResponse(
                "Error retrieving root categories", new List<string> { ex.Message }));
        }
    }

    /// <summary>
    /// GET /api/categories/{id} - Get category by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<CategoryDto>>> GetCategory(Guid id)
    {
        try
        {
            var category = await _categoryService.GetCategoryByIdAsync(id);

            if (category == null)
                return NotFound(ApiResponse<CategoryDto>.ErrorResponse("Category not found"));

            return Ok(ApiResponse<CategoryDto>.SuccessResponse(category));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting category {CategoryId}", id);
            return StatusCode(500, ApiResponse<CategoryDto>.ErrorResponse(
                "Error retrieving category", new List<string> { ex.Message }));
        }
    }

    /// <summary>
    /// GET /api/categories/slug/{slug} - Get category by slug
    /// </summary>
    [HttpGet("slug/{slug}")]
    public async Task<ActionResult<ApiResponse<CategoryDto>>> GetCategoryBySlug(string slug)
    {
        try
        {
            var category = await _categoryService.GetCategoryBySlugAsync(slug);

            if (category == null)
                return NotFound(ApiResponse<CategoryDto>.ErrorResponse("Category not found"));

            return Ok(ApiResponse<CategoryDto>.SuccessResponse(category));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting category by slug {Slug}", slug);
            return StatusCode(500, ApiResponse<CategoryDto>.ErrorResponse(
                "Error retrieving category", new List<string> { ex.Message }));
        }
    }

    /// <summary>
    /// GET /api/categories/{id}/children - Get child categories
    /// </summary>
    [HttpGet("{id:guid}/children")]
    public async Task<ActionResult<ApiResponse<List<CategoryDto>>>> GetChildCategories(Guid id)
    {
        try
        {
            var categories = await _categoryService.GetChildCategoriesAsync(id);
            return Ok(ApiResponse<List<CategoryDto>>.SuccessResponse(categories));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting child categories for {CategoryId}", id);
            return StatusCode(500, ApiResponse<List<CategoryDto>>.ErrorResponse(
                "Error retrieving child categories", new List<string> { ex.Message }));
        }
    }

    /// <summary>
    /// POST /api/categories - Create category
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<CategoryDto>>> CreateCategory(
        [FromBody] CreateCategoryDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<CategoryDto>.ErrorResponse("Invalid data"));

            var category = await _categoryService.CreateCategoryAsync(dto);

            if (category == null)
                return StatusCode(500, ApiResponse<CategoryDto>.ErrorResponse("Failed to create category"));

            return CreatedAtAction(nameof(GetCategory), new { id = category.Id },
                ApiResponse<CategoryDto>.SuccessResponse(category, "Category created successfully"));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<CategoryDto>.ErrorResponse(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ApiResponse<CategoryDto>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating category");
            return StatusCode(500, ApiResponse<CategoryDto>.ErrorResponse(
                "Error creating category", new List<string> { ex.Message }));
        }
    }

    /// <summary>
    /// PUT /api/categories/{id} - Update category
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<CategoryDto>>> UpdateCategory(
        Guid id, [FromBody] CreateCategoryDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<CategoryDto>.ErrorResponse("Invalid data"));

            var category = await _categoryService.UpdateCategoryAsync(id, dto);

            if (category == null)
                return NotFound(ApiResponse<CategoryDto>.ErrorResponse("Category not found"));

            return Ok(ApiResponse<CategoryDto>.SuccessResponse(category, "Category updated successfully"));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<CategoryDto>.ErrorResponse(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ApiResponse<CategoryDto>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating category {CategoryId}", id);
            return StatusCode(500, ApiResponse<CategoryDto>.ErrorResponse(
                "Error updating category", new List<string> { ex.Message }));
        }
    }

    /// <summary>
    /// DELETE /api/categories/{id} - Delete category permanently
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteCategory(Guid id)
    {
        try
        {
            var result = await _categoryService.DeleteCategoryAsync(id);

            if (!result)
                return NotFound(ApiResponse<bool>.ErrorResponse("Category not found"));

            return Ok(ApiResponse<bool>.SuccessResponse(true, "Category deleted permanently"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<bool>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting category {CategoryId}", id);
            return StatusCode(500, ApiResponse<bool>.ErrorResponse(
                "Error deleting category", new List<string> { ex.Message }));
        }
    }

    /// <summary>
    /// PATCH /api/categories/{id}/reactivate - Reactivate category
    /// </summary>
    [HttpPatch("{id:guid}/reactivate")]
    public async Task<ActionResult<ApiResponse<bool>>> ReactivateCategory(Guid id)
    {
        try
        {
            var result = await _categoryService.ReactivateCategoryAsync(id);

            if (!result)
                return NotFound(ApiResponse<bool>.ErrorResponse("Category not found"));

            return Ok(ApiResponse<bool>.SuccessResponse(true, "Category reactivated successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reactivating category {CategoryId}", id);
            return StatusCode(500, ApiResponse<bool>.ErrorResponse(
                "Error reactivating category", new List<string> { ex.Message }));
        }
    }

    /// <summary>
    /// PATCH /api/categories/{id}/deactivate - Deactivate category
    /// </summary>
    [HttpPatch("{id:guid}/deactivate")]
    public async Task<ActionResult<ApiResponse<bool>>> DeactivateCategory(Guid id)
    {
        try
        {
            var result = await _categoryService.DeactivateCategoryAsync(id);

            if (!result)
                return NotFound(ApiResponse<bool>.ErrorResponse("Category not found"));

            return Ok(ApiResponse<bool>.SuccessResponse(true, "Category deactivated successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating category {CategoryId}", id);
            return StatusCode(500, ApiResponse<bool>.ErrorResponse(
                "Error deactivating category", new List<string> { ex.Message }));
        }
    }

    /// <summary>
    /// POST /api/categories/upload-image - upload an image and return public URL
    /// </summary>
    [HttpPost("upload-image")]
    public async Task<ActionResult<ApiResponse<string>>> UploadImage([FromForm] IFormFile file, [FromForm] string? currentImageUrl = null)
    {
        try
        {
            if (file == null)
                return BadRequest(ApiResponse<string>.ErrorResponse("No file uploaded"));

            var url = await _categoryService.UploadCategoryImageAsync(file, currentImageUrl);
            return Ok(ApiResponse<string>.SuccessResponse(url ?? string.Empty, "Uploaded category image successfully"));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<string>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading category image");
            return StatusCode(500, ApiResponse<string>.ErrorResponse(
                "Error uploading image", new List<string> { ex.Message }));
        }
    }
}
