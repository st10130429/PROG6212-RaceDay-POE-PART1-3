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
public class ResultsController : ControllerBase
{
    private readonly RaceDayContext _db;

    public ResultsController(RaceDayContext db)
    {
        _db = db;
    }

    private static ResultResponse ToResponse(Result r) => new()
    {
        ResultId = r.ResultId,
        EnrolmentId = r.EnrolmentId,
        FinishTime = r.FinishTime,
        Position = r.Position
    };

    // POST /api/enrolments/{enrolmentId}/results - Organiser, must own the event behind the enrolment
    /// <summary>Captures a finish time and position for an enrolment in an event the Organiser owns.</summary>
    /// <remarks>
    /// <b>Input:</b> Route: enrolment id. JSON body: finishTime (hh:mm:ss), position. Organiser only.<br/>
    /// <b>Returns:</b> 201 Created with the result; 403 not the owner / not an Organiser; 404 enrolment not found; 409 result already recorded.
    /// </remarks>
    [Authorize(Policy = "OrganiserOnly")]
    [HttpPost("api/enrolments/{enrolmentId:int}/results")]
    [ProducesResponseType(201)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    [ProducesResponseType(409)]
    public async Task<ActionResult<ResultResponse>> Capture(int enrolmentId, ResultRequest request)
    {
        var enrolment = await _db.Enrolments
            .Include(e => e.Category)!.ThenInclude(c => c!.Event)
            .Include(e => e.Result)
            .FirstOrDefaultAsync(e => e.EnrolmentId == enrolmentId);

        if (enrolment is null) return NotFound();
        if (enrolment.Category!.Event!.OrganiserId != User.GetUserId()) return Forbid();
        if (enrolment.Category.Event.EventDate > DateTime.Now)
            return BadRequest(new { message = "Results can only be entered after the event has ended." });
        if (enrolment.Result is not null)
            return Conflict(new { message = "A result has already been recorded for this enrolment." });

        var result = new Result
        {
            EnrolmentId = enrolmentId,
            FinishTime = request.FinishTime,
            Position = request.Position
        };
        _db.Results.Add(result);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetMine), null, ToResponse(result));
    }

    // GET /api/results/me - Participant
    /// <summary>Lists the logged-in Participant's own result history.</summary>
    /// <remarks>
    /// <b>Input:</b> No body. Participant only.<br/>
    /// <b>Returns:</b> 200 OK with a list of results; 403 not a Participant.
    /// </remarks>
    [Authorize(Policy = "ParticipantOnly")]
    [HttpGet("api/results/me")]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<ActionResult<IEnumerable<ResultResponse>>> GetMine()
    {
        var participantId = User.GetUserId()!.Value;
        var results = await _db.Results.AsNoTracking()
            .Where(r => r.Enrolment!.ParticipantId == participantId)
            .ToListAsync();
        return Ok(results.Select(ToResponse));
    }
}
