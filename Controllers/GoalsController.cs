using FinTrack.Services;
using FinTrack.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace FinTrack.Controllers;

public class GoalsController : Controller
{
    private readonly IGoalService _goals;

    public GoalsController(IGoalService goals)
    {
        _goals = goals;
    }

    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Metas financeiras";
        return View(await _goals.GetListAsync());
    }

    public async Task<IActionResult> Details(int id)
    {
        var model = await _goals.GetDetailsAsync(id);
        if (model is null)
            return NotFound();
        ViewData["Title"] = model.Goal.Name;
        return View(model);
    }

    public async Task<IActionResult> Form(int? id)
    {
        var model = id.HasValue ? await _goals.GetFormAsync(id.Value) : await _goals.GetNewFormAsync();
        if (id.HasValue && model is null)
            return NotFound();
        return PartialView("_Form", model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(GoalFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).Where(m => !string.IsNullOrEmpty(m));
            return Json(new { success = false, message = "Verifique os campos destacados.", errors });
        }
        var result = await _goals.SaveAsync(model);
        return Json(new { success = result.Success, message = result.Message });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _goals.DeleteAsync(id);
        return Json(new { success = result.Success, message = result.Message });
    }

    public async Task<IActionResult> ContributionForm(int goalId)
    {
        var model = await _goals.GetContributionFormAsync(goalId);
        return model is null ? NotFound() : PartialView("_ContributionForm", model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddContribution(ContributionFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).Where(m => !string.IsNullOrEmpty(m));
            return Json(new { success = false, message = "Verifique os campos destacados.", errors });
        }
        var result = await _goals.AddContributionAsync(model);
        return Json(new { success = result.Success, message = result.Message });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteContribution(int id)
    {
        var result = await _goals.DeleteContributionAsync(id);
        return Json(new { success = result.Success, message = result.Message });
    }
}
