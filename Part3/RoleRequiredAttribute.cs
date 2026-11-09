using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace RaceDay.Web.Filters;

/// <summary>
/// Protects a controller/action using the MVC session. Not logged in -> redirect to Login.
/// Logged in with the wrong role -> redirect to that user's own home page.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RoleRequiredAttribute : ActionFilterAttribute
{
    private readonly string? _role;
    public RoleRequiredAttribute(string? role = null) => _role = role;

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var session = context.HttpContext.Session;
        var userId = session.GetInt32("UserId");
        var role = session.GetString("Role");

        if (userId is null || string.IsNullOrEmpty(role))
        {
            context.Result = new RedirectToActionResult("Login", "Account", null);
            return;
        }
        if (_role is not null && role != _role)
        {
            context.Result = role == "Organiser"
                ? new RedirectToActionResult("Dashboard", "Organiser", null)
                : new RedirectToActionResult("Index", "Home", null);
        }
    }
}
