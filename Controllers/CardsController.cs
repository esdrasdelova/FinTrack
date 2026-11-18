using FinTrack.Services;
using FinTrack.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace FinTrack.Controllers;

public class CardsController : Controller
{
    private readonly ICardService _cards;

    public CardsController(ICardService cards)
    {
        _cards = cards;
    }

    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Cartões";
        return View(await _cards.GetListAsync());
    }

    public async Task<IActionResult> Form(int? id)
    {
        var model = id.HasValue ? await _cards.GetFormAsync(id.Value) : await _cards.GetNewFormAsync();
        if (id.HasValue && model is null)
            return NotFound();
        return PartialView("_Form", model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(CardFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).Where(m => !string.IsNullOrEmpty(m));
            return Json(new { success = false, message = "Verifique os campos destacados.", errors });
        }
        var result = await _cards.SaveAsync(model);
        return Json(new { success = result.Success, message = result.Message });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _cards.DeleteAsync(id);
        return Json(new { success = result.Success, message = result.Message });
    }

    public async Task<IActionResult> PayForm(int id)
    {
        var model = await _cards.GetListAsync();
        var invoice = model.Invoices.FirstOrDefault(i => i.Id == id);
        if (invoice is null)
            return NotFound();
        return PartialView("_PayForm", new PayInvoiceViewModel
        {
            InvoiceId = id, Amount = invoice.Remaining,
            Accounts = await _cards.GetAccountOptionsAsync()
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> PayInvoice(PayInvoiceViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).Where(m => !string.IsNullOrEmpty(m));
            return Json(new { success = false, message = "Verifique os campos destacados.", errors });
        }
        var result = await _cards.PayInvoiceAsync(model);
        return Json(new { success = result.Success, message = result.Message });
    }
}
