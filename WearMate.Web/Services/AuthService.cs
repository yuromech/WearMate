using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using WearMate.Shared.DTOs.Auth;
using WearMate.Shared.Helpers;

namespace WearMate.Web.Services
{
    public class AuthService
    {
        private readonly SupabaseAuthHelper _authHelper;
        private readonly HttpClient _httpClient;
        private readonly string _supabaseUrl;
        private readonly string _supabaseServiceKey;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly ILogger<AuthService> _logger;

        public AuthService(IConfiguration configuration, ILogger<AuthService> logger)
        {
            _supabaseUrl = configuration["SUPABASE_URL"]
                ?? throw new Exception("SUPABASE_URL not found");

            var authBaseUrl = configuration["SUPABASE_AUTH_URL"]
                ?? configuration["SUPAAUTH_URL"]
                ?? _supabaseUrl;

            var anonKey = configuration["SUPABASE_ANON_KEY"]
                ?? throw new Exception("SUPABASE_ANON_KEY not found");

            _supabaseServiceKey = configuration["SUPABASE_SERVICE_KEY"]
                ?? throw new Exception("SUPABASE_SERVICE_KEY not found");

            // Use anon key for GoTrue password grant; service key is reserved for database calls
            _authHelper = new SupabaseAuthHelper(authBaseUrl, anonKey);

            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("apikey", _supabaseServiceKey);
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_supabaseServiceKey}");

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                PropertyNameCaseInsensitive = true
            };

            _logger = logger;
        }

        // =====================================================================
        // REGISTER – Supabase tự gửi email confirm
        // =====================================================================
        public async Task<AuthResponseDto?> RegisterAsync(RegisterDto dto)
        {
            var metadata = new Dictionary<string, object>
            {
                { "full_name", dto.FullName },
                { "phone", dto.Phone ?? "" }
            };

            // Signup bằng anon key (authhelper)
            var signup = await _authHelper.SignUpAsync(dto.Email, dto.Password, metadata);

            // Khi email confirm ON, signup.User = null → KHÔNG LỖI
            _logger.LogInformation("Signup OK: email verification sent for {Email}", dto.Email);

            return new AuthResponseDto
            {
                AccessToken = string.Empty,
                RefreshToken = string.Empty,
                User = new UserSessionDto
                {
                    Id = Guid.Empty, // không biết ID cho đến khi user verify
                    Email = dto.Email,
                    FullName = dto.FullName,
                    Phone = dto.Phone,
                    Role = "customer"
                }
            };
        }


        // =====================================================================
        // LOGIN – chặn email chưa verify
        // =====================================================================
        public async Task<AuthResponseDto?> LoginAsync(LoginDto dto)
        {
            var authResponse = await _authHelper.SignInAsync(dto.Email, dto.Password);

            if (authResponse?.User?.Id == null)
                return null;

            // CHẶN nếu chưa xác minh email
            if (authResponse.User.EmailConfirmedAt == null)
                throw new Exception("EmailNotVerified");

            var userId = Guid.Parse(authResponse.User.Id);

            var response = await _httpClient.GetAsync(
                $"{_supabaseUrl}/rest/v1/users?id=eq.{userId}&select=*"
            );

            if (!response.IsSuccessStatusCode)
                return null;

            var userJson = await response.Content.ReadAsStringAsync();
            var users = JsonSerializer.Deserialize<List<UserData>>(userJson, _jsonOptions);
            var userData = users?.FirstOrDefault();

            return new AuthResponseDto
            {
                AccessToken = authResponse.AccessToken ?? "",
                RefreshToken = authResponse.RefreshToken ?? "",
                User = new UserSessionDto
                {
                    Id = userId,
                    Email = userData?.Email ?? dto.Email,
                    FullName = userData?.FullName ?? "",
                    Phone = userData?.Phone,
                    AvatarUrl = userData?.AvatarUrl,
                    Role = userData?.Role ?? "customer"
                }
            };
        }

        // =====================================================================
        // FORGOT PASSWORD – Supabase gửi email reset
        // =====================================================================
        public async Task<bool> ForgotPasswordAsync(string email)
        {
            var payload = new { email };

            var req = new HttpRequestMessage(
                HttpMethod.Post,
                $"{_supabaseUrl}/auth/v1/recover"
            );

            req.Content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json"
            );

            req.Headers.Add("apikey", _supabaseServiceKey);

            var res = await _httpClient.SendAsync(req);

            return res.IsSuccessStatusCode;
        }

        // =====================================================================
        // LOGOUT (Supabase, optional)
        // =====================================================================
        public async Task<bool> LogoutAsync(string accessToken)
        {
            return await _authHelper.SignOutAsync(accessToken);
        }

        // =====================================================================
        // CURRENT USER (nếu bạn cần)
        // =====================================================================
        public async Task<UserSessionDto?> GetCurrentUserAsync(string accessToken)
        {
            var user = await _authHelper.GetUserAsync(accessToken);

            if (user?.Id == null)
                return null;

            var userId = Guid.Parse(user.Id);

            var response = await _httpClient.GetAsync(
                $"{_supabaseUrl}/rest/v1/users?id=eq.{userId}&select=*"
            );

            if (!response.IsSuccessStatusCode)
                return null;

            var userJson = await response.Content.ReadAsStringAsync();
            var users = JsonSerializer.Deserialize<List<UserData>>(userJson, _jsonOptions);
            var userData = users?.FirstOrDefault();

            return new UserSessionDto
            {
                Id = userId,
                Email = userData?.Email ?? user.Email ?? "",
                FullName = userData?.FullName ?? "",
                Phone = userData?.Phone,
                AvatarUrl = userData?.AvatarUrl,
                Role = userData?.Role ?? "customer"
            };
        }
        public async Task<bool> VerifyEmailAsync(string accessToken)
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", accessToken);

            client.DefaultRequestHeaders.Add("apikey", _supabaseServiceKey);

            // 1. Lấy thông tin user từ auth.users
            var res = await client.GetAsync($"{_supabaseUrl}/auth/v1/user");

            if (!res.IsSuccessStatusCode)
                return false;

            var json = await res.Content.ReadAsStringAsync();
            var authUser = JsonSerializer.Deserialize<SupabaseUser>(json, _jsonOptions);

            if (authUser == null || authUser.Id == null)
                return false;

            // 2. Update public.users.updated_at (optional)
            var updateBody = new
            {
                updated_at = DateTime.UtcNow
            };

            var updateJson = JsonSerializer.Serialize(updateBody, _jsonOptions);
            var content = new StringContent(updateJson, Encoding.UTF8, "application/json");

            await _httpClient.PatchAsync(
                $"{_supabaseUrl}/rest/v1/users?id=eq.{authUser.Id}",
                content);

            return true;
        }

    }

    // =====================================================================
    // DATA MODEL FOR DATABASE (users table)
    // =====================================================================
    public class UserData
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? AvatarUrl { get; set; }
        public string Role { get; set; } = "customer";
    }
}
