using System.Text;
using Microsoft.AspNetCore.Http;
using WearMate.ProductAPI.Data;
using WearMate.Shared.DTOs.Common;
using WearMate.Shared.DTOs.Products;
using WearMate.Shared.Helpers;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;

namespace WearMate.ProductAPI.Services;

public partial class ProductService
{
    private readonly SupabaseClient _supabase;
    private readonly IConfiguration _config;

    private readonly long _maxFileSize;
    private readonly string[] _allowedMime = new[] { "image/jpeg", "image/jpg", "image/png", "image/webp" };
    private readonly int _thumbWidth;
    private readonly int _thumbHeight;
    private readonly int _smallThumbWidth;
    private readonly int _smallThumbHeight;

    public ProductService(SupabaseClient supabase, IConfiguration config)
    {
        _supabase = supabase;
        _config = config;

        _maxFileSize = long.TryParse(_config["MAX_UPLOAD_SIZE_BYTES"], out var ms) ? ms : 5 * 1024 * 1024;
        _thumbWidth = int.TryParse(_config["THUMB_WIDTH"], out var tw) ? tw : 1200;
        _thumbHeight = int.TryParse(_config["THUMB_HEIGHT"], out var th) ? th : 1200;
        _smallThumbWidth = int.TryParse(_config["SMALL_THUMB_WIDTH"], out var stw) ? stw : 300;
        _smallThumbHeight = int.TryParse(_config["SMALL_THUMB_HEIGHT"], out var sth) ? sth : 300;
    }

    public async Task<PaginatedResult<ProductDto>> GetProductsAsync(
        int page = 1,
        int pageSize = 20,
        Guid? categoryId = null,
        Guid? brandId = null,
        string? search = null,
        bool? isFeatured = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        string? sortBy = null,
        bool? isActive = true)
    {
        page = PaginationHelper.ValidatePage(page);
        pageSize = PaginationHelper.ValidatePageSize(pageSize);
        var offset = PaginationHelper.CalculateOffset(page, pageSize);

        var query = _supabase.From("products");
        query.Select("*", "category:categories(*)", "brand:brands(*)");

        if (isActive.HasValue)
            query.Eq("is_active", isActive.Value);

        if (categoryId.HasValue)
            query.Eq("category_id", categoryId.Value);

        if (brandId.HasValue)
            query.Eq("brand_id", brandId.Value);

        if (!string.IsNullOrEmpty(search))
            query.ILike("name", search);

        if (isFeatured.HasValue)
            query.Eq("is_featured", isFeatured.Value);

        if (minPrice.HasValue)
            query.Gte("base_price", minPrice.Value);

        if (maxPrice.HasValue)
            query.Lte("base_price", maxPrice.Value);

        switch (sortBy?.ToLower())
        {
            case "price_asc":
                query.OrderBy("base_price", true);
                break;
            case "price_desc":
                query.OrderBy("base_price", false);
                break;
            case "name":
                query.OrderBy("name", true);
                break;
            case "newest":
                query.OrderBy("created_at", false);
                break;
            default:
                query.OrderBy("created_at", false);
                break;
        }

        query.Limit(pageSize).Offset(offset);

        var url = query.Build();
        var products = await _supabase.GetAsync<ProductDto>(url);

        var countQuery = _supabase.From("products");
        if (isActive.HasValue)
            countQuery.Eq("is_active", isActive.Value);
        if (categoryId.HasValue) countQuery.Eq("category_id", categoryId.Value);
        if (brandId.HasValue) countQuery.Eq("brand_id", brandId.Value);
        if (!string.IsNullOrEmpty(search)) countQuery.ILike("name", search);
        if (isFeatured.HasValue) countQuery.Eq("is_featured", isFeatured.Value);

        var countUrl = countQuery.Build();
        var totalCount = await _supabase.GetCountAsync(countUrl);

        return PaginationHelper.CreateResult(products, totalCount, page, pageSize);
    }

