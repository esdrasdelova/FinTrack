using FinTrack.Services;
using FinTrack.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace FinTrack.Controllers;

public class BudgetsController : Controller
{
    private readonly IBudgetService _budgets;

    public BudgetsController(IBudgetService budgets)
    {
        _budgets = budgets;
    }

    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Orçamentos";
        return View(await _budgets.GetListAsync());
    }

    public async Task<IActionResult> Form(int? id)
    {
        var model = id.HasValue ? await _budgets.GetFormAsync(id.Value) : await _budgets.GetNewFormAsync();
        if (id.HasValue && model is null)
            return NotFound();
        return PartialView("_Form", model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(BudgetFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).Where(m => !string.IsNullOrEmpty(m));
            return Json(new { success = false, message = "Verifique os campos destacados.", errors });
        }
        var result = await _budgets.SaveAsync(model);
        return Json(new { success = result.Success, message = result.Message });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _budgets.DeleteAsync(id);
        return Json(new { success = result.Success, message = result.Message });
    }
}
