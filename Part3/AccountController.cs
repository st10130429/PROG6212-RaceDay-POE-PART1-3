using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public class AccountController : Controller
{
    private readonly ApiClient _api;
    private readonly ILogger<AccountController> _log;
    public AccountController(ApiClient api, ILogger<AccountController> log) { _api = api; _log = log; }

    [HttpGet]
    public IActionResult Login() => HttpContext.Session.GetInt32("UserId") is not null ? RedirectToHome() : View(new LoginVm());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginVm vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var res = await _api.LoginAsync<UserDto>(new { email = vm.Email, password = vm.Password });
        if (!res.Ok || res.Data is null)
        {
            ModelState.AddModelError("", res.Error);
            return View(vm);
        }

        // Keep authenticated state + role in session for every page.
        HttpContext.Session.SetInt32("UserId", res.Data.UserId);
        HttpContext.Session.SetString("Name", res.Data.Name);
        HttpContext.Session.SetString("Role", res.Data.Role);
        _log.LogInformation("User {UserId} logged in as {Role}", res.Data.UserId, res.Data.Role);
        return RedirectToHome();
    }

    [HttpGet]
    public IActionResult Register() => View(new RegisterVm());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterVm vm)
    {
        if (vm.Role != "Organiser" && vm.Role != "Participant")
            ModelState.AddModelError(nameof(vm.Role), "Choose Organiser or Participant.");
        if (!ModelState.IsValid) return View(vm);

        var res = await _api.PostAsync<UserDto>("api/auth/register",
            new { name = vm.Name, email = vm.Email, password = vm.Password, role = vm.Role });
        if (!res.Ok)
        {
            ModelState.AddModelError("", res.Error);
            return View(vm);
        }
        TempData["Success"] = "Account created - please log in.";
        return RedirectToAction(nameof(Login));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _api.PostAsync<bool>("api/auth/logout"); // end the API session too
        HttpContext.Session.Clear();
        TempData["Success"] = "You have been logged out.";
        return RedirectToAction(nameof(Login));
    }

    private IActionResult RedirectToHome() =>
        HttpContext.Session.GetString("Role") == "Organiser"
            ? RedirectToAction("Dashboard", "Organiser")
            : RedirectToAction("Index", "Home");
}
