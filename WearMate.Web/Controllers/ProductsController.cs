using Microsoft.AspNetCore.Mvc;
using WearMate.Web.ApiClients;
using WearMate.Shared.DTOs.Products;
using WearMate.Web.Models.ViewModels;

namespace WearMate.Web.Controllers;

public class ProductsController : Controller
{
    private readonly ProductApiClient _productApi;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(ProductApiClient productApi, ILogger<ProductsController> logger)
    {
        _productApi = productApi;
        _logger = logger;
    }


    // ===================================================================
    //  PRODUCT LISTING (FILTER + SORT + PAGINATION)
    // ===================================================================
    public async Task<IActionResult> Index(
        int page = 1,
        Guid? categoryId = null,
        Guid? brandId = null,
        int? minPrice = null,
        int? maxPrice = null,
        string? size = null,
        string? color = null,
        string? sort = null,
        string? search = null
    )
    {
        // 1 Build Filter DTO gi xung microservice
        var filter = new ProductFilterDto
        {
            Page = page,
            PageSize = 12,
            CategoryId = categoryId,
            BrandId = brandId,
            MinPrice = minPrice,
            MaxPrice = maxPrice,
            Size = size,
            Color = color,
            Sort = sort,
            Search = search
        };

        // 2 Build ViewModel ban u ( gi state UI)
        var vm = new ProductListViewModel
        {
            CurrentPage = page,

            SelectedCategoryId = categoryId,
            SelectedBrandId = brandId,

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
