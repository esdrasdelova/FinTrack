using FinTrack.Services;
using FinTrack.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace FinTrack.Controllers;

public class SettingsController : Controller
{
    private readonly ISettingsService _settings;
    private readonly ICurrentUserService _currentUser;

    public SettingsController(ISettingsService settings, ICurrentUserService currentUser)
    {
        _settings = settings;
        _currentUser = currentUser;
    }

    public async Task<IActionResult> Index()
    {
        var model = await _settings.GetAsync(_currentUser.UserId);
        if (model is null)
            return Challenge();
        ViewData["Title"] = "Configurações";
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveProfile(ProfileFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).Where(m => !string.IsNullOrEmpty(m));
            return Json(new { success = false, message = "Verifique os campos destacados.", errors });
        }
        var result = await _settings.UpdateProfileAsync(_currentUser.UserId, model);
        return Json(new { success = result.Success, message = result.Message });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(PasswordFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).Where(m => !string.IsNullOrEmpty(m));
            return Json(new { success = false, message = "Verifique os campos destacados.", errors });
        }
        var result = await _settings.ChangePasswordAsync(_currentUser.UserId, model);
        return Json(new { success = result.Success, message = result.Message });
    }
}
