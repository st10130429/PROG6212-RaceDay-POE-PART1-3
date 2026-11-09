using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public class HomeController : Controller
{
    private readonly ApiClient _api;
    public HomeController(ApiClient api) => _api = api;

    // Participant home / public browse page: upcoming events with filters.
    public async Task<IActionResult> Index(string? search, string? eventType, DateTime? from)
    {
        if (HttpContext.Session.GetString("Role") == "Organiser")
            return RedirectToAction("Dashboard", "Organiser");

        var res = await _api.GetAsync<List<EventDto>>("api/events/browse");
        var vm = new EventFilterVm { Search = search, EventType = eventType, From = from };
        if (!res.Ok) TempData["Error"] = res.Error;

        var q = (res.Data ?? new()).Where(e => e.EventDate >= DateTime.Today.AddDays(-1));
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(e => e.Name.Contains(search, StringComparison.OrdinalIgnoreCase)
                          || e.Location.Contains(search, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(eventType)) q = q.Where(e => e.EventType == eventType);
        if (from.HasValue) q = q.Where(e => e.EventDate >= from.Value);
        vm.Events = q.OrderBy(e => e.EventDate).ToList();
        return View(vm);
    }

    public async Task<IActionResult> Details(int id)
    {
        var ev = await _api.GetAsync<EventDto>($"api/events/{id}/detail");
        if (!ev.Ok || ev.Data is null)
        {
            TempData["Error"] = ev.Error;
            return RedirectToAction(nameof(Index));
        }
        var cats = await _api.GetAsync<List<CategoryDto>>($"api/events/{id}/categories");
        return View(new EventDetailsVm { Event = ev.Data, Categories = cats.Data ?? new() });
    }

    public IActionResult Error() => View();
}
