using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Text.Json;
using WearMate.Shared.DTOs.Auth;

namespace WearMate.Web.Middleware;

public class AdminAuthorizeAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var session = context.HttpContext.Session.GetString("UserSession");

        if (string.IsNullOrEmpty(session))
        {
            context.Result = new RedirectToActionResult("Login", "Account", new { area = "", returnUrl = context.HttpContext.Request.Path });
            return;
        }

        try
        {
            var sessionData = JsonSerializer.Deserialize<JsonElement>(session);
            var userJson = sessionData.GetProperty("User").GetRawText();
            var user = JsonSerializer.Deserialize<UserSessionDto>(userJson);

            if (user?.Role != "admin" && user?.Role != "staff")
            {
                context.Result = new RedirectToActionResult("AccessDenied", "Account", new { area = "" });
                return;
            }
        }
        catch
        {
            context.Result = new RedirectToActionResult("Login", "Account", new { area = "" });
            return;
        }

        base.OnActionExecuting(context);
    }
}