    public async Task<ProductDto?> GetProductByIdAsync(Guid id)
    {
        var query = _supabase.From("products")
            .Eq("id", id);

        var product = await _supabase.GetSingleAsync<ProductDto>(query.Build());

        if (product == null) return null;

        if (product.CategoryId.HasValue)
        {
            var categoryQuery = _supabase.From("categories")
                .Eq("id", product.CategoryId.Value);
            product.Category = await _supabase.GetSingleAsync<CategoryDto>(categoryQuery.Build());
        }

        if (product.BrandId.HasValue)
        {
            var brandQuery = _supabase.From("brands")
                .Eq("id", product.BrandId.Value);
            product.Brand = await _supabase.GetSingleAsync<BrandDto>(brandQuery.Build());
        }

        var imagesQuery = _supabase.From("product_images")
            .Eq("product_id", id)
            .OrderBy("display_order", true);
        product.Images = await _supabase.GetAsync<ProductImageDto>(imagesQuery.Build());

        var variantsQuery = _supabase.From("product_variants")
            .Eq("product_id", id)
            .Eq("is_active", true);
        product.Variants = await _supabase.GetAsync<ProductVariantDto>(variantsQuery.Build());

        await IncrementViewCountAsync(id);

        return product;
    }

    public async Task<ProductDto?> GetProductBySlugAsync(string slug)
    {
        var query = _supabase.From("products")
            .Eq("slug", slug)
            .Eq("is_active", true);

        var product = await _supabase.GetSingleAsync<ProductDto>(query.Build());

        if (product == null) return null;

        return await GetProductByIdAsync(product.Id);
    }

    public async Task<ProductDto?> CreateProductAsync(CreateProductWithImagesDto dto)
    {
        // Slug safety: generate and ensure uniqueness
        var baseSlug = !string.IsNullOrWhiteSpace(dto.Slug) ? dto.Slug : dto.Name;
        var slug = await GenerateUniqueSlugAsync(baseSlug ?? Guid.NewGuid().ToString());

        // Thumbnail rule: if no explicit thumbnail, use first uploaded image, else use default
        var thumbnail = dto.ThumbnailUrl;
        if (string.IsNullOrWhiteSpace(thumbnail) && dto.Images != null && dto.Images.Any())
        {
            thumbnail = dto.Images.First();
        }
        else if (string.IsNullOrWhiteSpace(thumbnail))
        {
            // Use default image if no images provided
            thumbnail = ImageHelper.GetProductImage(null);
        }

        var data = new
        {
            category_id = dto.CategoryId,
            brand_id = dto.BrandId,
            name = dto.Name,
            slug = slug,
            description = dto.Description,
            short_description = dto.ShortDescription,
            base_price = dto.BasePrice,
            sale_price = dto.SalePrice,
            sku = dto.Sku,
            is_featured = dto.IsFeatured,
            is_active = dto.IsActive,
            thumbnail_url = thumbnail,
            created_at = DateTime.UtcNow,
            updated_at = DateTime.UtcNow
        };

        var product = await _supabase.PostAsync<ProductDto>("products", data);

        if (product != null && dto.Images != null && dto.Images.Any())
        {
            int order = 0; // Start from 0 as per requirements
            foreach (var imgUrl in dto.Images)
            {
                var imgData = new
                {
                    product_id = product.Id,
                    image_url = imgUrl,
                    display_order = order++,
                    created_at = DateTime.UtcNow
                };
                await _supabase.PostAsync<ProductImageDto>("product_images", imgData);
            }

            var imagesQuery = _supabase.From("product_images").Eq("product_id", product.Id).OrderBy("display_order", true);
            product.Images = await _supabase.GetAsync<ProductImageDto>(imagesQuery.Build());
        }

        // Create variants if provided
        if (product != null && dto.Variants != null && dto.Variants.Any())
        {
            foreach (var variantDto in dto.Variants)
            {
                variantDto.ProductId = product.Id;
                await CreateVariantAsync(variantDto);
            }

            // Load created variants
            var variantsQuery = _supabase.From("product_variants").Eq("product_id", product.Id).Eq("is_active", true);
            product.Variants = await _supabase.GetAsync<ProductVariantDto>(variantsQuery.Build());
        }

        return product;
    }

