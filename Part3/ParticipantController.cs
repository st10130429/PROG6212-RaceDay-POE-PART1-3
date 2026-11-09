using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

[RoleRequired("Participant")]
public class ParticipantController : Controller
{
    private readonly ApiClient _api;
    private readonly ILogger<ParticipantController> _log;
    public ParticipantController(ApiClient api, ILogger<ParticipantController> log) { _api = api; _log = log; }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Enrol(int eventId, int? categoryId)
    {
        if (categoryId is null)
        {
            TempData["Error"] = "Please choose a category before enrolling.";
            return RedirectToAction("Details", "Home", new { id = eventId });
        }
        var res = await _api.PostAsync<object>($"api/categories/{categoryId}/enrolments");
        if (res.Ok) TempData["Success"] = "You're enrolled! Your enrolment is now pending confirmation.";
        else TempData["Error"] = res.Error;
        _log.LogInformation("Enrol attempt category {CategoryId}: {Status}", categoryId, res.Status);
        return RedirectToAction(nameof(MyEnrolments));
    }

    public async Task<IActionResult> MyEnrolments()
    {
        var res = await _api.GetAsync<List<MyEnrolmentDto>>("api/enrolments/me/detail");
        if (!res.Ok) TempData["Error"] = res.Error;
        return View(res.Data ?? new());
    }

    public async Task<IActionResult> MyResults()
    {
        var res = await _api.GetAsync<List<MyResultDto>>("api/results/me/detail");
        if (!res.Ok) TempData["Error"] = res.Error;
        return View((res.Data ?? new()).OrderByDescending(r => r.EventDate).ToList());
    }

    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var res = await _api.GetAsync<UserDto>("api/users/me/profile");
        if (!res.Ok || res.Data is null) { TempData["Error"] = res.Error; return RedirectToAction("Index", "Home"); }
        return View(new ProfileVm { Name = res.Data.Name, Email = res.Data.Email, ProfilePictureUrl = res.Data.ProfilePictureUrl });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(ProfileVm vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var upd = await _api.PutAsync<UserDto>("api/users/me", new { name = vm.Name, email = vm.Email });
        if (!upd.Ok) { ModelState.AddModelError("", upd.Error); return View(vm); }
        HttpContext.Session.SetString("Name", vm.Name);

        if (vm.Picture is { Length: > 0 })
        {
            var up = await _api.UploadAsync<UserDto>("api/users/me/picture", vm.Picture);
            if (!up.Ok) { ModelState.AddModelError("", "Profile saved, but the picture couldn't be uploaded: " + up.Error); return View(vm); }
        }
        TempData["Success"] = "Profile updated.";
        return RedirectToAction(nameof(Profile));
    }
}
