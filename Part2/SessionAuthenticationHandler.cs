using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace RaceDay.Api.Auth;

public static class SessionAuthDefaults
{
    public const string Scheme = "Session";
    public const string UserIdKey = "UserId";
    public const string RoleKey = "Role";
    public const string NameKey = "Name";
    public const string EmailKey = "Email";
}

/// <summary>
/// Authenticates each request from the SERVER-SIDE session. On login the API stores the
/// user's ID and role in the server's session store; the browser only holds an opaque
/// session-id cookie. This handler rebuilds the user's identity (and role) from that
/// session on every request, so [Authorize] and role policies work as normal.
/// </summary>
public class SessionAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public SessionAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder) : base(options, logger, encoder)
    {
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        await Context.Session.LoadAsync();

        var userId = Context.Session.GetInt32(SessionAuthDefaults.UserIdKey);
        var role = Context.Session.GetString(SessionAuthDefaults.RoleKey);

        if (userId is null || string.IsNullOrEmpty(role))
            return AuthenticateResult.NoResult();

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.Value.ToString()),
            new(ClaimTypes.Role, role),
            new(ClaimTypes.Name, Context.Session.GetString(SessionAuthDefaults.NameKey) ?? string.Empty),
            new(ClaimTypes.Email, Context.Session.GetString(SessionAuthDefaults.EmailKey) ?? string.Empty)
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name);
        return AuthenticateResult.Success(ticket);
    }

    // Not logged in -> 401 (no redirect to a login page; this is an API)
    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    }

    // Logged in but wrong role / not the owner -> 403
    protected override Task HandleForbiddenAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    }
}
