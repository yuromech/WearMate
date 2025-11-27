using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using WearMate.Shared.DTOs.Auth;
using WearMate.Web.Services;
using WearMate.Web.ApiClients;
using WearMate.Web.Models.ViewModels;
using WearMate.Shared.DTOs.Orders;

namespace WearMate.Web.Controllers;

public class AccountController : Controller
{
    private readonly AuthService _authService;
    private readonly OrderApiClient _orderApi;
    private readonly ILogger<AccountController> _logger;
    private const string SessionKey = "UserSession";
    private const string CartSessionKey = "ShoppingCart";

    public AccountController(AuthService authService, OrderApiClient orderApi, ILogger<AccountController> logger)
    {
        _authService = authService;
        _orderApi = orderApi;
        _logger = logger;
    }

    // ===================================================
    // LOGIN
    // ===================================================
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginDto model, string? returnUrl = null)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Vui lòng điền đầy đủ thông tin đăng nhập.";
            return View(model);
        }

        try
        {
            var result = await _authService.LoginAsync(model);

            if (result == null)
            {
                TempData["Error"] = "Email hoặc mật khẩu không đúng. Vui lòng thử lại.";
                ModelState.AddModelError("", "Email hoặc mật khẩu không đúng");
                return View(model);
            }

            // Save session
            var sessionData = new
            {
                AccessToken = result.AccessToken,
                RefreshToken = result.RefreshToken,
                User = result.User
            };

            HttpContext.Session.SetString(SessionKey, JsonSerializer.Serialize(sessionData));

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Index", "Home");
        }
        catch (Exception ex)
        {
            if (ex.Message == "EmailNotVerified")
            {
                TempData["Error"] = "Email của bạn chưa được xác thực. Vui lòng kiểm tra hộp thư của bạn.";
                return RedirectToAction(nameof(Login));
            }

            _logger.LogError(ex, "Login error");
            TempData["Error"] = "Đã xảy ra lỗi trong quá trình đăng nhập. Vui lòng thử lại sau.";
            ModelState.AddModelError("", "Đã xảy ra lỗi trong quá trình đăng nhập.");
            return View(model);
        }

    }

    // ===================================================
    // REGISTER
    // ===================================================
    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Register(RegisterDto model)
    {
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            var result = await _authService.RegisterAsync(model);

            if (result == null)
            {
                ModelState.AddModelError("", "Registration failed. Email may already be in use.");
                return View(model);
            }

            TempData["Success"] = "Registration successful! Please check your email for verification.";
            return RedirectToAction(nameof(Login));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Registration error");
            ModelState.AddModelError("", ex.Message);
            return View(model);
        }
    }

    [HttpGet("/verify")]
    public IActionResult Verify()
    {
        return View("Confirm");
    }


    private string Extract(string text, string key)
    {
        var start = text.IndexOf($"{key}=");
        if (start == -1) return "";

        start += key.Length + 1;
        var end = text.IndexOf("&", start);
        if (end == -1) end = text.Length;

        return text.Substring(start, end - start);
    }

    [HttpPost("/verify/callback")]
    public async Task<IActionResult> VerifyCallback([FromBody] EmailVerifyDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.AccessToken))
            return BadRequest("Missing token");

        try
        {
            // Gọi AuthService để xác minh email
            bool ok = await _authService.VerifyEmailAsync(dto.AccessToken);

            if (!ok)
                return BadRequest("Invalid or expired verification token");

            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            Console.WriteLine("VERIFY ERROR: " + ex);
            return StatusCode(500, ex.Message);
        }
    }

    [HttpGet]
    public IActionResult ConfirmResult(bool success)
    {
        return View();
    }

    // ===================================================
    // FORGOT PASSWORD – Supabase gửi email reset
    // ===================================================
    [HttpGet]
    public IActionResult ForgotPassword()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> ForgotPassword(string email)
    {
        var success = await _authService.ForgotPasswordAsync(email);

        TempData[success ? "Success" : "Error"] =
            success ? "A reset link has been sent to your email."
                    : "Unable to send reset email.";

        return RedirectToAction(nameof(ForgotPassword));
    }

    // ===================================================
    // LOGOUT
    // ===================================================
    [HttpPost]
    public IActionResult Logout()
    {
        HttpContext.Session.Remove(SessionKey);
        return RedirectToAction("Index", "Home");
    }

    // ===================================================
    // PROFILE
    // ===================================================
    [HttpGet]
    public IActionResult Profile()
    {
        var sessionJson = HttpContext.Session.GetString(SessionKey);
        if (string.IsNullOrEmpty(sessionJson))
            return RedirectToAction(nameof(Login));

        try
        {
            var sessionData = JsonSerializer.Deserialize<JsonElement>(sessionJson);
            var userJson = sessionData.GetProperty("User").GetRawText();

            var user = JsonSerializer.Deserialize<UserSessionDto>(userJson);
            return View(user);
        }
        catch
        {
            return RedirectToAction(nameof(Login));
        }
    }

    [HttpGet]
    public async Task<IActionResult> Orders(int page = 1, int pageSize = 10)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty) return RedirectToAction(nameof(Login));

        var orders = await _orderApi.GetUserOrdersAsync(userId, page, pageSize)
            ?? new Shared.DTOs.Common.PaginatedResult<Shared.DTOs.Orders.OrderDto>
            {
                Items = new List<Shared.DTOs.Orders.OrderDto>(),
                Page = page,
                PageSize = pageSize
            };

        return View(orders);
    }

    private Guid GetCurrentUserId()
    {
        var sessionJson = HttpContext.Session.GetString(SessionKey);
        if (string.IsNullOrEmpty(sessionJson)) return Guid.Empty;

        try
        {
            var sessionData = JsonSerializer.Deserialize<JsonElement>(sessionJson);
            var userIdStr = sessionData.GetProperty("User").GetProperty("Id").GetString();
            return Guid.Parse(userIdStr ?? Guid.Empty.ToString());
        }
        catch
        {
            return Guid.Empty;
        }
    }

    private List<CartItemViewModel> GetCartFromSession()
    {
        var json = HttpContext.Session.GetString(CartSessionKey);
        if (string.IsNullOrEmpty(json))
            return new List<CartItemViewModel>();

        return JsonSerializer.Deserialize<List<CartItemViewModel>>(json) ?? new();
    }
}