    public async Task<ProductDto?> UpdateProductAsync(Guid id, CreateProductDto dto)
    {
        var baseSlug = !string.IsNullOrWhiteSpace(dto.Slug) ? dto.Slug : dto.Name;
        var slug = await GenerateUniqueSlugAsync(baseSlug ?? Guid.NewGuid().ToString(), id);

        var data = new
        {
            category_id = dto.CategoryId,
            brand_id = dto.BrandId,
            name = dto.Name,
            slug = slug,
            description = dto.Description,
            short_description = dto.ShortDescription,
            base_price = dto.BasePrice,
            sale_price = dto.SalePrice,
            sku = dto.Sku,
            is_featured = dto.IsFeatured,
            is_active = dto.IsActive,
            updated_at = DateTime.UtcNow
        };

        return await _supabase.PatchAsync<ProductDto>("products", id, data);
    }

    public async Task<bool> DeleteProductAsync(Guid id)
    {
        var product = await GetProductByIdAsync(id);
        if (product == null) return false;

        // Delete all product images from Storage and DB
        var bucket = _config["SUPABASE_BUCKET"] ?? "wear-mate";
        var images = await GetImagesByProductAsync(id);
        
        foreach (var img in images)
        {
            // Extract storage path and delete from Storage
            var storagePath = ImageHelper.ExtractStoragePath(img.ImageUrl);
            if (!string.IsNullOrWhiteSpace(storagePath))
            {
                try
                {
                    await _supabase.DeleteFileAsync(bucket, storagePath);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to delete image from Storage: {storagePath}, Error: {ex.Message}");
                }
            }
            
            // Delete from DB
            await _supabase.DeleteAsync("product_images", img.Id);
        }

        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var newSlug = $"{product.Slug}-deleted-{timestamp}";

        var data = new
        {
            is_active = false,
            slug = newSlug,
            updated_at = DateTime.UtcNow
        };

        var result = await _supabase.PatchAsync<ProductDto>("products", id, data);
        return result != null;
    }

    private async Task IncrementViewCountAsync(Guid id)
    {
        try
        {
            var query = _supabase.From("products")
                .Select("view_count")
                .Eq("id", id);

            var product = await _supabase.GetSingleAsync<ProductDto>(query.Build());

            if (product != null)
            {
                var data = new { view_count = product.ViewCount + 1 };
                await _supabase.PatchAsync<ProductDto>("products", id, data);
            }
        }
        catch
        {
        }
    }

    public async Task<List<ProductDto>> GetFeaturedProductsAsync(int limit = 10)
    {
        var query = _supabase.From("products")
            .Eq("is_active", true)
            .Eq("is_featured", true)
            .OrderBy("created_at", false)
            .Limit(limit);

        return await _supabase.GetAsync<ProductDto>(query.Build());
    }

    public async Task<List<ProductDto>> GetNewArrivalsAsync(int limit = 10)
    {
        var query = _supabase.From("products")
            .Eq("is_active", true)
            .OrderBy("created_at", false)
            .Limit(limit);

        return await _supabase.GetAsync<ProductDto>(query.Build());
    }

