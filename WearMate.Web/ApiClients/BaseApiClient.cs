using System.Net.Http.Json;
using System.Text.Json;

namespace WearMate.Web.ApiClients;

public abstract class BaseApiClient
{
    protected readonly HttpClient _http;
    protected readonly JsonSerializerOptions _json;

    protected BaseApiClient(HttpClient http)
    {
        _http = http;
        _json = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    protected async Task<T?> GetAsync<T>(string url)
    {
        var response = await _http.GetAsync(url);

        if (!response.IsSuccessStatusCode)
            return default;

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(json, _json);
    }

    protected async Task<T?> PostAsync<T>(string url, object body)
    {
        var response = await _http.PostAsJsonAsync(url, body);

        if (!response.IsSuccessStatusCode)
            return default;

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(json, _json);
    }

    protected async Task<T?> PatchAsync<T>(string url, object body)
    {
        var request = new HttpRequestMessage(HttpMethod.Patch, url)
        {
            Content = JsonContent.Create(body)
        };

        var response = await _http.SendAsync(request);
        if (!response.IsSuccessStatusCode)
            return default;

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(json, _json);
    }

    protected async Task<bool> DeleteAsync(string url)
    {
        var response = await _http.DeleteAsync(url);
        return response.IsSuccessStatusCode;
    }

    // Helper to post and ignore result
    protected async Task<bool> PostBoolAsync(string url, object body)
    {
        var response = await _http.PostAsJsonAsync(url, body);
        return response.IsSuccessStatusCode;
    }
}
