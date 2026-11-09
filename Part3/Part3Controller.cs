using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Auth;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

/// <summary>
/// Part 3 endpoints. These ADD to the Part 2 controllers (nothing existing is changed) and give the
/// MVC app the richer data it needs: event meta + banner, dashboard counts, entrants, personal
/// enrolments/results, profile picture and enrolment status. Role + ownership rules match Part 2.
/// </summary>
[ApiController]
[Route("api")]
public class Part3Controller : ControllerBase
{
    private readonly RaceDayContext _db;
    private readonly IBlobStorageService _blob;
    private readonly ILogger<Part3Controller> _log;

    public Part3Controller(RaceDayContext db, IBlobStorageService blob, ILogger<Part3Controller> log)
    { _db = db; _blob = blob; _log = log; }

    private static EventDetailResponse ToDetail(Event e) => new()
    {
        EventId = e.EventId, OrganiserId = e.OrganiserId, Name = e.Name, Description = e.Description,
        Location = e.Location, EventDate = e.EventDate, EventType = e.EventType, DistanceKm = e.DistanceKm, BannerUrl = e.BannerUrl
    };

    // ---- Public ----
    /// <summary>Lists all events with Part 3 fields (type, distance, banner).</summary>
    [HttpGet("events/browse")]
    public async Task<ActionResult<List<EventDetailResponse>>> Browse() =>
        (await _db.Events.AsNoTracking().OrderBy(e => e.EventDate).ToListAsync()).Select(ToDetail).ToList();

    /// <summary>Gets one event with Part 3 fields.</summary>
    [HttpGet("events/{id:int}/detail")]
    public async Task<ActionResult<EventDetailResponse>> Detail(int id)
    {
        var e = await _db.Events.AsNoTracking().FirstOrDefaultAsync(x => x.EventId == id);
        return e is null ? NotFound() : ToDetail(e);
    }

    // ---- Organiser ----
    /// <summary>Sets the event type and distance on an event the Organiser owns.</summary>
    [Authorize(Policy = "OrganiserOnly")]
    [HttpPut("events/{id:int}/meta")]
    public async Task<IActionResult> SetMeta(int id, EventMetaRequest req)
    {
        var e = await _db.Events.FindAsync(id);
        if (e is null) return NotFound();
        if (e.OrganiserId != User.GetUserId()) return Forbid();
        e.EventType = req.EventType; e.DistanceKm = req.DistanceKm;
        await _db.SaveChangesAsync();
        return Ok(ToDetail(e));
    }

