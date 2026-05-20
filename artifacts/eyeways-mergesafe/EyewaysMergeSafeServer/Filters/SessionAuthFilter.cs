using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace EyewaysMergeSafeServer.Filters;

/// <summary>
/// Global action filter — enforces session authentication for all routes except:
///   • Portal / Home controllers (login page)
///   • GET /api/* (read-only public stats/status)
/// POST /api/* write endpoints require either a valid session or X-Device-Token header.
/// </summary>
public class SessionAuthFilter : IActionFilter
{
    private static readonly HashSet<string> _publicControllers =
        new(StringComparer.OrdinalIgnoreCase) { "Portal", "Home" };

    public void OnActionExecuting(ActionExecutingContext context)
    {
        var ctrl    = context.RouteData.Values["controller"]?.ToString() ?? "";
        var path    = context.HttpContext.Request.Path;
        var method  = context.HttpContext.Request.Method;

        // Always allow Portal / Home
        if (_publicControllers.Contains(ctrl)) return;

        // Allow GET /api/* without authentication (read-only)
        if (path.StartsWithSegments("/api") && method == HttpMethods.Get) return;

        // POST /api/* : allow with valid session OR X-Device-Token header
        if (path.StartsWithSegments("/api") && method == HttpMethods.Post)
        {
            if (HasValidSession(context) || HasValidDeviceToken(context)) return;
            context.Result = new UnauthorizedResult();
            return;
        }

        // All other routes: require valid session
        if (!HasValidSession(context))
        {
            context.Result = new RedirectToActionResult("Index", "Portal", null);
        }
    }

    public void OnActionExecuted(ActionExecutedContext context) { }

    private static bool HasValidSession(ActionExecutingContext ctx)
        => !string.IsNullOrEmpty(ctx.HttpContext.Session.GetString("HighwayId"));

    private static bool HasValidDeviceToken(ActionExecutingContext ctx)
    {
        var token = ctx.HttpContext.Request.Headers["X-Device-Token"].ToString();
        if (string.IsNullOrEmpty(token)) return false;

        var cfg          = ctx.HttpContext.RequestServices.GetService<IConfiguration>();
        var configuredKey = cfg?["DeviceApiKey"];

        // If no key is configured, fall back to requiring session only
        if (string.IsNullOrEmpty(configuredKey)) return false;

        return token == configuredKey;
    }
}
