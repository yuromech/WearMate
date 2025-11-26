using WearMate.Shared.DTOs.Common;
using WearMate.Shared.DTOs.Products;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace WearMate.Web.ApiClients;

public class ProductApiClient : BaseApiClient
{
    public ProductApiClient(HttpClient http) : base(http) { }


    // =============================================================
    //  BASIC PRODUCT QUERIES
    public async Task<PaginatedResult<ProductDto>?> GetProductsAsync(
        int page = 1,
        int pageSize = 20,
        Guid? categoryId = null,
        string? search = null,
        bool? isActive = true)
    {
        var url = BuildUrl("/api/products", new()
        {
            { "page", page },
            { "pageSize", pageSize },
            { "categoryId", categoryId },
            { "search", search },
            { "isActive", isActive }
        });

        var api = await GetAsync<ApiResponse<PaginatedResult<ProductDto>>>(url);
        return api?.Data;
    }

    public async Task<ProductDto?> GetProductBySlugAsync(string slug)
    {
        var api = await GetAsync<ApiResponse<ProductDto>>($"/api/products/slug/{slug}");
        return api?.Data;
    }


    public async Task<ProductDto?> GetProductByIdAsync(Guid id)
    {
        var api = await GetAsync<ApiResponse<ProductDto>>($"/api/products/{id}");
        return api?.Data;
    }



    // =============================================================
    //  FEATURE / NEW ARRIVALS
    // =============================================================

    public async Task<List<ProductDto>?> GetFeaturedProductsAsync(int limit = 8)
    {
        var api = await GetAsync<ApiResponse<List<ProductDto>>>(
            $"/api/products/featured?limit={limit}"
        );
        return api?.Data;
    }


    public async Task<List<ProductDto>?> GetNewArrivalsAsync(int limit = 8)
    {
        var api = await GetAsync<ApiResponse<List<ProductDto>>>(
            $"/api/products/new?limit={limit}"
        );
        return api?.Data;
    }



    // =============================================================
    //  CATEGORY & BRAND
    // =============================================================

    public async Task<List<CategoryDto>?> GetCategoriesAsync()
    {
        var api = await GetAsync<ApiResponse<List<CategoryDto>>>("/api/categories");
        return api?.Data;
    }


    public async Task<List<BrandDto>?> GetBrandsAsync()
    {
        var api = await GetAsync<ApiResponse<List<BrandDto>>>("/api/brands");
        return api?.Data;
    }



    // =============================================================
    //  ADVANCED FILTER
    // (backend s dng ProductFilterDto  truy vn LINQ dynamic)
    // =============================================================

    public async Task<PaginatedResult<ProductDto>?> FilterProductsAsync(ProductFilterDto filter)
    {
        var url = BuildUrl("/api/products", new()
        {
            { "page", filter.Page },
            { "pageSize", filter.PageSize },
            { "category", filter.Category },
            { "brand", filter.Brand },
            { "search", filter.Search },
            { "minPrice", filter.MinPrice },
            { "maxPrice", filter.MaxPrice },
            { "size", filter.Size },
            { "color", filter.Color },
            { "sortBy", filter.Sort }
        });

        var api = await GetAsync<ApiResponse<PaginatedResult<ProductDto>>>(url);
        return api?.Data;
    }



    // =============================================================
    //  RELATED PRODUCTS
    // =============================================================

    public async Task<List<ProductDto>?> GetRelatedProductsAsync(Guid productId)
    {
        var api = await GetAsync<ApiResponse<List<ProductDto>>>(
            $"/api/products/{productId}/related"
        );

        return api?.Data;
    }



    // =============================================================
    //  PRODUCT VARIANTS
    // =============================================================

    public async Task<List<ProductVariantDto>?> GetVariantsAsync(Guid productId)
    {
        var api = await GetAsync<ApiResponse<List<ProductVariantDto>>>(
            $"/api/productvariants/by-product/{productId}"
        );
        return api?.Data;
    }



    // =============================================================
    //  PRODUCT IMAGES
    // =============================================================

    public async Task<List<ProductImageDto>?> GetImagesAsync(Guid productId)
    {
        var api = await GetAsync<ApiResponse<List<ProductImageDto>>>(
            $"/api/productimages/by-product/{productId}"
        );
        return api?.Data;
    }



    // =============================================================
    //  SEARCH SUGGEST
    // =============================================================

    public async Task<List<ProductDto>?> SearchSuggestAsync(string keyword)
    {
        var api = await GetAsync<ApiResponse<List<ProductDto>>>(
            $"/api/products/suggest?keyword={keyword}"
        );
        return api?.Data;
    }



    // =============================================================
    //  UPLOAD IMAGES
    // =============================================================

    public async Task<List<string>?> UploadImagesAsync(MultipartFormDataContent content)
    {
        var response = await _http.PostAsync("/api/products/upload-images", content);
        if (!response.IsSuccessStatusCode) return null;

        var json = await response.Content.ReadAsStringAsync();
        var api = JsonSerializer.Deserialize<ApiResponse<List<string>>>(json, _json);
        return api?.Data;
    }

    public async Task<List<ImageUploadResult>?> UploadImagesViaClientAsync(MultipartFormDataContent content)
    {
        var response = await _http.PostAsync("/api/products/upload-images", content);
        if (!response.IsSuccessStatusCode) return null;

        var json = await response.Content.ReadAsStringAsync();
        var api = JsonSerializer.Deserialize<ApiResponse<List<ImageUploadResult>>>(json, _json);
        return api?.Data;
    }



    // =============================================================
    //  CREATE / DELETE PRODUCT
    // =============================================================

    public async Task<ProductDto?> CreateProductAsync(CreateProductWithImagesDto dto)
    {
        var api = await PostAsync<ApiResponse<ProductDto>>("/api/products", dto);
        return api?.Data;
    }

    public async Task<bool> DeleteProductAsync(Guid id)
    {
        return await DeleteAsync($"/api/products/{id}");
    }



    // =============================================================
    //  HELPER: BUILD URL CLEAN
    // =============================================================

    private string BuildUrl(string baseUrl, Dictionary<string, object?> queryParams)
    {
        var query = string.Join("&",
            queryParams
                .Where(kv => kv.Value != null && kv.Value.ToString() != "")
                .Select(kv => $"{kv.Key}={kv.Value}")
        );

        return string.IsNullOrEmpty(query) ? baseUrl : $"{baseUrl}?{query}";
    }
}
