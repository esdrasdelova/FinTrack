using FinTrack.Models;
using FinTrack.Repositories;
using FinTrack.Services;
using FinTrack.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinTrack.Controllers;

public class TransactionsController : Controller
{
    private readonly ITransactionService _transactions;
    private readonly IExportService _export;
    private readonly IUnitOfWork _uow;
    private readonly IWebHostEnvironment _environment;

    public TransactionsController(ITransactionService transactions, IExportService export,
        IUnitOfWork uow, IWebHostEnvironment environment)
    {
        _transactions = transactions;
        _export = export;
        _uow = uow;
        _environment = environment;
    }

    public async Task<IActionResult> Index(TransactionFilterViewModel filter)
    {
        ViewData["Title"] = "Transações";
        var model = await _transactions.GetListAsync(filter);
        return View(model);
    }

    public async Task<IActionResult> Form(int? id, TransactionType? type)
    {
        var model = id.HasValue
            ? await _transactions.GetFormAsync(id.Value)
            : await _transactions.GetNewFormAsync(type);
        if (id.HasValue && model is null)
            return NotFound();
        return PartialView("_Form", model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(TransactionFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).Where(m => !string.IsNullOrEmpty(m));
            return Json(new { success = false, message = "Verifique os campos destacados.", errors });
        }

        var result = await _transactions.SaveAsync(model);
        return Json(new { success = result.Success, message = result.Message });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _transactions.DeleteAsync(id);
        return Json(new { success = result.Success, message = result.Message });
    }

    [HttpGet]
    public async Task<IActionResult> Export(string format, TransactionFilterViewModel filter)
    {
        var transactions = await _transactions.GetForExportAsync(filter);
        var stamp = DateTime.Today.ToString("yyyyMMdd");

        if (string.Equals(format, "excel", StringComparison.OrdinalIgnoreCase))
        {
            var bytes = _export.TransactionsExcel(transactions);
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"fintrack-transacoes-{stamp}.xlsx");
        }

        var csv = _export.TransactionsCsv(transactions);
        return File(csv, "text/csv; charset=utf-8", $"fintrack-transacoes-{stamp}.csv");
    }

    [HttpGet]
    public async Task<IActionResult> DownloadAttachment(int id)
    {
        var transaction = await _uow.Transactions.Query()
            .Include(t => t.Attachment)
            .FirstOrDefaultAsync(t => t.Id == id);
        if (transaction?.Attachment is null)
            return NotFound();

        var path = Path.Combine(_environment.WebRootPath ?? string.Empty, "uploads",
            transaction.UserId, transaction.Attachment.StoredFileName);
        if (!System.IO.File.Exists(path))
            return NotFound();

        var bytes = await System.IO.File.ReadAllBytesAsync(path);
        return File(bytes, transaction.Attachment.ContentType, transaction.Attachment.FileName);
    }
}
