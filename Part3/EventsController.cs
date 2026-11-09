using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Auth;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/events")]
public class EventsController : ControllerBase
{
    private readonly RaceDayContext _db;

    public EventsController(RaceDayContext db)
    {
        _db = db;
    }

    private static EventResponse ToResponse(Event e) => new()
    {
        EventId = e.EventId,
        OrganiserId = e.OrganiserId,
        Name = e.Name,
        Description = e.Description,
        Location = e.Location,
        EventDate = e.EventDate
    };

    // GET /api/events - public
    /// <summary>Lists all events. Public.</summary>
    /// <remarks>
    /// <b>Input:</b> No body.<br/>
    /// <b>Returns:</b> 200 OK with a list of events.
    /// </remarks>
    [HttpGet]
    [ProducesResponseType(200)]
    public async Task<ActionResult<IEnumerable<EventResponse>>> GetAll()
    {
        var events = await _db.Events.AsNoTracking().ToListAsync();
        return Ok(events.Select(ToResponse));
    }

    // GET /api/events/{id} - public
    /// <summary>Gets the details of one event. Public.</summary>
    /// <remarks>
    /// <b>Input:</b> Route: event id.<br/>
    /// <b>Returns:</b> 200 OK with the event; 404 not found.
    /// </remarks>
    [HttpGet("{id:int}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<EventResponse>> GetById(int id)
    {
        var ev = await _db.Events.AsNoTracking().FirstOrDefaultAsync(e => e.EventId == id);
        if (ev is null) return NotFound();
        return Ok(ToResponse(ev));
    }

    // POST /api/events - Organiser
    /// <summary>Creates a new event owned by the logged-in Organiser.</summary>
    /// <remarks>
    /// <b>Input:</b> JSON body: name, description, location, eventDate. Organiser only.<br/>
    /// <b>Returns:</b> 201 Created with the event; 401 not logged in; 403 not an Organiser.
    /// </remarks>
    [Authorize(Policy = "OrganiserOnly")]
    [HttpPost]
    [ProducesResponseType(201)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<ActionResult<EventResponse>> Create(EventRequest request)
    {
        var organiserId = User.GetUserId()!.Value;

        var ev = new Event
        {
            OrganiserId = organiserId,
            Name = request.Name,
            Description = request.Description,
            Location = request.Location,
            EventDate = request.EventDate
        };
        _db.Events.Add(ev);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = ev.EventId }, ToResponse(ev));
    }

    // PUT /api/events/{id} - Organiser, must own the event
    /// <summary>Updates an event the Organiser owns.</summary>
    /// <remarks>
    /// <b>Input:</b> Route: event id. JSON body: name, description, location, eventDate. Organiser only.<br/>
    /// <b>Returns:</b> 200 OK with updated event; 403 not the owner / not an Organiser; 404 not found.
    /// </remarks>
    [Authorize(Policy = "OrganiserOnly")]
    [HttpPut("{id:int}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<EventResponse>> Update(int id, EventRequest request)
    {
        var ev = await _db.Events.FindAsync(id);
        if (ev is null) return NotFound();
        if (ev.OrganiserId != User.GetUserId()) return Forbid();

        ev.Name = request.Name;
        ev.Description = request.Description;
        ev.Location = request.Location;
        ev.EventDate = request.EventDate;
        await _db.SaveChangesAsync();

        return Ok(ToResponse(ev));
    }

    // DELETE /api/events/{id} - Organiser, must own the event
    /// <summary>Deletes an event the Organiser owns.</summary>
    /// <remarks>
    /// <b>Input:</b> Route: event id. Organiser only.<br/>
    /// <b>Returns:</b> 204 No Content; 403 not the owner / not an Organiser; 404 not found.
    /// </remarks>
    [Authorize(Policy = "OrganiserOnly")]
    [HttpDelete("{id:int}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Delete(int id)
    {
        var ev = await _db.Events.FindAsync(id);
        if (ev is null) return NotFound();
        if (ev.OrganiserId != User.GetUserId()) return Forbid();

        var hasEnrolments = await _db.Enrolments.AnyAsync(en => en.Category!.EventId == id);
        if (hasEnrolments)
            return Conflict(new { message = "This event cannot be deleted because participants have already enrolled." });

        _db.Events.Remove(ev);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
