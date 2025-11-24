using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using WearMate.Shared.Helpers;

namespace WearMate.ProductAPI.Data;

/// <summary>
/// Custom Supabase Client using HttpClient (No SDK)
/// </summary>
public class SupabaseClient
{
    private readonly HttpClient _httpClient;
    private readonly string _supabaseUrl;
    private readonly string _supabaseKey;
    private readonly JsonSerializerOptions _jsonOptions;

    public SupabaseClient(IConfiguration configuration)
    {
        _supabaseUrl = configuration["SUPABASE_URL"]
            ?? throw new Exception("SUPABASE_URL not found in configuration");
        _supabaseKey = configuration["SUPABASE_SERVICE_KEY"]
            ?? throw new Exception("SUPABASE_SERVICE_KEY not found in configuration");

        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(_supabaseUrl)
        };

        _httpClient.DefaultRequestHeaders.Add("apikey", _supabaseKey);
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _supabaseKey);

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            PropertyNameCaseInsensitive = true
        };
    }

    /// <summary>
    /// Create QueryBuilder for table
    /// </summary>
    public QueryBuilder From(string table)
    {
        return new QueryBuilder(_supabaseUrl, table);
    }

    /// <summary>
    /// Execute GET request
    /// </summary>
    public async Task<List<T>> GetAsync<T>(string url)
    {
        try
        {
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<List<T>>(json, _jsonOptions);

            return result ?? new List<T>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SupabaseClient GetAsync Error: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Execute GET request for single item
    /// </summary>
    public async Task<T?> GetSingleAsync<T>(string url) where T : class
    {
        try
        {
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<List<T>>(json, _jsonOptions);

            return result?.FirstOrDefault();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SupabaseClient GetSingleAsync Error: {ex.Message}");
            return default;
        }
    }

    /// <summary>
    /// Execute POST request (Insert)
    /// </summary>
    public async Task<T?> PostAsync<T>(string table, object data) where T : class
    {
        try
        {
            var url = $"{_supabaseUrl}/rest/v1/{table}";
            var json = JsonSerializer.Serialize(data, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Add Prefer header to return inserted data
            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = content
            };
            request.Headers.Add("Prefer", "return=representation");

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"SupabaseClient PostAsync Error: {response.StatusCode} - {errorBody}");
                response.EnsureSuccessStatusCode();
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<List<T>>(responseJson, _jsonOptions);

            return result?.FirstOrDefault();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SupabaseClient PostAsync Error: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Execute PATCH request (Update)
    /// </summary>
    public async Task<T?> PatchAsync<T>(string table, Guid id, object data) where T : class
    {
        try
        {
            var url = $"{_supabaseUrl}/rest/v1/{table}?id=eq.{id}";
            var json = JsonSerializer.Serialize(data, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Patch, url)
            {
                Content = content
            };
            request.Headers.Add("Prefer", "return=representation");

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<List<T>>(responseJson, _jsonOptions);

            return result?.FirstOrDefault();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SupabaseClient PatchAsync Error: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Execute DELETE request
    /// </summary>
    public async Task<bool> DeleteAsync(string table, Guid id)
    {
        try
        {
            var url = $"{_supabaseUrl}/rest/v1/{table}?id=eq.{id}";
            var response = await _httpClient.DeleteAsync(url);
            response.EnsureSuccessStatusCode();

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SupabaseClient DeleteAsync Error: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Get total count
    /// </summary>
    public async Task<int> GetCountAsync(string url)
    {
        try
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
        catch (Exception ex)
        {
            Console.WriteLine($"SupabaseClient GetCountAsync Error: {ex.Message}");
            return 0;
        }
    }

    /// <summary>
    /// Upload file to Supabase Storage
    /// </summary>
    public async Task<string?> UploadFileAsync(string bucket, string path, byte[] fileData, string contentType)
    {
        try
        {
            var url = $"{_supabaseUrl}/storage/v1/object/{bucket}/{path}";
            var content = new ByteArrayContent(fileData);
            content.Headers.ContentType = new MediaTypeHeaderValue(contentType);

            var response = await _httpClient.PostAsync(url, content);
            response.EnsureSuccessStatusCode();

            // Return public URL
            return $"{_supabaseUrl}/storage/v1/object/public/{bucket}/{path}";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SupabaseClient UploadFileAsync Error: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Delete file from Supabase Storage
    /// </summary>
    public async Task<bool> DeleteFileAsync(string bucket, string path)
    {
        try
        {
            var url = $"{_supabaseUrl}/storage/v1/object/{bucket}/{path}";
            var response = await _httpClient.DeleteAsync(url);
            response.EnsureSuccessStatusCode();

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SupabaseClient DeleteFileAsync Error: {ex.Message}");
            return false;
        }
    }
}
