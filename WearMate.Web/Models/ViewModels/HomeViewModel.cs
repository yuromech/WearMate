using WearMate.Shared.DTOs.Products;

namespace WearMate.Web.Models.ViewModels;

public class HomeViewModel
{
    public List<ProductDto> FeaturedProducts { get; set; } = new();
    public List<ProductDto> NewArrivals { get; set; } = new();
    public List<ProductDto> BestSellers { get; set; } = new();
    public List<ProductDto> FlashSaleProducts { get; set; } = new();
    public List<CategoryDto> Categories { get; set; } = new();

    public List<string> HeroBanners { get; set; } = new();
    public List<string> MidBanners { get; set; } = new();
}

public class ProductListViewModel
{
    public List<ProductDto> Products { get; set; } = new();
    public List<CategoryDto> Categories { get; set; } = new();
    public List<BrandDto> Brands { get; set; } = new();

    // PAGING
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public int TotalCount { get; set; }
    public int PageSize { get; set; } = 12;

    // FILTERS
    public Guid? SelectedCategoryId { get; set; }
    public Guid? SelectedBrandId { get; set; }
    public int? MinPrice { get; set; }
    public int? MaxPrice { get; set; }
    public string? Size { get; set; }
    public string? Color { get; set; }

    // SORT
    public string? Sort { get; set; } // price_asc, price_desc, newest

    // SEARCH
    public string? SearchQuery { get; set; }
}



public class ProductDetailViewModel
{
    public ProductDto Product { get; set; } = new();

    public List<ProductVariantDto> Variants { get; set; } = new();
    public List<ProductImageDto> Images { get; set; } = new();
    public List<ProductDto> RelatedProducts { get; set; } = new();

    // Optional UI helpers
    public string? SelectedColor { get; set; }
    public string? SelectedSize { get; set; }
}


public class CheckoutViewModel
{
    public List<CartItemViewModel> CartItems { get; set; } = new();

    // Prices
    public string ShippingFullName { get; set; } = string.Empty;
    public string ShippingPhone { get; set; } = string.Empty;
    public string ShippingAddress { get; set; } = string.Empty;
    public string ShippingCity { get; set; } = string.Empty;
    public decimal Subtotal { get; set; }
    public decimal ShippingFee { get; set; }
    public decimal Total => Subtotal + ShippingFee;

    // Shipping info
    public string FullName { get; set; } = "";
    public string Phone { get; set; } = "";
    public string AddressLine { get; set; } = "";
    public string City { get; set; } = "";
    public string District { get; set; } = "";
    public string Ward { get; set; } = "";

    // Payment
    public string PaymentMethod { get; set; } = "cod";

    // Dropdown data
    public List<string> Cities { get; set; } = new();
    public List<string> Districts { get; set; } = new();
    public List<string> Wards { get; set; } = new();
}


public class CartItemViewModel
{
    public Guid ProductVariantId { get; set; }
    public string ProductName { get; set; } = "";
    public string? ProductImage { get; set; }
    public string? VariantInfo { get; set; }
    public int Quantity { get; set; }
    public decimal Price { get; set; }
    public decimal TotalPrice => Quantity * Price;

    // For detail page UI
    public string? Size { get; set; }
    public string? Color { get; set; }
}
