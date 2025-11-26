using Microsoft.AspNetCore.Http;
using WearMate.ProductAPI.Data;
using WearMate.Shared.DTOs.Products;
using WearMate.Shared.Helpers;

namespace WearMate.ProductAPI.Services;

public class BrandService
{
    private readonly SupabaseClient _supabase;
    private readonly IConfiguration _config;

    public BrandService(SupabaseClient supabase, IConfiguration config)
    {
        _supabase = supabase;
        _config = config;
    }

    /// <summary>
    /// Get all brands (optionally include inactive) with optional keyword search.
    /// </summary>
    public async Task<List<BrandDto>> GetAllBrandsAsync(bool includeInactive = false, string? search = null)
    {
        var query = _supabase.From("brands");

        if (!includeInactive)
            query = query.Eq("is_active", true);

        query = query.OrderBy("name", true);
        var brands = await _supabase.GetAsync<BrandDto>(query.Build());

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim().ToLowerInvariant();
            brands = brands.Where(b =>
                (!string.IsNullOrWhiteSpace(b.Name) && b.Name.ToLowerInvariant().Contains(keyword)) ||
                (!string.IsNullOrWhiteSpace(b.Slug) && b.Slug.ToLowerInvariant().Contains(keyword))
            ).ToList();
        }

        return brands;
    }

    public async Task<BrandDto?> GetBrandByIdAsync(Guid id)
    {
        var query = _supabase.From("brands").Eq("id", id);
        return await _supabase.GetSingleAsync<BrandDto>(query.Build());
    }

    public async Task<BrandDto?> GetBrandBySlugAsync(string slug)
    {
        var query = _supabase.From("brands")
            .Eq("slug", slug)
            .Eq("is_active", true);

        return await _supabase.GetSingleAsync<BrandDto>(query.Build());
    }

    public async Task<BrandDto?> CreateBrandAsync(CreateBrandDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new ArgumentException("Name is required", nameof(dto.Name));

        var userProvidedSlug = !string.IsNullOrWhiteSpace(dto.Slug);
        var baseSlug = SlugHelper.Slugify(userProvidedSlug ? dto.Slug : dto.Name, "brand");
        string slug;

        if (userProvidedSlug)
        {
            if (await SlugExistsAsync(baseSlug, null))
                throw new InvalidOperationException("Slug already exists. Please choose another.");
            slug = baseSlug;
        }
        else
        {
            slug = await GenerateUniqueSlugAsync(baseSlug);
        }

        var data = new
        {
            name = dto.Name.Trim(),
            slug,
            description = dto.Description,
            logo_url = dto.LogoUrl,
            is_active = dto.IsActive,
            created_at = DateTime.UtcNow
        };

        return await _supabase.PostAsync<BrandDto>("brands", data);
    }

    public async Task<BrandDto?> UpdateBrandAsync(Guid id, CreateBrandDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new ArgumentException("Name is required", nameof(dto.Name));

        var existing = await GetBrandByIdAsync(id);
        if (existing == null) return null;

        var userProvidedSlug = !string.IsNullOrWhiteSpace(dto.Slug);
        var baseSlug = SlugHelper.Slugify(userProvidedSlug ? dto.Slug : dto.Name, "brand");
        string slug;

        if (userProvidedSlug)
        {
            if (await SlugExistsAsync(baseSlug, id))
                throw new InvalidOperationException("Slug already exists. Please choose another.");
            slug = baseSlug;
        }
        else
        {
            slug = await GenerateUniqueSlugAsync(baseSlug, id);
        }

        var data = new
        {
            name = dto.Name.Trim(),
            slug,
            description = dto.Description,
            logo_url = dto.LogoUrl,
            is_active = dto.IsActive
        };

        var updated = await _supabase.PatchAsync<BrandDto>("brands", id, data);

        if (updated != null &&
            !string.IsNullOrWhiteSpace(existing.LogoUrl) &&
            !string.IsNullOrWhiteSpace(dto.LogoUrl) &&
            !string.Equals(existing.LogoUrl, dto.LogoUrl, StringComparison.OrdinalIgnoreCase))
        {
            await DeleteLogoAsync(existing.LogoUrl);
        }

        return updated;
    }

    public async Task<bool> DeleteBrandAsync(Guid id)
    {
        // ensure no products use this brand
        var productQuery = _supabase.From("products").Eq("brand_id", id);
        var count = await _supabase.GetCountAsync(productQuery.Build());
        if (count > 0)
            throw new InvalidOperationException("Cannot delete brand that is linked to products.");

        var data = new { is_active = false };
        var result = await _supabase.PatchAsync<BrandDto>("brands", id, data);
        return result != null;
    }

    public async Task<bool> ReactivateBrandAsync(Guid id)
    {
        var data = new { is_active = true };
        var result = await _supabase.PatchAsync<BrandDto>("brands", id, data);
        return result != null;
    }

    public async Task<bool> DeactivateBrandAsync(Guid id)
    {
        var data = new { is_active = false };
        var result = await _supabase.PatchAsync<BrandDto>("brands", id, data);
        return result != null;
    }

    public async Task<string?> UploadLogoAsync(IFormFile file, string? currentLogoUrl = null)
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("File is empty");

        if (!ImageHelper.ValidateContentType(file.ContentType) || !ImageHelper.ValidateFileFormat(file.FileName))
            throw new ArgumentException("Invalid image type");

        var maxBytes = ImageHelper.MAX_FILE_SIZE_MB * 1024 * 1024;
        if (file.Length > maxBytes)
            throw new ArgumentException($"Image exceeds {ImageHelper.MAX_FILE_SIZE_MB}MB limit");

        var bucket = _config["SUPABASE_BUCKET"] ?? "wear-mate";
        var ext = Path.GetExtension(file.FileName);
        var fileName = ImageHelper.GenerateGuidFileName(ext);
        var path = $"brands/{fileName}".Replace("\\", "/");

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms);
        var data = ms.ToArray();

        var logoUrl = await _supabase.UploadFileAsync(bucket, path, data, file.ContentType ?? "application/octet-stream");

        if (!string.IsNullOrWhiteSpace(currentLogoUrl))
        {
            await DeleteLogoAsync(currentLogoUrl);
        }

        return logoUrl;
    }

    private async Task<string> GenerateUniqueSlugAsync(string input, Guid? currentId = null)
    {
        var baseSlug = SlugHelper.Slugify(input, "brand");
        var slug = baseSlug;
        var counter = 1;

        while (await SlugExistsAsync(slug, currentId))
        {
            slug = $"{baseSlug}-{counter++}";
        }

        return slug;
    }

    private async Task<bool> SlugExistsAsync(string slug, Guid? currentId)
    {
        var query = _supabase.From("brands").Eq("slug", slug);
        var existing = await _supabase.GetAsync<BrandDto>(query.Build());

        if (existing == null || existing.Count == 0)
            return false;

        if (currentId.HasValue && existing.Any(b => b.Id == currentId.Value))
            return false;

        return true;
    }

    private async Task DeleteLogoAsync(string? logoUrl)
    {
        if (string.IsNullOrWhiteSpace(logoUrl))
            return;

        var bucket = _config["SUPABASE_BUCKET"] ?? "wear-mate";
        var storagePath = ImageHelper.ExtractStoragePath(logoUrl);
        if (string.IsNullOrWhiteSpace(storagePath))
            return;

        try
        {
            await _supabase.DeleteFileAsync(bucket, storagePath);
        }
        catch
        {
        }
    }
}
