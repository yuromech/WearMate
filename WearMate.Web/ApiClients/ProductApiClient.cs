using Microsoft.AspNetCore.Http;
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

    public async Task<List<CategoryDto>?> GetCategoriesAsync(bool includeInactive = false, string? search = null)
    {
        var url = BuildUrl("/api/categories", new()
        {
            { "includeInactive", includeInactive },
            { "search", search }
        });

        var api = await GetAsync<ApiResponse<List<CategoryDto>>>(url);
        return api?.Data;
    }

    public async Task<CategoryDto?> GetCategoryAsync(Guid id)
    {
        var api = await GetAsync<ApiResponse<CategoryDto>>($"/api/categories/{id}");
        return api?.Data;
    }

    public async Task<ApiResponse<CategoryDto>?> CreateCategoryAsync(CreateCategoryDto dto)
    {
        var response = await _http.PostAsJsonAsync("/api/categories", dto);
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<ApiResponse<CategoryDto>>(json, _json);
    }

    public async Task<ApiResponse<CategoryDto>?> UpdateCategoryAsync(Guid id, CreateCategoryDto dto)
    {
        var response = await _http.PutAsJsonAsync($"/api/categories/{id}", dto);
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<ApiResponse<CategoryDto>>(json, _json);
    }

    public async Task<ApiResponse<bool>?> DeleteCategoryAsync(Guid id)
    {
        var response = await _http.DeleteAsync($"/api/categories/{id}");
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<ApiResponse<bool>>(json, _json);
    }

    public async Task<ApiResponse<bool>?> DeactivateCategoryAsync(Guid id)
    {
        var request = new HttpRequestMessage(HttpMethod.Patch, $"/api/categories/{id}/deactivate");
        var response = await _http.SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<ApiResponse<bool>>(json, _json);
    }

    public async Task<ApiResponse<bool>?> ReactivateCategoryAsync(Guid id)
    {
        var request = new HttpRequestMessage(HttpMethod.Patch, $"/api/categories/{id}/reactivate");
        var response = await _http.SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<ApiResponse<bool>>(json, _json);
    }

    public async Task<ApiResponse<BrandDto>?> CreateBrandAsync(CreateBrandDto dto)
    {
        var response = await _http.PostAsJsonAsync("/api/brands", dto);
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<ApiResponse<BrandDto>>(json, _json);
    }

    public async Task<ApiResponse<BrandDto>?> UpdateBrandAsync(Guid id, CreateBrandDto dto)
    {
        var response = await _http.PutAsJsonAsync($"/api/brands/{id}", dto);
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<ApiResponse<BrandDto>>(json, _json);
    }

    public async Task<ApiResponse<bool>?> DeleteBrandAsync(Guid id)
    {
        var response = await _http.DeleteAsync($"/api/brands/{id}");
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<ApiResponse<bool>>(json, _json);
    }

    public async Task<ApiResponse<bool>?> DeactivateBrandAsync(Guid id)
    {
        var request = new HttpRequestMessage(HttpMethod.Patch, $"/api/brands/{id}/deactivate");
        var response = await _http.SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<ApiResponse<bool>>(json, _json);
    }

    public async Task<ApiResponse<bool>?> ReactivateBrandAsync(Guid id)
    {
        var request = new HttpRequestMessage(HttpMethod.Patch, $"/api/brands/{id}/reactivate");
        var response = await _http.SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<ApiResponse<bool>>(json, _json);
    }

    public async Task<ApiResponse<string>?> UploadBrandLogoAsync(IFormFile file, string? currentLogoUrl = null)
    {
        using var content = new MultipartFormDataContent();
        var streamContent = new StreamContent(file.OpenReadStream());
        streamContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType ?? "application/octet-stream");
        content.Add(streamContent, "file", file.FileName);
        if (!string.IsNullOrWhiteSpace(currentLogoUrl))
            content.Add(new StringContent(currentLogoUrl), "currentLogoUrl");

        var response = await _http.PostAsync("/api/brands/upload-logo", content);
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<ApiResponse<string>>(json, _json);
    }

    public async Task<ApiResponse<string>?> UploadCategoryImageAsync(IFormFile file, string? currentImageUrl = null)
    {
        using var content = new MultipartFormDataContent();

        var streamContent = new StreamContent(file.OpenReadStream());
        streamContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType ?? "application/octet-stream");
        content.Add(streamContent, "file", file.FileName);

        if (!string.IsNullOrWhiteSpace(currentImageUrl))
        {
            content.Add(new StringContent(currentImageUrl), "currentImageUrl");
        }

        var response = await _http.PostAsync("/api/categories/upload-image", content);
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<ApiResponse<string>>(json, _json);
    }


    public async Task<List<BrandDto>?> GetBrandsAsync(bool includeInactive = false, string? search = null)
    {
        var url = BuildUrl("/api/brands", new()
        {
            { "includeInactive", includeInactive },
            { "search", search }
        });
        var api = await GetAsync<ApiResponse<List<BrandDto>>>(url);
        return api?.Data;
    }

    public async Task<BrandDto?> GetBrandAsync(Guid id)
    {
        var api = await GetAsync<ApiResponse<BrandDto>>($"/api/brands/{id}");
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
                .Select(kv =>
                {
                    var stringValue = kv.Value switch
                    {
                        bool b => b.ToString().ToLowerInvariant(),
                        _ => kv.Value?.ToString() ?? string.Empty
                    };

                    return $"{kv.Key}={Uri.EscapeDataString(stringValue)}";
                })
        );

        return string.IsNullOrEmpty(query) ? baseUrl : $"{baseUrl}?{query}";
    }
}
