using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;
using WearMate.Shared.DTOs.Products;
using WearMate.Web.ApiClients;
using WearMate.Web.Middleware;

namespace WearMate.Web.Areas.Admin.Controllers;

[Area("Admin")]
[AdminAuthorize]
public class BrandsController : Controller
{
    private readonly ProductApiClient _productApi;
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;

    public BrandsController(ProductApiClient productApi, IHttpClientFactory httpClientFactory)
    {
        _productApi = productApi;
        _httpClient = httpClientFactory.CreateClient("ProductAPI");
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<IActionResult> Index(bool includeInactive = false)
    {
        var response = await _httpClient.GetAsync($"/api/brands?includeInactive={includeInactive}");
        List<BrandDto> brands = new();

        if (response.IsSuccessStatusCode)
        {
            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var data = doc.RootElement.GetProperty("data");
            brands = JsonSerializer.Deserialize<List<BrandDto>>(data.GetRawText(), _jsonOptions) ?? new();
        }

        ViewBag.IncludeInactive = includeInactive;
        return View(brands);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new CreateBrandDto());
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateBrandDto model)
    {
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            var json = JsonSerializer.Serialize(model);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("/api/brands", content);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Brand created successfully!";
                return RedirectToAction(nameof(Index));
            }

            TempData["Error"] = "Failed to create brand";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error: {ex.Message}";
        }

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"/api/brands/{id}");
            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Brand deleted successfully!";
            }
            else
            {
                TempData["Error"] = "Failed to delete brand";
            }
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error: {ex.Message}";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Deactivate(Guid id)
    {
        try
        {
            var response = await _httpClient.PatchAsync($"/api/brands/{id}/deactivate", null);
            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Brand deactivated successfully!";
            }
            else
            {
                TempData["Error"] = "Failed to deactivate brand";
            }
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error: {ex.Message}";
        }

        return RedirectToAction(nameof(Index), new { includeInactive = true });
    }

    [HttpPost]
    public async Task<IActionResult> Reactivate(Guid id)
    {
        try
        {
            var response = await _httpClient.PatchAsync($"/api/brands/{id}/reactivate", null);
            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Brand reactivated successfully!";
            }
            else
            {
                TempData["Error"] = "Failed to reactivate brand";
            }
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error: {ex.Message}";
        }

        return RedirectToAction(nameof(Index), new { includeInactive = true });
    }
}
