using System.Net.Mime;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using WearMate.Shared.DTOs.Products;

namespace WearMate.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductVariantsController : ControllerBase
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;

    public ProductVariantsController(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient("ProductAPI");
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
    }

    [HttpGet("by-product/{productId:guid}")]
    public async Task<IActionResult> GetByProduct(Guid productId)
    {
        var response = await _httpClient.GetAsync($"/api/productvariants/by-product/{productId}");
        var content = await response.Content.ReadAsStringAsync();
        return new ContentResult
        {
            StatusCode = (int)response.StatusCode,
            Content = content,
            ContentType = MediaTypeNames.Application.Json
        };
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProductVariantDto dto)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/productvariants", dto, _jsonOptions);
        var content = await response.Content.ReadAsStringAsync();
        return new ContentResult
        {
            StatusCode = (int)response.StatusCode,
            Content = content,
            ContentType = MediaTypeNames.Application.Json
        };
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProductVariantDto dto)
    {
        dto.Id = id;
        var response = await _httpClient.PutAsJsonAsync($"/api/productvariants/{id}", dto, _jsonOptions);
        var content = await response.Content.ReadAsStringAsync();
        return new ContentResult
        {
            StatusCode = (int)response.StatusCode,
            Content = content,
            ContentType = MediaTypeNames.Application.Json
        };
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var response = await _httpClient.DeleteAsync($"/api/productvariants/{id}");
        var content = await response.Content.ReadAsStringAsync();
        return new ContentResult
        {
            StatusCode = (int)response.StatusCode,
            Content = content,
            ContentType = MediaTypeNames.Application.Json
        };
    }
}
