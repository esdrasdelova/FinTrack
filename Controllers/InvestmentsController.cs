using FinTrack.Services;
using FinTrack.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace FinTrack.Controllers;

public class InvestmentsController : Controller
{
    private readonly IInvestmentService _investments;

    public InvestmentsController(IInvestmentService investments)
    {
        _investments = investments;
    }

    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Investimentos";
        return View(await _investments.GetListAsync());
    }

    public async Task<IActionResult> Details(int id)
    {
        var model = await _investments.GetDetailsAsync(id);
        if (model is null)
            return NotFound();
        ViewData["Title"] = model.Investment.Name;
        return View(model);
    }

    public async Task<IActionResult> Form(int? id)
    {
        var model = id.HasValue ? await _investments.GetFormAsync(id.Value) : await _investments.GetNewFormAsync();
        if (id.HasValue && model is null)
            return NotFound();
        return PartialView("_Form", model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(InvestmentFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).Where(m => !string.IsNullOrEmpty(m));
            return Json(new { success = false, message = "Verifique os campos destacados.", errors });
        }
        var result = await _investments.SaveAsync(model);
        return Json(new { success = result.Success, message = result.Message });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _investments.DeleteAsync(id);
        return Json(new { success = result.Success, message = result.Message });
    }

    public async Task<IActionResult> TransactionForm(int investmentId)
    {
        var model = await _investments.GetTransactionFormAsync(investmentId);
        return model is null ? NotFound() : PartialView("_TransactionForm", model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddTransaction(InvestmentTransactionFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).Where(m => !string.IsNullOrEmpty(m));
            return Json(new { success = false, message = "Verifique os campos destacados.", errors });
        }
        var result = await _investments.AddTransactionAsync(model);
        return Json(new { success = result.Success, message = result.Message });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteTransaction(int id)
    {
        var result = await _investments.DeleteTransactionAsync(id);
        return Json(new { success = result.Success, message = result.Message });
    }
}
