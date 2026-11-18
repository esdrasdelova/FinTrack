using FinTrack.Models;
using FinTrack.Services;
using FinTrack.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace FinTrack.Controllers;

public class ExpensesController : Controller
{
    private readonly IFinanceService _finance;
    private readonly IPeriodService _periodService;

    public ExpensesController(IFinanceService finance, IPeriodService periodService)
    {
        _finance = finance;
        _periodService = periodService;
    }

    public async Task<IActionResult> Index(PeriodPreset preset = PeriodPreset.ThisMonth, DateTime? from = null, DateTime? to = null)
    {
        var range = _periodService.Resolve(preset, from, to);
        var data = await _finance.GetExpenseDashboardAsync(range);

        var model = new ExpenseDashboardViewModel
        {
            Period = range,
            PeriodOptions = _periodService.PresetOptions(preset),
            From = from,
            To = to,
            Data = data,
            MonthlyChartJson = ChartBuilder.Bar(
                data.MonthlyExpenses.Select(m => m.Label),
                new[] { new ChartSeriesDto { Name = "Despesas", Color = "#f43f5e", Data = data.MonthlyExpenses.Select(m => m.Expenses).ToList() } }),
            CategoryChartJson = ChartBuilder.Doughnut(
                data.ByCategory.Select(c => c.Name),
                data.ByCategory.Select(c => c.Value),
                data.ByCategory.Select(c => c.Color)),
            FixedVariableChartJson = ChartBuilder.Pie(
                new[] { "Fixas", "Variáveis" },
                new[] { data.FixedTotal, data.VariableTotal },
                new[] { "#6366f1", "#06b6d4" }),
            LargestChartJson = ChartBuilder.Bar(
                data.LargestExpenses.Select(e => e.Name),
                new[] { new ChartSeriesDto { Name = "Valor", Color = "#f43f5e", Data = data.LargestExpenses.Select(e => e.Value).ToList() } },
                horizontal: true)
        };

        ViewData["Title"] = "Despesas";
        return View(model);
    }
}
