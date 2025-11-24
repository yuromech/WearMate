using WearMate.ProductAPI.Data;
using WearMate.Shared.DTOs.Products;

namespace WearMate.ProductAPI.Services;

public class BrandService
{
    private readonly SupabaseClient _supabase;

    public BrandService(SupabaseClient supabase)
    {
        _supabase = supabase;
    }

    /// <summary>
    /// Get all active brands
    /// </summary>
    public async Task<List<BrandDto>> GetAllBrandsAsync()
    {
        var query = _supabase.From("brands")
            .Eq("is_active", true)
            .OrderBy("name", true);

        return await _supabase.GetAsync<BrandDto>(query.Build());
    }

    /// <summary>
    /// Get brand by ID
    /// </summary>
    public async Task<BrandDto?> GetBrandByIdAsync(Guid id)
    {
        var query = _supabase.From("brands")
            .Eq("id", id);

        return await _supabase.GetSingleAsync<BrandDto>(query.Build());
    }

    /// <summary>
    /// Get brand by slug
    /// </summary>
    public async Task<BrandDto?> GetBrandBySlugAsync(string slug)
    {
        var query = _supabase.From("brands")
            .Eq("slug", slug)
            .Eq("is_active", true);

        return await _supabase.GetSingleAsync<BrandDto>(query.Build());
    }

    /// <summary>
    /// Create brand
    /// </summary>
    public async Task<BrandDto?> CreateBrandAsync(CreateBrandDto dto)
    {
        var data = new
        {
            name = dto.Name,
            slug = dto.Slug,
            description = dto.Description,
            is_active = dto.IsActive,
            created_at = DateTime.UtcNow
        };

        return await _supabase.PostAsync<BrandDto>("brands", data);
    }

    /// <summary>
    /// Update brand
    /// </summary>
    public async Task<BrandDto?> UpdateBrandAsync(Guid id, CreateBrandDto dto)
    {
        var data = new
        {
            name = dto.Name,
            slug = dto.Slug,
            description = dto.Description,
            is_active = dto.IsActive
        };

        return await _supabase.PatchAsync<BrandDto>("brands", id, data);
    }

    /// <summary>
    /// Delete brand (soft delete)
    /// </summary>
    public async Task<bool> DeleteBrandAsync(Guid id)
    {
        var data = new { is_active = false };
        var result = await _supabase.PatchAsync<BrandDto>("brands", id, data);
        return result != null;
    }
}