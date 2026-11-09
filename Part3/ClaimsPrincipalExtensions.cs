using System.Security.Claims;

namespace RaceDay.Api.Auth;

public static class ClaimsPrincipalExtensions
{
    /// <summary>Gets the logged-in user's UserId from their session claims, or null if not available.</summary>
    public static int? GetUserId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out var id) ? id : null;
    }

    public static string? GetRole(this ClaimsPrincipal principal)
        => principal.FindFirstValue(ClaimTypes.Role);
}
