using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Auth;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[ApiController]
[Authorize]
public class EnrolmentsController : ControllerBase
{
    private readonly RaceDayContext _db;

    public EnrolmentsController(RaceDayContext db)
    {
        _db = db;
    }

    private static EnrolmentResponse ToResponse(Enrolment e) => new()
    {
        EnrolmentId = e.EnrolmentId,
        ParticipantId = e.ParticipantId,
        CategoryId = e.CategoryId,
        EnrolmentDate = e.EnrolmentDate
    };

    // POST /api/categories/{categoryId}/enrolments - Participant
    /// <summary>Enrols the logged-in Participant into a category.</summary>
    /// <remarks>
    /// <b>Input:</b> Route: category id. No body. Participant only.<br/>
    /// <b>Returns:</b> 201 Created with the enrolment; 403 not a Participant; 404 category not found; 409 already enrolled.
    /// </remarks>
    [Authorize(Policy = "ParticipantOnly")]
    [HttpPost("api/categories/{categoryId:int}/enrolments")]
    [ProducesResponseType(201)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    [ProducesResponseType(409)]
    public async Task<ActionResult<EnrolmentResponse>> Enrol(int categoryId)
    {
        var categoryExists = await _db.Categories.AnyAsync(c => c.CategoryId == categoryId);
        if (!categoryExists) return NotFound();

        var participantId = User.GetUserId()!.Value;

        var alreadyEnrolled = await _db.Enrolments
            .AnyAsync(e => e.CategoryId == categoryId && e.ParticipantId == participantId);
        if (alreadyEnrolled)
            return Conflict(new { message = "You are already enrolled in this category." });

        var enrolment = new Enrolment
        {
            CategoryId = categoryId,
            ParticipantId = participantId,
            EnrolmentDate = DateTime.UtcNow
        };
        _db.Enrolments.Add(enrolment);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetMine), null, ToResponse(enrolment));
    }

    // GET /api/enrolments/me - Participant
    /// <summary>Lists the logged-in Participant's own enrolments.</summary>
    /// <remarks>
    /// <b>Input:</b> No body. Participant only.<br/>
    /// <b>Returns:</b> 200 OK with a list of enrolments; 403 not a Participant.
    /// </remarks>
    [Authorize(Policy = "ParticipantOnly")]
    [HttpGet("api/enrolments/me")]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<ActionResult<IEnumerable<EnrolmentResponse>>> GetMine()
    {
        var participantId = User.GetUserId()!.Value;
        var enrolments = await _db.Enrolments.AsNoTracking()
            .Where(e => e.ParticipantId == participantId)
            .ToListAsync();
        return Ok(enrolments.Select(ToResponse));
    }

    // GET /api/events/{eventId}/enrolments - Organiser, must own the event
    /// <summary>Lists all enrolments for an event the Organiser owns.</summary>
    /// <remarks>
    /// <b>Input:</b> Route: event id. Organiser only.<br/>
    /// <b>Returns:</b> 200 OK with a list of enrolments; 403 not the owner / not an Organiser; 404 event not found.
    /// </remarks>
    [Authorize(Policy = "OrganiserOnly")]
    [HttpGet("api/events/{eventId:int}/enrolments")]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<IEnumerable<EnrolmentResponse>>> GetForEvent(int eventId)
    {
        var ev = await _db.Events.FindAsync(eventId);
        if (ev is null) return NotFound();
        if (ev.OrganiserId != User.GetUserId()) return Forbid();

        var enrolments = await _db.Enrolments.AsNoTracking()
            .Where(e => e.Category!.EventId == eventId)
            .Include(e => e.Category)
            .ToListAsync();

        return Ok(enrolments.Select(ToResponse));
    }
}
