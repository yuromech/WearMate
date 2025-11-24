using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;
using WearMate.Shared.DTOs.Products;
using WearMate.Web.ApiClients;
using WearMate.Web.Middleware;

namespace WearMate.Web.Areas.Admin.Controllers;

[Area("Admin")]
[AdminAuthorize]
public class CategoriesController : Controller
{
    private readonly ProductApiClient _productApi;
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;

    public CategoriesController(ProductApiClient productApi, IHttpClientFactory httpClientFactory)
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
        var url = $"/api/categories?includeInactive={includeInactive}";
        var response = await _httpClient.GetAsync(url);
        
        if (!response.IsSuccessStatusCode)
        {
            return View(new List<CategoryDto>());
        }

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var dataElement = doc.RootElement.GetProperty("data");
        var categories = JsonSerializer.Deserialize<List<CategoryDto>>(dataElement.GetRawText(), _jsonOptions) ?? new();
        
        ViewBag.IncludeInactive = includeInactive;
        return View(categories);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new CreateCategoryDto());
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateCategoryDto model)
    {
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            var json = JsonSerializer.Serialize(model, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("/api/categories", content);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Category created successfully!";
                return RedirectToAction(nameof(Index));
            }

            TempData["Error"] = "Failed to create category";
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
            var response = await _httpClient.DeleteAsync($"/api/categories/{id}");
            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Category deleted permanently!";
            }
            else
            {
                TempData["Error"] = "Failed to delete category";
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
            var response = await _httpClient.PatchAsync($"/api/categories/{id}/deactivate", null);
            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Category deactivated successfully!";
            }
            else
            {
                TempData["Error"] = "Failed to deactivate category";
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
            var response = await _httpClient.PatchAsync($"/api/categories/{id}/reactivate", null);
            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Category reactivated successfully!";
            }
            else
            {
                TempData["Error"] = "Failed to reactivate category";
            }
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error: {ex.Message}";
        }

        return RedirectToAction(nameof(Index), new { includeInactive = true });
    }
}
