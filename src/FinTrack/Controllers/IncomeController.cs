using FinTrack.Models;
using FinTrack.Services;
using FinTrack.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace FinTrack.Controllers;

public class IncomeController : Controller
{
    private readonly IFinanceService _finance;
    private readonly IPeriodService _periodService;

    public IncomeController(IFinanceService finance, IPeriodService periodService)
    {
        _finance = finance;
        _periodService = periodService;
    }

    public async Task<IActionResult> Index(PeriodPreset preset = PeriodPreset.ThisMonth, DateTime? from = null, DateTime? to = null)
    {
        var range = _periodService.Resolve(preset, from, to);
        var data = await _finance.GetIncomeDashboardAsync(range);

        var model = new IncomeDashboardViewModel
        {
            Period = range,
            PeriodOptions = _periodService.PresetOptions(preset),
            From = from,
            To = to,
            Data = data,
            MonthlyChartJson = ChartBuilder.Bar(
                data.MonthlyIncome.Select(m => m.Label),
                new[] { new ChartSeriesDto { Name = "Receitas", Color = "#10b981", Data = data.MonthlyIncome.Select(m => m.Income).ToList() } }),
            SourcesChartJson = ChartBuilder.Doughnut(
                data.Sources.Select(s => s.Name),
                data.Sources.Select(s => s.Value),
                data.Sources.Select(s => s.Color)),
            ForecastChartJson = ChartBuilder.Line(
                data.Forecast.Select(f => f.Date.ToString("dd/MM")),
                new[] { new ChartSeriesDto { Name = "Previsto", Color = "#06b6d4", Data = data.Forecast.Select(f => f.Amount).ToList() } })
        };

        ViewData["Title"] = "Receitas";
        return View(model);
    }
}
