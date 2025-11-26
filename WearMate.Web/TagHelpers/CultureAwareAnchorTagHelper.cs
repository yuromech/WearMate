using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using System.Globalization;

namespace WearMate.Web.TagHelpers;

[HtmlTargetElement("a", Attributes = "asp-controller")]
[HtmlTargetElement("a", Attributes = "asp-action")]
public class CultureAwareAnchorTagHelper : TagHelper
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    [ViewContext]
    [HtmlAttributeNotBound]
    public ViewContext? ViewContext { get; set; }

    public CultureAwareAnchorTagHelper(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null) return;

        var path = httpContext.Request.Path.Value ?? "/";
        
        if (path.StartsWith("/Admin", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
        
        if (output.Attributes.ContainsName("asp-route-culture"))
        {
            return;
        }

        output.Attributes.SetAttribute("asp-route-culture", culture);
    }
}
