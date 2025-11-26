using System.Text;
using Microsoft.AspNetCore.Http;
using WearMate.ProductAPI.Data;
using WearMate.Shared.DTOs.Products;
using WearMate.Shared.Helpers;

namespace WearMate.ProductAPI.Services;

public class CategoryService
{
    private readonly SupabaseClient _supabase;
    private readonly IConfiguration _config;

    public CategoryService(SupabaseClient supabase, IConfiguration config)
    {
        _supabase = supabase;
        _config = config;
    }

    /// <summary>
    /// Get all categories (optionally including inactive) with optional keyword filter
    /// </summary>
    public async Task<List<CategoryDto>> GetAllCategoriesAsync(bool includeInactive = false, string? search = null)
    {
        var query = _supabase.From("categories");

        if (!includeInactive)
        {
            query = query.Eq("is_active", true);
        }

        query = query.OrderBy("display_order", true);

        var categories = await _supabase.GetAsync<CategoryDto>(query.Build());

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim().ToLowerInvariant();
            categories = categories
                .Where(c =>
                    (!string.IsNullOrWhiteSpace(c.Name) && c.Name.ToLowerInvariant().Contains(keyword)) ||
                    (!string.IsNullOrWhiteSpace(c.Slug) && c.Slug.ToLowerInvariant().Contains(keyword)))
                .ToList();
        }

        return categories;
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
    /// Get category by slug (active only)
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
        ValidateName(dto.Name);

        if (dto.ParentId.HasValue)
        {
            await ValidateParentAsync(dto.ParentId.Value);
        }

        var userProvidedSlug = !string.IsNullOrWhiteSpace(dto.Slug);
        var baseSlug = SlugHelper.Slugify(userProvidedSlug ? dto.Slug : dto.Name, "category");
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
            image_url = dto.ImageUrl,
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
        ValidateName(dto.Name);

        var existing = await GetCategoryByIdAsync(id);
        if (existing == null)
            return null;

        if (dto.ParentId.HasValue)
        {
            await ValidateParentAsync(dto.ParentId.Value, id);
        }

        var userProvidedSlug = !string.IsNullOrWhiteSpace(dto.Slug);
        var baseSlug = SlugHelper.Slugify(userProvidedSlug ? dto.Slug : dto.Name, "category");
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
            image_url = dto.ImageUrl,
            parent_id = dto.ParentId,
            display_order = dto.DisplayOrder,
            is_active = dto.IsActive
        };

        var updated = await _supabase.PatchAsync<CategoryDto>("categories", id, data);

        if (updated != null &&
            !string.IsNullOrWhiteSpace(existing.ImageUrl) &&
            !string.IsNullOrWhiteSpace(dto.ImageUrl) &&
            !string.Equals(existing.ImageUrl, dto.ImageUrl, StringComparison.OrdinalIgnoreCase))
        {
            await DeleteImageAsync(existing.ImageUrl);
        }

        return updated;
    }

    /// <summary>
    /// Delete category (hard delete - only when no dependencies)
    /// </summary>
    public async Task<bool> DeleteCategoryAsync(Guid id)
    {
        if (await HasChildCategoriesAsync(id))
            throw new InvalidOperationException("Cannot delete category that still has child categories.");

        if (await HasProductsAsync(id))
            throw new InvalidOperationException("Cannot delete category that still has products.");

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
    /// Deactivate category (set is_active to false)
    /// </summary>
    public async Task<bool> DeactivateCategoryAsync(Guid id)
    {
        var data = new { is_active = false };
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

    /// <summary>
    /// Upload a single image to Supabase Storage (for categories)
    /// </summary>
    public async Task<string?> UploadCategoryImageAsync(IFormFile file, string? currentImageUrl = null)
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
        var path = $"categories/{fileName}".Replace("\\", "/");

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms);
        var data = ms.ToArray();

        var imageUrl = await _supabase.UploadFileAsync(bucket, path, data, file.ContentType ?? "application/octet-stream");

        if (!string.IsNullOrWhiteSpace(currentImageUrl))
        {
            await DeleteImageAsync(currentImageUrl);
        }

        return imageUrl;
    }

    private void ValidateName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required", nameof(name));
    }

    private async Task ValidateParentAsync(Guid parentId, Guid? currentId = null)
    {
        var parent = await GetCategoryByIdAsync(parentId);
        if (parent == null)
            throw new InvalidOperationException("Parent category not found");

        if (currentId.HasValue)
        {
            if (parentId == currentId.Value)
                throw new InvalidOperationException("Parent category cannot be itself");

            if (await CausesCycleAsync(currentId.Value, parentId))
                throw new InvalidOperationException("Parent category cannot be a descendant of the current category");
        }
    }

    private async Task<bool> CausesCycleAsync(Guid categoryId, Guid? newParentId)
    {
        var cursor = newParentId;
        while (cursor.HasValue)
        {
            if (cursor.Value == categoryId)
                return true;

            var parent = await GetCategoryByIdAsync(cursor.Value);
            cursor = parent?.ParentId;
        }

        return false;
    }

    private async Task<bool> HasChildCategoriesAsync(Guid id)
    {
        var query = _supabase.From("categories").Eq("parent_id", id);
        var children = await _supabase.GetAsync<CategoryDto>(query.Build());
        return children.Any();
    }

    private async Task<bool> HasProductsAsync(Guid id)
    {
        var query = _supabase.From("products").Eq("category_id", id);
        var count = await _supabase.GetCountAsync(query.Build());
        return count > 0;
    }

    private async Task<string> GenerateUniqueSlugAsync(string input, Guid? currentId = null)
    {
        var baseSlug = SlugHelper.Slugify(input, "category");
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
        var query = _supabase.From("categories")
            .Eq("slug", slug);

        var existing = await _supabase.GetAsync<CategoryDto>(query.Build());

        if (existing == null || existing.Count == 0)
            return false;

        if (currentId.HasValue && existing.Any(c => c.Id == currentId.Value))
            return false;

        return true;
    }

    private async Task DeleteImageAsync(string? imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
            return;

        var bucket = _config["SUPABASE_BUCKET"] ?? "wear-mate";
        var storagePath = ImageHelper.ExtractStoragePath(imageUrl);

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
