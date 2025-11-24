using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace WearMate.Shared.Helpers;

public class SupabaseAuthHelper
{
    private readonly HttpClient _httpClient;
    private readonly string _supabaseUrl;
    private readonly string _supabaseAnonKey;
    private readonly JsonSerializerOptions _jsonOptions;

    public SupabaseAuthHelper(string supabaseUrl, string supabaseAnonKey)
    {
        _supabaseUrl = supabaseUrl.TrimEnd('/');
        _supabaseAnonKey = supabaseAnonKey;

        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("apikey", _supabaseAnonKey);
        _httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<SupabaseAuthResponse?> SignUpAsync(string email, string password, Dictionary<string, object>? metadata = null)
    {
        var payload = new
        {
            email = email,
            password = password,
            data = metadata ?? new Dictionary<string, object>()
        };

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync($"{_supabaseUrl}/auth/v1/signup", content);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Signup failed ({(int)response.StatusCode}): {error}");
        }

        var responseJson = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<SupabaseAuthResponse>(responseJson, _jsonOptions);
    }

    public async Task<SupabaseAuthResponse?> SignInAsync(string email, string password)
    {
        var payload = new
        {
            email = email,
            password = password
        };

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync($"{_supabaseUrl}/auth/v1/token?grant_type=password", content);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Login failed ({(int)response.StatusCode} {response.ReasonPhrase}): {error}");
        }

        var responseJson = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<SupabaseAuthResponse>(responseJson, _jsonOptions);
    }

    public async Task<SupabaseAuthResponse?> RefreshTokenAsync(string refreshToken)
    {
        var payload = new
        {
            refresh_token = refreshToken
        };

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync($"{_supabaseUrl}/auth/v1/token?grant_type=refresh_token", content);

        if (!response.IsSuccessStatusCode)
            return null;

        var responseJson = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<SupabaseAuthResponse>(responseJson, _jsonOptions);
    }

    public async Task<bool> SignOutAsync(string accessToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"{_supabaseUrl}/auth/v1/logout");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _httpClient.SendAsync(request);
        return response.IsSuccessStatusCode;
    }

    public async Task<SupabaseUser?> GetUserAsync(string accessToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"{_supabaseUrl}/auth/v1/user");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
            return null;

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<SupabaseUser>(json, _jsonOptions);
    }
}

public class SupabaseAuthResponse
{
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public SupabaseUser? User { get; set; }
}

public class SupabaseUser
{
    public string? Id { get; set; }
    public string? Email { get; set; }
    public Dictionary<string, object>? UserMetadata { get; set; }
    public string? Role { get; set; }

    // ---- VERIFICATION ----
    public DateTime? EmailConfirmedAt { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? LastSignInAt { get; set; }
}