    /// <summary>Uploads an event banner to Azure Blob Storage (multipart field "file").</summary>
    [Authorize(Policy = "OrganiserOnly")]
    [HttpPost("events/{id:int}/banner")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> UploadBanner(int id, IFormFile file)
    {
        var e = await _db.Events.FindAsync(id);
        if (e is null) return NotFound();
        if (e.OrganiserId != User.GetUserId()) return Forbid();
        try { e.BannerUrl = await _blob.UploadImageAsync(file, "banners"); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        await _db.SaveChangesAsync();
        return Ok(ToDetail(e));
    }

    /// <summary>Dashboard: the Organiser's events with enrolment totals.</summary>
    [Authorize(Policy = "OrganiserOnly")]
    [HttpGet("organiser/dashboard")]
    public async Task<ActionResult<List<DashboardItemResponse>>> Dashboard()
    {
        var uid = User.GetUserId();
        return await _db.Events.AsNoTracking().Where(e => e.OrganiserId == uid).OrderBy(e => e.EventDate)
            .Select(e => new DashboardItemResponse
            {
                EventId = e.EventId, Name = e.Name, EventDate = e.EventDate, Location = e.Location,
                EnrolmentCount = e.Categories.SelectMany(c => c.Enrolments).Count()
            }).ToListAsync();
    }

    /// <summary>Everyone enrolled in an event (with category, status and any result).</summary>
    [Authorize(Policy = "OrganiserOnly")]
    [HttpGet("events/{id:int}/entrants")]
    public async Task<ActionResult<List<EntrantResponse>>> Entrants(int id)
    {
        var e = await _db.Events.AsNoTracking().FirstOrDefaultAsync(x => x.EventId == id);
        if (e is null) return NotFound();
        if (e.OrganiserId != User.GetUserId()) return Forbid();
        return await _db.Enrolments.AsNoTracking().Where(en => en.Category!.EventId == id)
            .OrderBy(en => en.Participant!.Name)
            .Select(en => new EntrantResponse
            {
                EnrolmentId = en.EnrolmentId, ParticipantName = en.Participant!.Name, ParticipantEmail = en.Participant.Email,
                CategoryName = en.Category!.Name, Status = en.Status, HasResult = en.Result != null,
                FinishTime = en.Result != null ? en.Result.FinishTime : null, Position = en.Result != null ? en.Result.Position : null
            }).ToListAsync();
    }

    /// <summary>Confirms/changes an enrolment's status (Pending, Confirmed, Cancelled).</summary>
    [Authorize(Policy = "OrganiserOnly")]
    [HttpPut("enrolments/{id:int}/status")]
    public async Task<IActionResult> SetStatus(int id, StatusRequest req)
    {
        if (!new[] { "Pending", "Confirmed", "Cancelled" }.Contains(req.Status)) return BadRequest();
        var en = await _db.Enrolments.Include(x => x.Category).ThenInclude(c => c!.Event).FirstOrDefaultAsync(x => x.EnrolmentId == id);
        if (en is null) return NotFound();
        if (en.Category!.Event!.OrganiserId != User.GetUserId()) return Forbid();
        en.Status = req.Status;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ---- Participant ----
    /// <summary>The Participant's enrolments with event name/date, category and status.</summary>
    [Authorize(Policy = "ParticipantOnly")]
    [HttpGet("enrolments/me/detail")]
    public async Task<ActionResult<List<MyEnrolmentResponse>>> MyEnrolments()
    {
        var uid = User.GetUserId();
        return await _db.Enrolments.AsNoTracking().Where(en => en.ParticipantId == uid)
            .OrderBy(en => en.Category!.Event!.EventDate)
            .Select(en => new MyEnrolmentResponse
            {
                EnrolmentId = en.EnrolmentId, EventId = en.Category!.EventId, EventName = en.Category.Event!.Name,
                EventDate = en.Category.Event.EventDate, CategoryName = en.Category.Name, Status = en.Status
            }).ToListAsync();
    }

    /// <summary>The Participant's race history, including their position out of total finishers.</summary>
    [Authorize(Policy = "ParticipantOnly")]
    [HttpGet("results/me/detail")]
    public async Task<ActionResult<List<MyResultResponse>>> MyResults()
    {
        var uid = User.GetUserId();
        return await _db.Results.AsNoTracking().Where(r => r.Enrolment!.ParticipantId == uid)
            .Select(r => new MyResultResponse
            {
                EventName = r.Enrolment!.Category!.Event!.Name, EventDate = r.Enrolment.Category.Event.EventDate,
                CategoryName = r.Enrolment.Category.Name, FinishTime = r.FinishTime, Position = r.Position,
                TotalFinishers = _db.Results.Count(x => x.Enrolment!.CategoryId == r.Enrolment.CategoryId)
            }).ToListAsync();
    }

    // ---- Any logged-in user ----
    /// <summary>Own profile including profile picture URL.</summary>
    [Authorize]
    [HttpGet("users/me/profile")]
    public async Task<ActionResult<UserProfileResponse>> Profile()
    {
        var u = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == User.GetUserId());
        return u is null ? NotFound() : new UserProfileResponse { UserId = u.UserId, Name = u.Name, Email = u.Email, Role = u.Role, ProfilePictureUrl = u.ProfilePictureUrl };
    }

    /// <summary>Uploads a profile picture to Azure Blob Storage (multipart field "file").</summary>
    [Authorize]
    [HttpPost("users/me/picture")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> UploadPicture(IFormFile file)
    {
        var u = await _db.Users.FindAsync(User.GetUserId());
        if (u is null) return NotFound();
        try { u.ProfilePictureUrl = await _blob.UploadImageAsync(file, "profiles"); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        await _db.SaveChangesAsync();
        return Ok(new UserProfileResponse { UserId = u.UserId, Name = u.Name, Email = u.Email, Role = u.Role, ProfilePictureUrl = u.ProfilePictureUrl });
    }
}
