using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace EyewaysMergeSafeServer.Filters;

public class SessionAuthFilter : IActionFilter
{
    private static readonly HashSet<string> _publicControllers = new(StringComparer.OrdinalIgnoreCase)
    {
        "Portal", "Home"
    };

    private static readonly HashSet<string> _publicAreas = new(StringComparer.OrdinalIgnoreCase)
    {
        "api"
    };

    public void OnActionExecuting(ActionExecutingContext context)
    {
        var ctrl = context.RouteData.Values["controller"]?.ToString() ?? "";
        var area = context.RouteData.Values["area"]?.ToString() ?? "";

        if (_publicControllers.Contains(ctrl) || _publicAreas.Contains(area))
            return;

        if (context.HttpContext.Request.Path.StartsWithSegments("/api"))
            return;

        var session = context.HttpContext.Session;
        var highwayId = session.GetString("HighwayId");

        if (string.IsNullOrEmpty(highwayId))
        {
            context.Result = new RedirectToActionResult("Index", "Portal", null);
        }
    }

    public void OnActionExecuted(ActionExecutedContext context) { }
}
