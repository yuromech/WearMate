//using Microsoft.AspNetCore.Http.Extensions;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using WearMate.Shared.Helpers;

namespace WearMate.OrderAPI.Data;

public class SupabaseClient
{
    private readonly HttpClient _httpClient;
    private readonly string _supabaseUrl;
    private readonly string _supabaseKey;
    private readonly JsonSerializerOptions _jsonOptions;

    public SupabaseClient(IConfiguration configuration)
    {
        _supabaseUrl = configuration["SUPABASE_URL"]
            ?? throw new Exception("SUPABASE_URL not found");
        _supabaseKey = configuration["SUPABASE_SERVICE_KEY"]
            ?? throw new Exception("SUPABASE_SERVICE_KEY not found");

        _httpClient = new HttpClient { BaseAddress = new Uri(_supabaseUrl) };
        _httpClient.DefaultRequestHeaders.Add("apikey", _supabaseKey);
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _supabaseKey);

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            PropertyNameCaseInsensitive = true
        };
    }

    public QueryBuilder From(string table) => new QueryBuilder(_supabaseUrl, table);

    public async Task<List<T>> GetAsync<T>(string url)
    {
        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<T>>(json, _jsonOptions) ?? new List<T>();
    }

    public async Task<T?> GetSingleAsync<T>(string url) where T : class
    {
        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<List<T>>(json, _jsonOptions);
        return result?.FirstOrDefault();
    }

    public async Task<T?> PostAsync<T>(string table, object data) where T : class
    {
        var url = $"{_supabaseUrl}/rest/v1/{table}";
        var json = JsonSerializer.Serialize(data, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };
        request.Headers.Add("Prefer", "return=representation");

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var responseJson = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<List<T>>(responseJson, _jsonOptions);
        return result?.FirstOrDefault();
    }

    public async Task<T?> PatchAsync<T>(string table, Guid id, object data) where T : class
    {
        var url = $"{_supabaseUrl}/rest/v1/{table}?id=eq.{id}";
        var json = JsonSerializer.Serialize(data, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var request = new HttpRequestMessage(HttpMethod.Patch, url) { Content = content };
        request.Headers.Add("Prefer", "return=representation");

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var responseJson = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<List<T>>(responseJson, _jsonOptions);
        return result?.FirstOrDefault();
    }

    public async Task<bool> DeleteAsync(string table, Guid id)
    {
        var url = $"{_supabaseUrl}/rest/v1/{table}?id=eq.{id}";
        var response = await _httpClient.DeleteAsync(url);
        response.EnsureSuccessStatusCode();
        return true;
    }

    public async Task<int> GetCountAsync(string url)
    {
        var request = new HttpRequestMessage(HttpMethod.Head, url);
        request.Headers.Add("Prefer", "count=exact");
        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        if (response.Content.Headers.TryGetValues("Content-Range", out var values))
        {
            var range = values.FirstOrDefault();
            if (!string.IsNullOrEmpty(range) && range.Contains('/'))
            {
                var countStr = range.Split('/')[1];
                if (int.TryParse(countStr, out var count))
                    return count;
            }
        }
        return 0;
    }
}