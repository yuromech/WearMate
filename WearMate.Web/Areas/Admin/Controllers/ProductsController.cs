using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;
using WearMate.Shared.DTOs.Products;
using WearMate.Shared.DTOs.Common;
using WearMate.Web.ApiClients;
using WearMate.Web.Middleware;

namespace WearMate.Web.Areas.Admin.Controllers;

[Area("Admin")]
[AdminAuthorize]
public class ProductsController : Controller
{
    private readonly ProductApiClient _productApi;
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;

    public ProductsController(ProductApiClient productApi, IHttpClientFactory httpClientFactory)
    {
        _productApi = productApi;
        _httpClient = httpClientFactory.CreateClient("ProductAPI");
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<IActionResult> Index(int page = 1, int pageSize = 20, Guid? categoryId = null, Guid? brandId = null, string? search = null, string? status = "all")
    {
        bool? isActive = null;
        if (status?.ToLower() == "active") isActive = true;
        else if (status?.ToLower() == "inactive") isActive = false;

        var products = await _productApi.GetProductsAsync(
            page, 
            pageSize, 
            categoryId: categoryId, 
            brandId: brandId, 
            search: search, 
            isActive: isActive);
        ViewBag.Categories = await _productApi.GetCategoriesAsync() ?? new List<CategoryDto>();
        ViewBag.Brands = await _productApi.GetBrandsAsync() ?? new List<BrandDto>();
        ViewBag.SearchQuery = search;
        ViewBag.SelectedCategoryId = categoryId;
        ViewBag.SelectedBrandId = brandId;
        ViewBag.CurrentStatus = status?.ToLower() ?? "all";
        return View(products ?? new PaginatedResult<ProductDto>());
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        ViewBag.Categories = await _productApi.GetCategoriesAsync() ?? new List<CategoryDto>();
        ViewBag.Brands = await _productApi.GetBrandsAsync() ?? new List<BrandDto>();
        return View(new CreateProductDto());
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateProductDto model, List<IFormFile> images, IFormFile? thumbnailFile)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Categories = await _productApi.GetCategoriesAsync() ?? new();
            ViewBag.Brands = await _productApi.GetBrandsAsync() ?? new();
            return View(model);
        }

        try
        {
            var uploadedUrls = new List<string>();
            string? thumbnailUrl = null;

            if (thumbnailFile != null)
            {
                using var thumbMultipart = new MultipartFormDataContent();
                var streamContent = new StreamContent(thumbnailFile.OpenReadStream());
                streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(thumbnailFile.ContentType ?? "application/octet-stream");
                thumbMultipart.Add(streamContent, "files", thumbnailFile.FileName);
                thumbMultipart.Add(new StringContent(model.Name ?? ""), "prefix");

                var uploads = await _productApi.UploadImagesViaClientAsync(thumbMultipart);
                if (uploads != null)
                {
                    thumbnailUrl = uploads.FirstOrDefault(u => !string.IsNullOrEmpty(u.OriginalUrl))?.OriginalUrl;
                }
            }

            if (images != null && images.Count > 0)
            {
                using var multipart = new MultipartFormDataContent();
                foreach (var file in images)
                {
                    var streamContent = new StreamContent(file.OpenReadStream());
                    streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType ?? "application/octet-stream");
                    multipart.Add(streamContent, "files", file.FileName);
                }
                multipart.Add(new StringContent(model.Name ?? ""), "prefix");

                var uploads = await _productApi.UploadImagesViaClientAsync(multipart);
                if (uploads != null)
                {
                    uploadedUrls.AddRange(uploads.Where(u => !string.IsNullOrEmpty(u.OriginalUrl)).Select(u => u.OriginalUrl));
                }
            }

            if (string.IsNullOrWhiteSpace(model.Slug)) model.Slug = GenerateSlug(model.Name);

            var payload = new CreateProductWithImagesDto
            {
                CategoryId = model.CategoryId,
                BrandId = model.BrandId,
                Name = model.Name ?? string.Empty,
                Slug = model.Slug ?? string.Empty,
                Description = model.Description ?? string.Empty,
                ShortDescription = model.ShortDescription ?? string.Empty,
                BasePrice = model.BasePrice,
                SalePrice = model.SalePrice,
                Sku = model.Sku,
                MetaTitle = model.MetaTitle,
                MetaDescription = model.MetaDescription,
                IsFeatured = model.IsFeatured,
                IsActive = model.IsActive,
                ThumbnailUrl = thumbnailUrl ?? uploadedUrls.FirstOrDefault(),
                Images = uploadedUrls.Any() ? uploadedUrls : new List<string>(),
                Variants = model.Variants
            };

            var json = JsonSerializer.Serialize(payload, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("/api/products", content);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Product created successfully!";
                return RedirectToAction(nameof(Index));
            }

