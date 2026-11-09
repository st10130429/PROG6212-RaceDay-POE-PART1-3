using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

[RoleRequired("Organiser")]
public class OrganiserController : Controller
{
    private readonly ApiClient _api;
    private readonly ILogger<OrganiserController> _log;
    public OrganiserController(ApiClient api, ILogger<OrganiserController> log) { _api = api; _log = log; }

    // ---------- Dashboard ----------
    public async Task<IActionResult> Dashboard()
    {
        var res = await _api.GetAsync<List<DashboardItemDto>>("api/organiser/dashboard");
        if (!res.Ok) TempData["Error"] = res.Error;
        return View(res.Data ?? new());
    }

    // ---------- Events ----------
    public async Task<IActionResult> Events()
    {
        var res = await _api.GetAsync<List<DashboardItemDto>>("api/organiser/dashboard");
        if (!res.Ok) TempData["Error"] = res.Error;
        return View((res.Data ?? new()).OrderBy(e => e.EventDate).ToList());
    }

    [HttpGet] public IActionResult CreateEvent() => View("EventForm", new EventFormVm());

    [HttpGet]
    public async Task<IActionResult> EditEvent(int id)
    {
        var res = await _api.GetAsync<EventDto>($"api/events/{id}/detail");
        if (!res.Ok || res.Data is null) { TempData["Error"] = res.Error; return RedirectToAction(nameof(Events)); }
        var e = res.Data;
        return View("EventForm", new EventFormVm
        {
            EventId = e.EventId, Name = e.Name, Description = e.Description, Location = e.Location,
            EventDate = e.EventDate, EventType = e.EventType, DistanceKm = e.DistanceKm, ExistingBannerUrl = e.BannerUrl
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveEvent(EventFormVm vm)
    {
        if (vm.Banner is { Length: > 0 } && !IsAllowedImage(vm.Banner))
            ModelState.AddModelError(nameof(vm.Banner), "Banner must be a JPG, PNG or WEBP image under 5 MB.");
        if (!ModelState.IsValid) return View("EventForm", vm);

        var body = new { name = vm.Name, description = vm.Description ?? "", location = vm.Location, eventDate = vm.EventDate };
        ApiResult<EventDto> saved = vm.EventId == 0
            ? await _api.PostAsync<EventDto>("api/events", body)
            : await _api.PutAsync<EventDto>($"api/events/{vm.EventId}", body);
        if (!saved.Ok || saved.Data is null) { ModelState.AddModelError("", saved.Error); return View("EventForm", vm); }

        var id = saved.Data.EventId;
        var meta = await _api.PutAsync<object>($"api/events/{id}/meta", new { eventType = vm.EventType, distanceKm = vm.DistanceKm });
        if (!meta.Ok) { ModelState.AddModelError("", meta.Error); return View("EventForm", vm); }

        if (vm.Banner is { Length: > 0 })
        {
            var up = await _api.UploadAsync<EventDto>($"api/events/{id}/banner", vm.Banner);
            if (!up.Ok) TempData["Error"] = "Event saved, but the banner upload failed: " + up.Error;
        }
        _log.LogInformation("Event {EventId} saved", id);
        TempData["Success"] = vm.EventId == 0 ? "Event created." : "Event updated.";
        return RedirectToAction(nameof(Events));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        var res = await _api.DeleteAsync($"api/events/{id}");
        if (res.Ok) TempData["Success"] = "Event deleted."; else TempData["Error"] = res.Error;
        return RedirectToAction(nameof(Events));
    }

    // ---------- Categories ----------
    public async Task<IActionResult> Categories(int eventId)
    {
        var ev = await _api.GetAsync<EventDto>($"api/events/{eventId}/detail");
        if (!ev.Ok || ev.Data is null) { TempData["Error"] = ev.Error; return RedirectToAction(nameof(Events)); }
        var cats = await _api.GetAsync<List<CategoryDto>>($"api/events/{eventId}/categories");
        return View(new CategoriesVm { Event = ev.Data, Categories = cats.Data ?? new() });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddCategory(int eventId, CategoryFormVm vm)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = string.Join(" ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            return RedirectToAction(nameof(Categories), new { eventId });
        }
        var res = await _api.PostAsync<CategoryDto>($"api/events/{eventId}/categories",
            new { name = vm.Name, distanceKm = vm.DistanceKm, price = vm.Price });
        if (res.Ok) TempData["Success"] = "Category added."; else TempData["Error"] = res.Error;
        return RedirectToAction(nameof(Categories), new { eventId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCategory(int eventId, int categoryId)
    {
        var res = await _api.DeleteAsync($"api/categories/{categoryId}");
        if (res.Ok) TempData["Success"] = "Category removed."; else TempData["Error"] = res.Error;
        return RedirectToAction(nameof(Categories), new { eventId });
    }

    // ---------- Enrolments ----------
    public async Task<IActionResult> Enrolments(int eventId)
    {
        var vm = await LoadEntrantsAsync(eventId);
        return vm is null ? RedirectToAction(nameof(Events)) : View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetStatus(int eventId, int enrolmentId, string status)
    {
        var res = await _api.PutAsync<object>($"api/enrolments/{enrolmentId}/status", new { status });
        if (res.Ok) TempData["Success"] = $"Enrolment marked {status}."; else TempData["Error"] = res.Error;
        return RedirectToAction(nameof(Enrolments), new { eventId });
    }

    // ---------- Results ----------
    public async Task<IActionResult> Results(int eventId)
    {
        var vm = await LoadEntrantsAsync(eventId);
        return vm is null ? RedirectToAction(nameof(Events)) : View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveResult(int eventId, int enrolmentId, string finishTime, int position)
    {
        // Validate input here so bad values never reach the API or crash the page.
        if (!TimeSpan.TryParse(finishTime, out var ts) || ts <= TimeSpan.Zero)
            TempData["Error"] = "Enter the finish time as hh:mm:ss (for example 00:48:32).";
        else if (position < 1)
            TempData["Error"] = "Finishing position must be 1 or higher.";
        else
        {
            var res = await _api.PostAsync<object>($"api/enrolments/{enrolmentId}/results",
                new { finishTime = ts.ToString("c"), position });
            if (res.Ok) TempData["Success"] = "Result published."; else TempData["Error"] = res.Error;
        }
        return RedirectToAction(nameof(Results), new { eventId });
    }

    // ---------- helpers ----------
    private async Task<ResultsEntryVm?> LoadEntrantsAsync(int eventId)
    {
        var ev = await _api.GetAsync<EventDto>($"api/events/{eventId}/detail");
        if (!ev.Ok || ev.Data is null) { TempData["Error"] = ev.Error; return null; }
        var ent = await _api.GetAsync<List<EntrantDto>>($"api/events/{eventId}/entrants");
        if (!ent.Ok) TempData["Error"] = ent.Error;
        return new ResultsEntryVm { Event = ev.Data, Entrants = ent.Data ?? new() };
    }

    private static bool IsAllowedImage(IFormFile f)
    {
        var okType = new[] { "image/jpeg", "image/png", "image/webp" }.Contains(f.ContentType);
        return okType && f.Length <= 5 * 1024 * 1024;
    }
}
