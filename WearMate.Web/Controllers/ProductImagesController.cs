using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Mime;
using Microsoft.AspNetCore.Http;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using WearMate.Shared.DTOs.Products;

namespace WearMate.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductImagesController : ControllerBase
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;

    public ProductImagesController(IHttpClientFactory httpClientFactory)
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
        var response = await _httpClient.GetAsync($"/api/productimages/by-product/{productId}");
        var content = await response.Content.ReadAsStringAsync();
        return ProxyResponse(response, content);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var response = await _httpClient.DeleteAsync($"/api/productimages/{id}");
        var content = await response.Content.ReadAsStringAsync();
        return ProxyResponse(response, content);
    }

    [HttpPut("thumbnail")]
    public async Task<IActionResult> UpdateThumbnail([FromBody] UpdateThumbnailRequest request)
    {
        var response = await _httpClient.PutAsJsonAsync("/api/productimages/thumbnail", request, _jsonOptions);
        var content = await response.Content.ReadAsStringAsync();
        return ProxyResponse(response, content);
    }

    [HttpPost("{id:guid}/replace")]
    public async Task<IActionResult> Replace(Guid id, [FromForm] IFormFile file)
    {
        if (file == null) return BadRequest("File is required");

        using var formData = new MultipartFormDataContent();
        var streamContent = new StreamContent(file.OpenReadStream());
        streamContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType ?? "application/octet-stream");
        formData.Add(streamContent, "file", file.FileName);

        var response = await _httpClient.PostAsync($"/api/productimages/{id}/replace", formData);
        var content = await response.Content.ReadAsStringAsync();
        return ProxyResponse(response, content);
    }

    [HttpPost("reorder")]
    public async Task<IActionResult> Reorder([FromBody] ReorderRequest request)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/productimages/reorder", request, _jsonOptions);
        var content = await response.Content.ReadAsStringAsync();
        return ProxyResponse(response, content);
    }

    [HttpPost("add")]
    public async Task<IActionResult> Add([FromForm] Guid productId, [FromForm] IFormFile file)
    {
        if (file == null) return BadRequest("File is required");

        using var formData = new MultipartFormDataContent();
        formData.Add(new StringContent(productId.ToString()), "productId");
        var streamContent = new StreamContent(file.OpenReadStream());
        streamContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType ?? "application/octet-stream");
        formData.Add(streamContent, "file", file.FileName);

        var response = await _httpClient.PostAsync("/api/productimages/add", formData);
        var content = await response.Content.ReadAsStringAsync();
        return ProxyResponse(response, content);
    }

    private ContentResult ProxyResponse(HttpResponseMessage response, string content)
    {
        return new ContentResult
        {
            StatusCode = (int)response.StatusCode,
            Content = content,
            ContentType = MediaTypeNames.Application.Json
        };
    }

    public class ReorderRequest
    {
        public Guid ProductId { get; set; }
        public List<Guid> OrderedIds { get; set; } = new();
    }
}