            var error = await response.Content.ReadAsStringAsync();
            TempData["Error"] = $"Failed to create product: {error}";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error: {ex.Message}";
        }

        ViewBag.Categories = await _productApi.GetCategoriesAsync() ?? new();
        ViewBag.Brands = await _productApi.GetBrandsAsync() ?? new();
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var response = await _httpClient.GetAsync($"/api/products/{id}");
        if (!response.IsSuccessStatusCode)
            return NotFound();

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var dataElement = doc.RootElement.GetProperty("data");
        var product = JsonSerializer.Deserialize<ProductDto>(dataElement.GetRawText(), _jsonOptions);

        if (product == null)
            return NotFound();

        var model = new CreateProductDto
        {
            CategoryId = product.CategoryId,
            BrandId = product.BrandId,
            Name = product.Name,
            Slug = product.Slug,
            Description = product.Description,
            ShortDescription = product.ShortDescription,
            BasePrice = product.BasePrice,
            SalePrice = product.SalePrice,
            Sku = product.Sku,
            MetaTitle = product.MetaTitle,
            MetaDescription = product.MetaDescription,
            ThumbnailUrl = product.ThumbnailUrl,
            IsFeatured = product.IsFeatured,
            IsActive = product.IsActive
        };

        ViewBag.ProductId = id;
        ViewBag.Product = product;
        ViewBag.Categories = await _productApi.GetCategoriesAsync() ?? new List<CategoryDto>();
        ViewBag.Brands = await _productApi.GetBrandsAsync() ?? new List<BrandDto>();
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(Guid id, CreateProductDto model, IFormFile? thumbnailFile)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.ProductId = id;
            ViewBag.Categories = await _productApi.GetCategoriesAsync() ?? new();
            ViewBag.Brands = await _productApi.GetBrandsAsync() ?? new();
            return View(model);
        }

        try
        {
            if (thumbnailFile != null)
            {
                using var thumbMultipart = new MultipartFormDataContent();
                var streamContent = new StreamContent(thumbnailFile.OpenReadStream());
                streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(thumbnailFile.ContentType ?? "application/octet-stream");
                thumbMultipart.Add(streamContent, "files", thumbnailFile.FileName);
                thumbMultipart.Add(new StringContent(model.Name ?? ""), "prefix");

                var uploads = await _productApi.UploadImagesViaClientAsync(thumbMultipart);
                if (uploads != null)
                    model.ThumbnailUrl = uploads.FirstOrDefault(u => !string.IsNullOrEmpty(u.OriginalUrl))?.OriginalUrl ?? model.ThumbnailUrl;
            }

            var json = JsonSerializer.Serialize(model, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"/api/products/{id}", content);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Product updated successfully!";
                return RedirectToAction(nameof(Index));
            }

            var error = await response.Content.ReadAsStringAsync();
            TempData["Error"] = $"Failed to update product: {error}";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error: {ex.Message}";
        }

        ViewBag.ProductId = id;
        ViewBag.Categories = await _productApi.GetCategoriesAsync() ?? new();
        ViewBag.Brands = await _productApi.GetBrandsAsync() ?? new();
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var result = await _productApi.DeleteProductAsync(id);
            if (result)
            {
                TempData["Success"] = "Product deleted successfully!";
            }
            else
            {
                TempData["Error"] = "Failed to delete product";
            }
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error: {ex.Message}";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Export(int page = 1, int pageSize = 1000, Guid? categoryId = null, Guid? brandId = null, string? search = null)
    {
        var products = await _productApi.GetProductsAsync(
            1, 
            pageSize, 
            categoryId: categoryId, 
            brandId: brandId, 
            search: search);
        var list = products?.Items ?? new List<ProductDto>();

        var sb = new StringBuilder();
        sb.AppendLine("Id,Name,Slug,Category,Brand,Price,SalePrice,Active,CreatedAt");
        foreach (var p in list)
        {
            sb.AppendLine($"{p.Id},\"{p.Name.Replace("\"", "")}\",{p.Slug},\"{p.Category?.Name}\",\"{p.Brand?.Name}\",{p.BasePrice},{p.SalePrice},{p.IsActive},{p.CreatedAt:O}");
        }

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        return File(bytes, "text/csv", "products_export.csv");
    }

    [HttpGet]
    public async Task<IActionResult> GetImages(Guid id)
    {
        try
        {
            var product = await _productApi.GetProductByIdAsync(id);
            var thumbnailUrl = product?.ThumbnailUrl ?? string.Empty;

            var response = await _httpClient.GetAsync($"/api/productimages/by-product/{id}");
            if (!response.IsSuccessStatusCode) return Ok(new List<object>());
            
            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var data = doc.RootElement.GetProperty("data");

            var images = JsonSerializer.Deserialize<List<ProductImageDto>>(data.GetRawText(), _jsonOptions) ?? new();
            var shaped = images.Select(img => new
            {
                img.Id,
                img.ProductId,
                img.ImageUrl,
                img.DisplayOrder,
                img.CreatedAt,
                IsThumbnail = !string.IsNullOrEmpty(thumbnailUrl) && string.Equals(img.ImageUrl, thumbnailUrl, StringComparison.OrdinalIgnoreCase)
            });

            return Content(JsonSerializer.Serialize(shaped, _jsonOptions), "application/json");
        }
        catch
        {
            return Ok(new List<object>());
        }
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteImage(Guid imageId)
    {
        var response = await _httpClient.DeleteAsync($"/api/productimages/{imageId}");
        var json = await response.Content.ReadAsStringAsync();
        return Content(json, "application/json", Encoding.UTF8);
    }

    [HttpPost]
    public async Task<IActionResult> ReplaceImage(Guid id, IFormFile file)
    {
        if (file == null) return BadRequest("No file provided");
        using var form = new MultipartFormDataContent();
        var content = new StreamContent(file.OpenReadStream());
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType ?? "application/octet-stream");
        form.Add(content, "file", file.FileName);
        var response = await _httpClient.PostAsync($"/api/productimages/{id}/replace", form);
        var json = await response.Content.ReadAsStringAsync();
        return Content(json, "application/json", Encoding.UTF8);
    }

    public class ReorderDto { public Guid ProductId { get; set; } public List<Guid> OrderedIds { get; set; } = new(); }

    [HttpPost]
    public async Task<IActionResult> ReorderImages([FromBody] ReorderDto dto)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/productimages/reorder", dto);
        var json = await response.Content.ReadAsStringAsync();
        return Content(json, "application/json", Encoding.UTF8);
    }

    [HttpPut]
    public async Task<IActionResult> SetThumbnail([FromBody] UpdateThumbnailRequest dto)
    {
        var response = await _httpClient.PutAsJsonAsync("/api/productimages/thumbnail", dto);
        var json = await response.Content.ReadAsStringAsync();
        return Content(json, "application/json", Encoding.UTF8);
    }

    // --- Variant proxy endpoints for admin JS (call ProductAPI) ---
    [HttpGet]
    public async Task<IActionResult> GetVariants(Guid id)
    {
        var response = await _httpClient.GetAsync($"/api/productvariants/by-product/{id}");
        if (!response.IsSuccessStatusCode) return StatusCode((int)response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        return Content(json, "application/json");
    }

    [HttpPost]
    public async Task<IActionResult> SaveVariant([FromBody] CreateProductVariantDto dto)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/productvariants", dto);
        var json = await response.Content.ReadAsStringAsync();
        return Content(json, "application/json", Encoding.UTF8);
    }

    [HttpPut]
    public async Task<IActionResult> UpdateVariant(Guid id, [FromBody] UpdateProductVariantDto dto)
    {
        var response = await _httpClient.PutAsJsonAsync($"/api/productvariants/{id}", dto);
        var json = await response.Content.ReadAsStringAsync();
        return Content(json, "application/json", Encoding.UTF8);
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteVariant(Guid id)
    {
        var response = await _httpClient.DeleteAsync($"/api/productvariants/{id}");
        var json = await response.Content.ReadAsStringAsync();
        return Content(json, "application/json", Encoding.UTF8);
    }

    [HttpPost]
    public async Task<IActionResult> UploadImages(Guid productId, List<IFormFile> files)
    {
        try
        {
            if (files == null || !files.Any()) return BadRequest("No files");

            foreach (var file in files)
            {
                var formData = new MultipartFormDataContent();
                formData.Add(new StringContent(productId.ToString()), "productId");
                
                var fileContent = new StreamContent(file.OpenReadStream());
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);
                formData.Add(fileContent, "file", file.FileName);
                
                var response = await _httpClient.PostAsync("/api/productimages/add", formData);
                if (!response.IsSuccessStatusCode)
                {
                    return StatusCode((int)response.StatusCode);
                }
            }
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    private string GenerateSlug(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return Guid.NewGuid().ToString();
        var normalized = text.ToLowerInvariant().Trim();
        var sb = new StringBuilder();
        foreach (var ch in normalized)
        {
            if (char.IsLetterOrDigit(ch) || ch == ' ' || ch == '-') sb.Append(ch);
        }
        var slug = sb.ToString();
        slug = System.Text.RegularExpressions.Regex.Replace(slug, "\\s+", "-");
        slug = System.Text.RegularExpressions.Regex.Replace(slug, "-+", "-");
        return slug.Trim('-');
    }
}
