using Microsoft.AspNetCore.Mvc;

namespace WearMate.Web.Controllers;

public class LanguageController : Controller
{
    private readonly string[] _supportedCultures = { "vi", "en" };

    [HttpPost]
    [Route("Language/SetLanguage")]
    public IActionResult SetLanguage(string culture, string returnUrl)
    {
        if (!_supportedCultures.Contains(culture))
        {
            culture = "en";
        }

        Response.Cookies.Append("culture", culture, new CookieOptions
        {
            Expires = DateTimeOffset.UtcNow.AddYears(1),
            IsEssential = true,
            Path = "/"
        });

        if (!string.IsNullOrEmpty(returnUrl))
        {
            var segments = returnUrl.Split('/', StringSplitOptions.RemoveEmptyEntries);
            
            if (segments.Length > 0 && _supportedCultures.Contains(segments[0]))
            {
                var pathWithoutCulture = "/" + string.Join("/", segments.Skip(1));
                returnUrl = $"/{culture}{pathWithoutCulture}";
            }
            else
            {
                returnUrl = $"/{culture}{returnUrl}";
            }
            
            return Redirect(returnUrl);
        }

        return Redirect($"/{culture}");
    }
}
