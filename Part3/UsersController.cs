using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Auth;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly RaceDayContext _db;

    public UsersController(RaceDayContext db)
    {
        _db = db;
    }

    // GET /api/users/me
    /// <summary>Returns the logged-in user's own profile.</summary>
    /// <remarks>
    /// <b>Input:</b> No body. Requires login.<br/>
    /// <b>Returns:</b> 200 OK with the profile; 401 not logged in.
    /// </remarks>
    [HttpGet("me")]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<UserResponse>> GetMe()
    {
        var userId = User.GetUserId();
        var user = await _db.Users.FindAsync(userId);
        if (user is null) return NotFound();

        return Ok(new UserResponse
        {
            UserId = user.UserId,
            Name = user.Name,
            Email = user.Email,
            Role = user.Role
        });
    }

    // PUT /api/users/me
    /// <summary>Updates the logged-in user's own name and email.</summary>
    /// <remarks>
    /// <b>Input:</b> JSON body: name, email. Requires login.<br/>
    /// <b>Returns:</b> 200 OK with updated profile; 400 invalid data or email in use; 401 not logged in.
    /// </remarks>
    [HttpPut("me")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<UserResponse>> UpdateMe(UpdateProfileRequest request)
    {
        var userId = User.GetUserId();
        var user = await _db.Users.FindAsync(userId);
        if (user is null) return NotFound();

        var emailTaken = await _db.Users.AnyAsync(u => u.Email == request.Email && u.UserId != userId);
        if (emailTaken) return BadRequest(new { message = "That email is already in use." });

        user.Name = request.Name;
        user.Email = request.Email;
        await _db.SaveChangesAsync();

        return Ok(new UserResponse
        {
            UserId = user.UserId,
            Name = user.Name,
            Email = user.Email,
            Role = user.Role
        });
    }
}
