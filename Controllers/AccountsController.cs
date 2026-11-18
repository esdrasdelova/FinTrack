using FinTrack.Services;
using FinTrack.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace FinTrack.Controllers;

public class AccountsController : Controller
{
    private readonly IAccountService _accounts;

    public AccountsController(IAccountService accounts)
    {
        _accounts = accounts;
    }

    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Contas e carteiras";
        return View(await _accounts.GetListAsync());
    }

    public async Task<IActionResult> Details(int id)
    {
        var model = await _accounts.GetDetailsAsync(id);
        if (model is null)
            return NotFound();
        ViewData["Title"] = model.Account.Name;
        return View(model);
    }

    public async Task<IActionResult> Form(int? id)
    {
        var model = id.HasValue ? await _accounts.GetFormAsync(id.Value) : await _accounts.GetNewFormAsync();
        if (id.HasValue && model is null)
            return NotFound();
        return PartialView("_Form", model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(AccountFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).Where(m => !string.IsNullOrEmpty(m));
            return Json(new { success = false, message = "Verifique os campos destacados.", errors });
        }
        var result = await _accounts.SaveAsync(model);
        return Json(new { success = result.Success, message = result.Message });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _accounts.DeleteAsync(id);
        return Json(new { success = result.Success, message = result.Message });
    }
}
