using Microsoft.AspNetCore.Mvc;
using WearMate.Web.ApiClients;
using WearMate.Shared.DTOs.Products;
using WearMate.Web.Models.ViewModels;

namespace WearMate.Web.Controllers;

public class ProductsController : Controller
{
    private readonly ProductApiClient _productApi;
    private readonly InventoryApiClient _inventoryApi;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(ProductApiClient productApi, InventoryApiClient inventoryApi, ILogger<ProductsController> logger)
    {
        _productApi = productApi;
        _inventoryApi = inventoryApi;
        _logger = logger;
    }


    // ===================================================================
    //  PRODUCT LISTING (FILTER + SORT + PAGINATION)
    // ===================================================================
    public async Task<IActionResult> Index(
        int page = 1,
        string? category = null,
        string? brand = null,
        int? minPrice = null,
        int? maxPrice = null,
        string? size = null,
        string? color = null,
        string? sort = null,
        string? search = null,
        // Legacy GUID parameters for backward compatibility
        Guid? categoryId = null,
        Guid? brandId = null
    )
    {
        // SEO: Redirect legacy GUID-based URLs to slug-based URLs
        if (categoryId.HasValue && string.IsNullOrWhiteSpace(category))
        {
            var categories = await _productApi.GetCategoriesAsync();
            var cat = categories?.FirstOrDefault(c => c.Id == categoryId.Value);
            if (cat != null)
                return RedirectToAction("Index", new { category = cat.Slug, brand, search, sort, page });
        }

        if (brandId.HasValue && string.IsNullOrWhiteSpace(brand))
        {
            var brands = await _productApi.GetBrandsAsync();
            var b = brands?.FirstOrDefault(b => b.Id == brandId.Value);
            if (b != null)
                return RedirectToAction("Index", new { category, brand = b.Slug, search, sort, page });
        }

        // Build Filter DTO
        var filter = new ProductFilterDto
        {
            Page = page,
            PageSize = 12,
            Category = category,
            Brand = brand,
            MinPrice = minPrice,
            MaxPrice = maxPrice,
            Size = size,
            Color = color,
            Sort = sort,
            Search = search
        };

        // Build ViewModel
        var vm = new ProductListViewModel
        {
            CurrentPage = page,

            SelectedCategory = category,
            SelectedBrand = brand,

            MinPrice = minPrice,
            MaxPrice = maxPrice,
            Size = size,
            Color = color,
            Sort = sort,
            SearchQuery = search
        };

        try
        {
            // 3 CALL API FILTER (nu backend c)
            var filtered = await _productApi.FilterProductsAsync(filter);

            if (filtered != null)
            {
                vm.Products = filtered.Items;
                vm.TotalPages = filtered.TotalPages;
                vm.TotalCount = filtered.TotalCount;
                vm.PageSize = filtered.PageSize;
            }
            else
            {
                // fallback khi backend cha support filter nng cao
                var fallback = await _productApi.GetProductsAsync(
                    page: page, pageSize: 12,
                    categoryId: categoryId,
                    brandId: brandId,
                    search: search
                );

                if (fallback != null)
                {
                    vm.Products = fallback.Items;
                    vm.TotalPages = fallback.TotalPages;
                    vm.TotalCount = fallback.TotalCount;
                    vm.PageSize = fallback.PageSize;
                }
            }

            // 4 Ti categories + brands phc v sidebar filter
            vm.Categories = await _productApi.GetCategoriesAsync() ?? new();
            vm.Brands = await _productApi.GetBrandsAsync() ?? new();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading product listing");
            TempData["Error"] = "Không thể tải danh sách sản phẩm.";
        }

        return View(vm);
    }



    // ===================================================================
    //  PRODUCT DETAIL
    // ===================================================================
    public async Task<IActionResult> Detail(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return RedirectToAction("Index");

        try
        {
            // 1 GET PRODUCT
            var product = await _productApi.GetProductBySlugAsync(slug);
            if (product == null)
                return NotFound();

            // 2 To ViewModel
            var vm = new ProductDetailViewModel
            {
                Product = product,
                Variants = new(),
                Images = new(),
                RelatedProducts = new()
            };

            // 3 GET VARIANTS
            vm.Variants = await _productApi.GetVariantsAsync(product.Id) ?? new();

            // 4 GET IMAGES
            vm.Images = await _productApi.GetImagesAsync(product.Id) ?? new();

            // 4b: availability per variant
            var variantStocks = new List<ProductVariantWithStock>();
            foreach (var v in vm.Variants.Where(v => v.IsActive))
            {
                var inventories = await _inventoryApi.GetInventoryByProductAsync(v.Id) ?? new();
                var available = inventories.Any()
                    ? inventories.Sum(i => i.AvailableQuantity)
                    : v.StockQuantity;

                variantStocks.Add(new ProductVariantWithStock
                {
                    Id = v.Id,
                    Sku = v.Sku,
                    Size = v.Size,
                    Color = v.Color,
                    PriceAdjustment = v.PriceAdjustment,
                    IsActive = v.IsActive,
                    Available = available
                });
            }
            vm.VariantStocks = variantStocks;

            // 5 RELATED PRODUCTS theo category
            if (product.CategoryId.HasValue)
            {
                vm.RelatedProducts =
                    await _productApi.GetRelatedProductsAsync(product.Id)
                    ?? new();
            }

            return View(vm);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading product detail");
            return NotFound();
        }
    }
}
