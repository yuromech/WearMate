using WearMate.ProductAPI.Data;
using WearMate.Shared.DTOs.Products;

namespace WearMate.ProductAPI.Services;

public class CategoryService
{
    private readonly SupabaseClient _supabase;

    public CategoryService(SupabaseClient supabase)
    {
        _supabase = supabase;
    }

    /// <summary>
    /// Get all categories (optionally including inactive)
    /// </summary>
    public async Task<List<CategoryDto>> GetAllCategoriesAsync(bool includeInactive = false)
    {
        var query = _supabase.From("categories");
        
        if (!includeInactive)
        {
            query = query.Eq("is_active", true);
        }
        
        query = query.OrderBy("display_order", true);

        return await _supabase.GetAsync<CategoryDto>(query.Build());
    }

    /// <summary>
    /// Get category by ID
    /// </summary>
    public async Task<CategoryDto?> GetCategoryByIdAsync(Guid id)
    {
        var query = _supabase.From("categories")
            .Eq("id", id);

        return await _supabase.GetSingleAsync<CategoryDto>(query.Build());
    }

    /// <summary>
    /// Get category by slug
    /// </summary>
    public async Task<CategoryDto?> GetCategoryBySlugAsync(string slug)
    {
        var query = _supabase.From("categories")
            .Eq("slug", slug)
            .Eq("is_active", true);

        return await _supabase.GetSingleAsync<CategoryDto>(query.Build());
    }

    /// <summary>
    /// Create category
    /// </summary>
    public async Task<CategoryDto?> CreateCategoryAsync(CreateCategoryDto dto)
    {
        var data = new
        {
            name = dto.Name,
            slug = dto.Slug,
            description = dto.Description,
            parent_id = dto.ParentId,
            display_order = dto.DisplayOrder,
            is_active = dto.IsActive,
            created_at = DateTime.UtcNow
        };

        return await _supabase.PostAsync<CategoryDto>("categories", data);
    }

    /// <summary>
    /// Update category
    /// </summary>
    public async Task<CategoryDto?> UpdateCategoryAsync(Guid id, CreateCategoryDto dto)
    {
        var data = new
        {
            name = dto.Name,
            slug = dto.Slug,
            description = dto.Description,
            parent_id = dto.ParentId,
            display_order = dto.DisplayOrder,
            is_active = dto.IsActive
        };

        return await _supabase.PatchAsync<CategoryDto>("categories", id, data);
    }

    /// <summary>
    /// Delete category (hard delete - permanently removes from database)
    /// </summary>
    public async Task<bool> DeleteCategoryAsync(Guid id)
    {
        return await _supabase.DeleteAsync("categories", id);
    }

    /// <summary>
    /// Reactivate category (set is_active to true)
    /// </summary>
    public async Task<bool> ReactivateCategoryAsync(Guid id)
    {
        var data = new { is_active = true };
        var result = await _supabase.PatchAsync<CategoryDto>("categories", id, data);
        return result != null;
    }

    /// <summary>
    /// Get root categories (no parent)
    /// </summary>
    public async Task<List<CategoryDto>> GetRootCategoriesAsync()
    {
        var query = _supabase.From("categories")
            .Eq("is_active", true)
            .IsNull("parent_id")
            .OrderBy("display_order", true);

        return await _supabase.GetAsync<CategoryDto>(query.Build());
    }

    /// <summary>
    /// Get child categories
    /// </summary>
    public async Task<List<CategoryDto>> GetChildCategoriesAsync(Guid parentId)
    {
        var query = _supabase.From("categories")
            .Eq("is_active", true)
            .Eq("parent_id", parentId)
            .OrderBy("display_order", true);

        return await _supabase.GetAsync<CategoryDto>(query.Build());
    }
}