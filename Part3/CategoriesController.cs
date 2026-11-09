using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Auth;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[ApiController]
public class CategoriesController : ControllerBase
{
    private readonly RaceDayContext _db;

    public CategoriesController(RaceDayContext db)
    {
        _db = db;
    }

    private static CategoryResponse ToResponse(Category c) => new()
    {
        CategoryId = c.CategoryId,
        EventId = c.EventId,
        Name = c.Name,
        DistanceKm = c.DistanceKm,
        Price = c.Price
    };

    // GET /api/events/{eventId}/categories - public
    /// <summary>Lists the categories of an event. Public.</summary>
    /// <remarks>
    /// <b>Input:</b> Route: event id.<br/>
    /// <b>Returns:</b> 200 OK with a list of categories; 404 event not found.
    /// </remarks>
    [HttpGet("api/events/{eventId:int}/categories")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<IEnumerable<CategoryResponse>>> GetForEvent(int eventId)
    {
        var eventExists = await _db.Events.AnyAsync(e => e.EventId == eventId);
        if (!eventExists) return NotFound();

        var categories = await _db.Categories.AsNoTracking()
            .Where(c => c.EventId == eventId)
            .ToListAsync();
        return Ok(categories.Select(ToResponse));
    }

    // POST /api/events/{eventId}/categories - Organiser, must own the event
    /// <summary>Adds a category to an event the Organiser owns.</summary>
    /// <remarks>
    /// <b>Input:</b> Route: event id. JSON body: name, distanceKm, price. Organiser only.<br/>
    /// <b>Returns:</b> 201 Created with the category; 403 not the owner / not an Organiser; 404 event not found.
    /// </remarks>
    [Authorize(Policy = "OrganiserOnly")]
    [HttpPost("api/events/{eventId:int}/categories")]
    [ProducesResponseType(201)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<CategoryResponse>> Create(int eventId, CategoryRequest request)
    {
        var ev = await _db.Events.FindAsync(eventId);
        if (ev is null) return NotFound();
        if (ev.OrganiserId != User.GetUserId()) return Forbid();

        var category = new Category
        {
            EventId = eventId,
            Name = request.Name,
            DistanceKm = request.DistanceKm,
            Price = request.Price
        };
        _db.Categories.Add(category);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = category.CategoryId }, ToResponse(category));
    }

    // GET /api/categories/{id} - public (used for CreatedAtAction + convenience lookup)
    /// <summary>Gets one category by id. Public.</summary>
    /// <remarks>
    /// <b>Input:</b> Route: category id.<br/>
    /// <b>Returns:</b> 200 OK with the category; 404 not found.
    /// </remarks>
    [HttpGet("api/categories/{id:int}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<CategoryResponse>> GetById(int id)
    {
        var category = await _db.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.CategoryId == id);
        if (category is null) return NotFound();
        return Ok(ToResponse(category));
    }

    // PUT /api/categories/{id} - Organiser, must own the parent event
    /// <summary>Updates a category belonging to an event the Organiser owns.</summary>
    /// <remarks>
    /// <b>Input:</b> Route: category id. JSON body: name, distanceKm, price. Organiser only.<br/>
    /// <b>Returns:</b> 200 OK with updated category; 403 not the owner / not an Organiser; 404 not found.
    /// </remarks>
    [Authorize(Policy = "OrganiserOnly")]
    [HttpPut("api/categories/{id:int}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<CategoryResponse>> Update(int id, CategoryRequest request)
    {
        var category = await _db.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryId == id);
        if (category is null) return NotFound();
        if (category.Event!.OrganiserId != User.GetUserId()) return Forbid();

        category.Name = request.Name;
        category.DistanceKm = request.DistanceKm;
        category.Price = request.Price;
        await _db.SaveChangesAsync();

        return Ok(ToResponse(category));
    }

    // DELETE /api/categories/{id} - Organiser, must own the parent event
    /// <summary>Deletes a category belonging to an event the Organiser owns.</summary>
    /// <remarks>
    /// <b>Input:</b> Route: category id. Organiser only.<br/>
    /// <b>Returns:</b> 204 No Content; 403 not the owner / not an Organiser; 404 not found.
    /// </remarks>
    [Authorize(Policy = "OrganiserOnly")]
    [HttpDelete("api/categories/{id:int}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Delete(int id)
    {
        var category = await _db.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryId == id);
        if (category is null) return NotFound();
        if (category.Event!.OrganiserId != User.GetUserId()) return Forbid();

        var hasEnrolments = await _db.Enrolments.AnyAsync(en => en.CategoryId == id);
        if (hasEnrolments)
            return Conflict(new { message = "This category cannot be deleted because participants have enrolled in it." });

        _db.Categories.Remove(category);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
