using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using RaceDay.Api.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly RaceDayContext _db;
    private readonly PasswordHasher<User> _hasher = new();

    public AuthController(RaceDayContext db)
    {
        _db = db;
    }

    // POST /api/auth/register
    /// <summary>Registers a new user account as an Organiser or a Participant.</summary>
    /// <remarks>
    /// <b>Input:</b> JSON body: name, email, password (min 6 chars), role ("Organiser" or "Participant").<br/>
    /// <b>Returns:</b> 201 Created with the new user (never the password); 400 invalid role/data; 409 email already registered.
    /// </remarks>
    [HttpPost("register")]
    [ProducesResponseType(201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(409)]
    public async Task<ActionResult<UserResponse>> Register(RegisterRequest request)
    {
        if (!request.IsValidRole())
            return BadRequest(new { message = "Role must be 'Organiser' or 'Participant'." });

        var emailExists = await _db.Users.AnyAsync(u => u.Email == request.Email);
        if (emailExists)
            return Conflict(new { message = "A user with this email already exists." });

        var user = new User
        {
            Name = request.Name,
            Email = request.Email,
            Role = request.Role
        };
        user.PasswordHash = _hasher.HashPassword(user, request.Password);

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(Register), new UserResponse
        {
            UserId = user.UserId,
            Name = user.Name,
            Email = user.Email,
            Role = user.Role
        });
    }

    // POST /api/auth/login
    /// <summary>Logs in and starts a server-side session storing the user ID and role.</summary>
    /// <remarks>
    /// <b>Input:</b> JSON body: email, password.<br/>
    /// <b>Returns:</b> 200 OK with user info and a session cookie; 401 invalid credentials.
    /// </remarks>
    [HttpPost("login")]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    public async Task<ActionResult<UserResponse>> Login(LoginRequest request)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user is null)
            return Unauthorized(new { message = "Invalid email or password." });

        var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (result == PasswordVerificationResult.Failed)
            return Unauthorized(new { message = "Invalid email or password." });

        // Store the user's ID and role in the SERVER-SIDE session.
        await HttpContext.Session.LoadAsync();
        HttpContext.Session.Clear();
        HttpContext.Session.SetInt32(SessionAuthDefaults.UserIdKey, user.UserId);
        HttpContext.Session.SetString(SessionAuthDefaults.RoleKey, user.Role);
        HttpContext.Session.SetString(SessionAuthDefaults.NameKey, user.Name);
        HttpContext.Session.SetString(SessionAuthDefaults.EmailKey, user.Email);

        return Ok(new UserResponse
        {
            UserId = user.UserId,
            Name = user.Name,
            Email = user.Email,
            Role = user.Role
        });
    }

    // POST /api/auth/logout
    /// <summary>Logs out and clears the server-side session.</summary>
    /// <remarks>
    /// <b>Input:</b> No body.<br/>
    /// <b>Returns:</b> 204 No Content; 401 if not logged in.
    /// </remarks>
    [Authorize]
    [HttpPost("logout")]
    [ProducesResponseType(204)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.Session.LoadAsync();
        HttpContext.Session.Clear();
        return NoContent();
    }
}
