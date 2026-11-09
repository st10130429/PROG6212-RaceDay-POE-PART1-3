using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Auth;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RouteInfoModel = RaceDay.Api.Models.RouteInformation;

namespace RaceDay.Api.Controllers;

[ApiController]
public class RouteInformationController : ControllerBase
{
    private readonly RaceDayContext _db;

    public RouteInformationController(RaceDayContext db)
    {
        _db = db;
    }

    private static RouteResponse ToResponse(RouteInfoModel r) => new()
    {
        RouteId = r.RouteId,
        CategoryId = r.CategoryId,
        DistanceKm = r.DistanceKm,
        ElevationGain = r.ElevationGain,
        MapUrl = r.MapUrl
    };

    // GET /api/categories/{categoryId}/route - public
    /// <summary>Gets the route information (distance, elevation gain, map URL) for a category. Public.</summary>
    /// <remarks>
    /// <b>Input:</b> Route: category id.<br/>
    /// <b>Returns:</b> 200 OK with the route; 404 not found.
    /// </remarks>
    [HttpGet("api/categories/{categoryId:int}/route")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<RouteResponse>> Get(int categoryId)
    {
        var route = await _db.RouteInformation.AsNoTracking()
            .FirstOrDefaultAsync(r => r.CategoryId == categoryId);
        if (route is null) return NotFound();
        return Ok(ToResponse(route));
    }

    // POST /api/categories/{categoryId}/route - Organiser, must own the parent event
    /// <summary>Adds (or updates) the route information for a category in an event the Organiser owns.</summary>
    /// <remarks>
    /// <b>Input:</b> Route: category id. JSON body: distanceKm, elevationGain, mapUrl. Organiser only.<br/>
    /// <b>Returns:</b> 201 Created (or 200 OK if updated); 403 not the owner / not an Organiser; 404 category not found.
    /// </remarks>
    [Authorize(Policy = "OrganiserOnly")]
    [HttpPost("api/categories/{categoryId:int}/route")]
    [ProducesResponseType(200)]
    [ProducesResponseType(201)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<RouteResponse>> Set(int categoryId, RouteRequest request)
    {
        var category = await _db.Categories.Include(c => c.Event)
            .FirstOrDefaultAsync(c => c.CategoryId == categoryId);
        if (category is null) return NotFound();
        if (category.Event!.OrganiserId != User.GetUserId()) return Forbid();

        var existing = await _db.RouteInformation.FirstOrDefaultAsync(r => r.CategoryId == categoryId);
        if (existing is not null)
        {
            // 1:1 relationship - treat a repeat POST as an update rather than a duplicate error.
            existing.DistanceKm = request.DistanceKm;
            existing.ElevationGain = request.ElevationGain;
            existing.MapUrl = request.MapUrl;
            await _db.SaveChangesAsync();
            return Ok(ToResponse(existing));
        }

        var route = new RouteInfoModel
        {
            CategoryId = categoryId,
            DistanceKm = request.DistanceKm,
            ElevationGain = request.ElevationGain,
            MapUrl = request.MapUrl
        };
        _db.RouteInformation.Add(route);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(Get), new { categoryId }, ToResponse(route));
    }
}
