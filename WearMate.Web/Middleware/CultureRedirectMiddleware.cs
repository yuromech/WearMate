using System.Globalization;

namespace WearMate.Web.Middleware;

public class CultureRedirectMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string[] _supportedCultures = { "vi", "en" };
    private readonly string _defaultCulture = "en";

    public CultureRedirectMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "/";

        if (path.StartsWith("/Admin", StringComparison.OrdinalIgnoreCase) || 
            path.StartsWith("/Language/SetLanguage", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        
        if (segments.Length == 0 || !_supportedCultures.Contains(segments[0]))
        {
            var cultureCookie = context.Request.Cookies["culture"];
            var culture = _supportedCultures.Contains(cultureCookie) ? cultureCookie : _defaultCulture;
            
            var newPath = $"/{culture}{path}";
            context.Response.Redirect(newPath, permanent: false);
            return;
        }

        var requestedCulture = segments[0];
        var cultureInfo = new CultureInfo(requestedCulture);
        CultureInfo.CurrentCulture = cultureInfo;
        CultureInfo.CurrentUICulture = cultureInfo;

        context.Response.Cookies.Append("culture", requestedCulture, new CookieOptions
        {
            Expires = DateTimeOffset.UtcNow.AddYears(1),
            IsEssential = true,
            Path = "/"
        });

        await _next(context);
    }
}