    public async Task<List<ImageUploadResult>> UploadImagesAsync(IFormFileCollection files, string prefix = "product")
    {
        var result = new List<ImageUploadResult>();
        var bucket = _config["SUPABASE_BUCKET"] ?? "wear-mate";
        
        // Use prefix as-is (should be product UID/GUID)
        // If it's a valid GUID, use it; otherwise use "temp" folder
        var folderName = Guid.TryParse(prefix, out var productId) ? productId.ToString("N") : $"temp/{Guid.NewGuid():N}";

        foreach (var file in files)
        {
            try
            {
                // Strict validation using ImageHelper
                if (!ImageHelper.ValidateContentType(file.ContentType))
                {
                    Console.WriteLine($"Invalid content type: {file.ContentType}");
                    continue;
                }

                if (!ImageHelper.ValidateFileFormat(file.FileName))
                {
                    Console.WriteLine($"Invalid file format: {file.FileName}");
                    continue;
                }

                if (file.Length > _maxFileSize)
                {
                    Console.WriteLine($"File too large: {file.FileName} ({ImageHelper.GetFileSizeMB(file.Length)}MB)");
                    continue;
                }

                // Use GUID-based filename
                var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                var guidFileName = ImageHelper.GenerateGuidFileName(ext);
                var origPath = $"products/{folderName}/{guidFileName}".Replace("\\", "/");

                using var ms = new MemoryStream();
                await file.CopyToAsync(ms);
                var data = ms.ToArray();

                var origUrl = await _supabase.UploadFileAsync(bucket, origPath, data, file.ContentType ?? "application/octet-stream");

                string? thumbUrl = null;
                string? smallThumbUrl = null;

                try
                {
                    using var image = Image.Load(data);

                    var thumb = image.Clone(ctx => ctx.Resize(new ResizeOptions
                    {
                        Mode = ResizeMode.Max,
                        Size = new SixLabors.ImageSharp.Size(_thumbWidth, _thumbHeight)
                    }));

                    using var thumbMs = new MemoryStream();
                    thumb.Save(thumbMs, new JpegEncoder { Quality = 85 });
                    var thumbData = thumbMs.ToArray();
                    var thumbGuid = ImageHelper.GenerateGuidFileName(".jpg");
                    var thumbPath = $"products/{folderName}/thumbs/{thumbGuid}".Replace("\\", "/");
                    thumbUrl = await _supabase.UploadFileAsync(bucket, thumbPath, thumbData, "image/jpeg");

                    var small = image.Clone(ctx => ctx.Resize(new ResizeOptions
                    {
                        Mode = ResizeMode.Max,
                        Size = new SixLabors.ImageSharp.Size(_smallThumbWidth, _smallThumbHeight)
                    }));

                    using var smallMs = new MemoryStream();
                    small.Save(smallMs, new JpegEncoder { Quality = 80 });
                    var smallData = smallMs.ToArray();
                    var smallGuid = ImageHelper.GenerateGuidFileName(".jpg");
                    var smallPath = $"products/{folderName}/thumbs/small/{smallGuid}".Replace("\\", "/");
                    smallThumbUrl = await _supabase.UploadFileAsync(bucket, smallPath, smallData, "image/jpeg");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Thumbnail generation failed: " + ex.Message);
                }

                result.Add(new ImageUploadResult
                {
                    OriginalUrl = origUrl ?? string.Empty,
                    ThumbUrl = thumbUrl,
                    SmallThumbUrl = smallThumbUrl,
                    FileName = file.FileName
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine("UploadImagesAsync error: " + ex.Message);
            }
        }

        return result;
    }

    public async Task<bool> ReorderImagesAsync(Guid productId, List<Guid> orderedIds)
    {
        try
        {
            // Validate all images belong to this product
            var existingImages = await GetImagesByProductAsync(productId);
            var existingIds = existingImages.Select(i => i.Id).ToHashSet();
            
            if (!orderedIds.All(id => existingIds.Contains(id)))
            {
                Console.WriteLine("Some image IDs do not belong to this product");
                return false;
            }

            int order = 0; // Start from 0 as per requirements
            foreach (var imageId in orderedIds)
            {
                var data = new { display_order = order++ };
                await _supabase.PatchAsync<ProductImageDto>("product_images", imageId, data);
            }
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> DeleteImageAsync(Guid imageId)
    {
        try
        {
            var bucket = _config["SUPABASE_BUCKET"] ?? "wear-mate";
            
            // Get image details
            var query = _supabase.From("product_images").Eq("id", imageId);
            var images = await _supabase.GetAsync<ProductImageDto>(query.Build());
            var image = images.FirstOrDefault();
            
            if (image == null) return false;
            
            var productId = image.ProductId;
            var imageUrl = image.ImageUrl;
            
            // Check if this is the current thumbnail
            var productQuery = _supabase.From("products").Eq("id", productId);
            var product = await _supabase.GetSingleAsync<ProductDto>(productQuery.Build());
            bool isThumbnail = product != null && product.ThumbnailUrl == imageUrl;
            
            // Delete from Storage
            var storagePath = ImageHelper.ExtractStoragePath(imageUrl);
            if (!string.IsNullOrWhiteSpace(storagePath))
            {
                try
                {
                    await _supabase.DeleteFileAsync(bucket, storagePath);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to delete file from Storage: {ex.Message}");
                }
            }
            
            // Delete from DB
            await _supabase.DeleteAsync("product_images", imageId);
            
            // If this was the thumbnail, reassign to first remaining image or default
            if (isThumbnail && product != null)
            {
                var remainingImages = await GetImagesByProductAsync(productId);
                var newThumbnail = remainingImages.Any() 
                    ? remainingImages.First().ImageUrl 
                    : ImageHelper.GetProductImage(null); // Use default
                    
                var updateData = new { thumbnail_url = newThumbnail, updated_at = DateTime.UtcNow };
                await _supabase.PatchAsync<ProductDto>("products", productId, updateData);
            }
            
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"DeleteImageAsync error: {ex.Message}");
            return false;
        }
    }

    public async Task<List<ProductImageDto>> GetImagesByProductAsync(Guid productId)
    {
        var query = _supabase.From("product_images")
            .Eq("product_id", productId)
            .OrderBy("display_order", true);
        return await _supabase.GetAsync<ProductImageDto>(query.Build());
    }

    /// <summary>
    /// Add new image(s) to an existing product
    /// </summary>
    public async Task<ProductImageDto?> AddProductImageAsync(Guid productId, IFormFile file)
    {
        try
        {
            var bucket = _config["SUPABASE_BUCKET"] ?? "wear-mate";
            
            // Validate file
            if (!ImageHelper.ValidateContentType(file.ContentType))
                throw new Exception($"Invalid content type: {file.ContentType}");
                
            if (!ImageHelper.ValidateFileFormat(file.FileName))
                throw new Exception($"Invalid file format: {file.FileName}");
                
            if (file.Length > _maxFileSize)
                throw new Exception($"File too large: {ImageHelper.GetFileSizeMB(file.Length)}MB (max {ImageHelper.MAX_FILE_SIZE_MB}MB)");
            
            // Upload to Storage
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var guidFileName = ImageHelper.GenerateGuidFileName(ext);
            var folderName = productId.ToString("N");
            var filePath = $"products/{folderName}/{guidFileName}".Replace("\\", "/");
            
            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            var data = ms.ToArray();
            
            var imageUrl = await _supabase.UploadFileAsync(bucket, filePath, data, file.ContentType ?? "application/octet-stream");
            
            if (string.IsNullOrEmpty(imageUrl))
                throw new Exception("Failed to upload image to Storage");
            
            // Get current max display_order
            var existingImages = await GetImagesByProductAsync(productId);
            var maxOrder = existingImages.Any() ? existingImages.Max(i => i.DisplayOrder) : -1;
            
            // Insert into DB
            var imgData = new
            {
                product_id = productId,
                image_url = imageUrl,
                display_order = maxOrder + 1,
                created_at = DateTime.UtcNow
            };
            
            return await _supabase.PostAsync<ProductImageDto>("product_images", imgData);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"AddProductImageAsync error: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Update product thumbnail to a different image
    /// </summary>
    public async Task<bool> UpdateProductThumbnailAsync(Guid productId, Guid imageId)
    {
        try
        {
            // Get the image to verify it belongs to this product
            var query = _supabase.From("product_images")
                .Eq("id", imageId)
                .Eq("product_id", productId);
            var images = await _supabase.GetAsync<ProductImageDto>(query.Build());
            var image = images.FirstOrDefault();
            
            if (image == null)
                throw new Exception("Image not found or doesn't belong to this product");
            
            // Update product thumbnail
            var data = new
            {
                thumbnail_url = image.ImageUrl,
                updated_at = DateTime.UtcNow
            };
            
            var result = await _supabase.PatchAsync<ProductDto>("products", productId, data);
            return result != null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"UpdateProductThumbnailAsync error: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Replace an existing image with a new one
    /// </summary>
    public async Task<ProductImageDto?> ReplaceProductImageAsync(Guid imageId, IFormFile newFile)
    {
        try
        {
            var bucket = _config["SUPABASE_BUCKET"] ?? "wear-mate";
            
            // Get existing image
            var query = _supabase.From("product_images").Eq("id", imageId);
            var images = await _supabase.GetAsync<ProductImageDto>(query.Build());
            var existingImage = images.FirstOrDefault();
            
            if (existingImage == null)
                throw new Exception("Image not found");
            
            var oldImageUrl = existingImage.ImageUrl;
            var productId = existingImage.ProductId;
            
            // Validate new file
            if (!ImageHelper.ValidateContentType(newFile.ContentType))
                throw new Exception($"Invalid content type: {newFile.ContentType}");
                
            if (!ImageHelper.ValidateFileFormat(newFile.FileName))
                throw new Exception($"Invalid file format: {newFile.FileName}");
                
            if (newFile.Length > _maxFileSize)
                throw new Exception($"File too large: {ImageHelper.GetFileSizeMB(newFile.Length)}MB");
            
            // Upload new file
            var ext = Path.GetExtension(newFile.FileName).ToLowerInvariant();
            var guidFileName = ImageHelper.GenerateGuidFileName(ext);
            var folderName = productId.ToString("N");
            var newFilePath = $"products/{folderName}/{guidFileName}".Replace("\\", "/");
            
            using var ms = new MemoryStream();
            await newFile.CopyToAsync(ms);
            var data = ms.ToArray();
            
            var newImageUrl = await _supabase.UploadFileAsync(bucket, newFilePath, data, newFile.ContentType ?? "application/octet-stream");
            
            if (string.IsNullOrEmpty(newImageUrl))
                throw new Exception("Failed to upload new image to Storage");
            
            // Update DB record
            var updateData = new
            {
                image_url = newImageUrl,
                created_at = DateTime.UtcNow // Update timestamp for replaced image
            };
            
            var updatedImage = await _supabase.PatchAsync<ProductImageDto>("product_images", imageId, updateData);
            
            // If this was the thumbnail, update product's thumbnail_url
            var productQuery = _supabase.From("products").Eq("id", productId);
            var product = await _supabase.GetSingleAsync<ProductDto>(productQuery.Build());
            
            if (product != null && product.ThumbnailUrl == oldImageUrl)
            {
                var productUpdate = new
                {
                    thumbnail_url = newImageUrl,
                    updated_at = DateTime.UtcNow
                };
                await _supabase.PatchAsync<ProductDto>("products", productId, productUpdate);
            }
            
            // Delete old file from Storage
            var oldStoragePath = ImageHelper.ExtractStoragePath(oldImageUrl);
            if (!string.IsNullOrWhiteSpace(oldStoragePath))
            {
                try
                {
                    await _supabase.DeleteFileAsync(bucket, oldStoragePath);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to delete old image from Storage: {ex.Message}");
                }
            }
            
            return updatedImage;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ReplaceProductImageAsync error: {ex.Message}");
            throw;
        }
    }

    private static string Slugify(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return Guid.NewGuid().ToString();
        var normalized = text.Normalize(NormalizationForm.FormKD);
        var sb = new StringBuilder();
        foreach (var ch in normalized)
        {
            if (char.IsLetterOrDigit(ch) || ch == ' ' || ch == '-') sb.Append(ch);
        }
        var slug = sb.ToString().ToLowerInvariant().Trim();
        slug = System.Text.RegularExpressions.Regex.Replace(slug, "\\s+", "-");
        slug = System.Text.RegularExpressions.Regex.Replace(slug, "-+", "-");
        return slug;
    }

    public async Task<List<ProductVariantDto>> GetVariantsByProductAsync(Guid productId)
    {
        var query = _supabase.From("product_variants").Eq("product_id", productId).Eq("is_active", true);
        return await _supabase.GetAsync<ProductVariantDto>(query.Build());
    }

    public async Task<ProductVariantDto?> CreateVariantAsync(CreateProductVariantDto dto)
    {
        // Ensure SKU uniqueness (constraint: product_variants.sku is unique)
        var sku = await GenerateUniqueVariantSkuAsync(dto.Sku, dto.ProductId, dto.Color, dto.Size);

        var data = new
        {
            product_id = dto.ProductId,
            sku = sku,
            size = dto.Size,
            color = dto.Color,
            price_adjustment = dto.PriceAdjustment,
            is_active = dto.IsActive,
            created_at = DateTime.UtcNow
        };

        return await _supabase.PostAsync<ProductVariantDto>("product_variants", data);
    }

    public async Task<ProductVariantDto?> UpdateVariantAsync(Guid id, CreateProductVariantDto dto)
    {
        var sku = await GenerateUniqueVariantSkuAsync(dto.Sku, dto.ProductId, dto.Color, dto.Size, id);

        var data = new
        {
            sku = sku,
            size = dto.Size,
            color = dto.Color,
            price_adjustment = dto.PriceAdjustment,
            is_active = dto.IsActive
        };

        return await _supabase.PatchAsync<ProductVariantDto>("product_variants", id, data);
    }

    public async Task<bool> DeleteVariantAsync(Guid id)
    {
        var data = new
        {
            is_active = false
        };

        var result = await _supabase.PatchAsync<ProductVariantDto>("product_variants", id, data);
        return result != null;
    }

    private async Task<string> GenerateUniqueSlugAsync(string input, Guid? currentId = null)
    {
        var baseSlug = Slugify(input);
        var slug = baseSlug;
        int counter = 1;

        while (await SlugExists(slug, currentId))
        {
            slug = $"{baseSlug}-{counter++}";
        }

        return slug;
    }

    private async Task<bool> SlugExists(string slug, Guid? currentId)
    {
        var query = _supabase.From("products")
            .Eq("slug", slug);

        var existing = await _supabase.GetAsync<ProductDto>(query.Build());

        if (existing == null || existing.Count == 0)
            return false;

        if (currentId.HasValue && existing.Any(p => p.Id == currentId.Value))
            return false;

        return true;
    }

    private async Task<string> GenerateUniqueVariantSkuAsync(string input, Guid? currentId = null)
    {
        var baseSku = Slugify(input);
        if (string.IsNullOrWhiteSpace(baseSku))
            baseSku = "sku";

        var sku = baseSku;
        int counter = 1;

        while (await VariantSkuExistsAsync(sku, currentId))
        {
            sku = $"{baseSku}-{counter++}";
        }

        return sku.ToUpperInvariant();
    }

    private async Task<bool> VariantSkuExistsAsync(string sku, Guid? currentId)
    {
        var query = _supabase.From("product_variants").Eq("sku", sku);
        var existing = await _supabase.GetAsync<ProductVariantDto>(query.Build());

        if (existing == null || existing.Count == 0)
            return false;

        if (currentId.HasValue && existing.Any(v => v.Id == currentId.Value))
            return false;

        return true;
    }

    private async Task<string?> GetProductSlugAsync(Guid productId)
    {
        var query = _supabase.From("products").Select("slug").Eq("id", productId);
        var product = await _supabase.GetSingleAsync<ProductDto>(query.Build());
        return product?.Slug;
    }

    private async Task<string> GenerateUniqueVariantSkuAsync(string? requestedSku, Guid productId, string? color, string? size, Guid? currentVariantId = null)
    {
        // Build a base SKU that includes product slug to avoid cross-product collisions
        var productSlug = await GetProductSlugAsync(productId) ?? "product";
        var basePart = !string.IsNullOrWhiteSpace(requestedSku)
            ? requestedSku
            : $"{productSlug}-{color}-{size}";

        var baseSku = Slugify(basePart);
        if (string.IsNullOrWhiteSpace(baseSku))
            baseSku = "sku";

        var sku = baseSku.ToUpperInvariant();
        int counter = 1;

        while (await VariantSkuExistsAsync(sku, currentVariantId))
        {
            sku = $"{baseSku}-{counter++}".ToUpperInvariant();
        }

        return sku;
    }
}
