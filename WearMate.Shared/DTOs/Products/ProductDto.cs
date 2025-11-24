namespace WearMate.Shared.DTOs.Products;

public class CategoryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public Guid? ParentId { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class BrandDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ProductDto
{
    public Guid Id { get; set; }
    public Guid? CategoryId { get; set; }
    public Guid? BrandId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ShortDescription { get; set; }
    public decimal BasePrice { get; set; }
    public decimal? SalePrice { get; set; }
    public string? Sku { get; set; }
    public string? ThumbnailUrl { get; set; }
    public bool IsFeatured { get; set; }
    public bool IsActive { get; set; }
    public int ViewCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public CategoryDto? Category { get; set; }
    public BrandDto? Brand { get; set; }
    public List<ProductImageDto> Images { get; set; } = new();
    public List<ProductVariantDto> Variants { get; set; } = new();
}

public class ProductImageDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ProductVariantDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string? Size { get; set; }
    public string? Color { get; set; }
    public decimal PriceAdjustment { get; set; }
    public bool IsActive { get; set; }
    public int StockQuantity { get; set; } // From inventory
    public DateTime CreatedAt { get; set; }

    // Optional URL/slug for variant-specific urls
    public string? Slug { get; set; }
    public string? Url { get; set; }
}

public class ProductReviewDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public Guid UserId { get; set; }
    public int Rating { get; set; }
    public string? Title { get; set; }
    public string? Comment { get; set; }
    public bool IsVerifiedPurchase { get; set; }
    public bool IsApproved { get; set; }
    public DateTime CreatedAt { get; set; }
    public string UserName { get; set; } = string.Empty;
}

// Create/Update DTOs
public class CreateProductDto
{
    public Guid? CategoryId { get; set; }
    public Guid? BrandId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ShortDescription { get; set; }
    public decimal BasePrice { get; set; }
    public decimal? SalePrice { get; set; }
    public string? Sku { get; set; }
    public bool IsFeatured { get; set; }
    public bool IsActive { get; set; } = true;
    public List<CreateProductVariantDto>? Variants { get; set; }
}

public class CreateProductWithImagesDto : CreateProductDto
{
    public List<string>? Images { get; set; }
    public string? ThumbnailUrl { get; set; }
}

public class CreateCategoryDto
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? ParentId { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public class CreateBrandDto
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}

public class ProductFilterDto
{
    // Paging
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 12;

    // Search
    public string? Search { get; set; }

    // Category / Brand
    public Guid? CategoryId { get; set; }
    public Guid? BrandId { get; set; }

    // Price filter
    public int? MinPrice { get; set; }
    public int? MaxPrice { get; set; }

    // Variants filter
    public string? Size { get; set; }
    public string? Color { get; set; }

    // Sorting
    // available values:
    // - "price_asc"
    // - "price_desc"
    // - "newest"
    public string? Sort { get; set; }
}

public class CreateProductVariantDto
{
    public Guid ProductId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string? Size { get; set; }
    public string? Color { get; set; }
    public decimal PriceAdjustment { get; set; }
    public bool IsActive { get; set; } = true;
}

public class UpdateProductVariantDto : CreateProductVariantDto
{
    public Guid Id { get; set; }
}

public class ImageUploadResult
{
    public string OriginalUrl { get; set; } = string.Empty;
    public string? ThumbUrl { get; set; }
    public string? SmallThumbUrl { get; set; }
    public string? FileName { get; set; }
}

// Image Management DTOs
public class AddProductImageRequest
{
    public Guid ProductId { get; set; }
}

public class UpdateThumbnailRequest
{
    public Guid ProductId { get; set; }
    public Guid ImageId { get; set; }
}

public class UpdateProductThumbnailDto
{
    public Guid ProductId { get; set; }
   public string ThumbnailUrl { get; set; } = string.Empty;
}