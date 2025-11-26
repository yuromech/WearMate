using Microsoft.AspNetCore.Mvc;
using System.Globalization;

namespace WearMate.Web.Helpers;

public static class UrlHelperExtensions
{
    public static string? CultureAction(
        this IUrlHelper urlHelper,
        string? action = null,
        string? controller = null,
        object? routeValues = null)
    {
        var culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
        
        var values = new RouteValueDictionary(routeValues);
        values["culture"] = culture;

        return urlHelper.Action(action, controller, values);
    }

    public static string? CultureRouteUrl(
        this IUrlHelper urlHelper,
        string? routeName,
        object? routeValues = null)
    {
        var culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
        
        var values = new RouteValueDictionary(routeValues);
        values["culture"] = culture;

        return urlHelper.RouteUrl(routeName, values);
    }
}
