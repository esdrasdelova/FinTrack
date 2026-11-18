using FinTrack.Extensions;
using FinTrack.Models;
using FinTrack.Services;
using FinTrack.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace FinTrack.Controllers;

public class ReportsController : Controller
{
    private readonly IFinanceService _finance;
    private readonly IPeriodService _periodService;
    private readonly IExportService _export;
    private readonly ICurrentUserService _currentUser;

    public ReportsController(IFinanceService finance, IPeriodService periodService, IExportService export, ICurrentUserService currentUser)
    {
        _finance = finance;
        _periodService = periodService;
        _export = export;
        _currentUser = currentUser;
    }

    public async Task<IActionResult> Index(ReportKind kind = ReportKind.CashFlow, PeriodPreset preset = PeriodPreset.ThisYear, DateTime? from = null, DateTime? to = null)
    {
        var range = _periodService.Resolve(preset, from, to);
        var data = await _finance.GetReportDataAsync(kind, range);

        var model = new ReportViewModel
        {
            Kind = kind, Period = range, From = from, To = to, Data = data,
            PeriodOptions = _periodService.PresetOptions(preset),
            ReportOptions = Enum.GetValues<ReportKind>().Select(k => new SelectOption
            {
                Value = k.ToString(), Text = ReportName(k), Selected = k == kind
            }).ToList(),
            ChartJson = BuildChart(data)
        };

        ViewData["Title"] = "Relatórios";
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Export(ReportKind kind, PeriodPreset preset = PeriodPreset.ThisYear, DateTime? from = null, DateTime? to = null, string format = "csv")
    {
        var range = _periodService.Resolve(preset, from, to);
        var data = await _finance.GetReportDataAsync(kind, range);
        var fileName = $"fintrack-{kind.ToString().ToLowerInvariant()}-{DateTime.Today:yyyyMMdd}";

        if (string.Equals(format, "pdf", StringComparison.OrdinalIgnoreCase))
        {
            var pdf = _export.ReportPdf(data, _currentUser.FullName);
            return File(pdf, "application/pdf", fileName + ".pdf");
        }

        var csv = _export.ReportCsv(data);
        return File(csv, "text/csv; charset=utf-8", fileName + ".csv");
    }

    private static string BuildChart(ReportDataDto data)
    {
        return data.ChartType switch
        {
            "bar" => ChartBuilder.Bar(data.Labels, data.Series),
            "doughnut" => ChartBuilder.Doughnut(data.Labels, data.Series.FirstOrDefault()?.Data ?? new List<decimal>()),
            "radar" => ChartBuilder.Radar(data.Labels, data.Series.FirstOrDefault()?.Data ?? new List<decimal>()),
            "area" => ChartBuilder.Area(data.Labels, data.Series),
            _ => ChartBuilder.Line(data.Labels, data.Series)
        };
    }

    private static string ReportName(ReportKind kind) => kind switch
    {
        ReportKind.IncomeByPeriod => "Receitas por período",
        ReportKind.ExpensesByPeriod => "Despesas por período",
        ReportKind.CashFlow => "Fluxo de caixa",
        ReportKind.MonthlyResult => "Resultado mensal",
        ReportKind.ExpensesByCategory => "Gastos por categoria",
        ReportKind.NetWorthEvolution => "Evolução patrimonial",
        ReportKind.YearlyComparison => "Comparativo anual",
        ReportKind.Performance => "Desempenho financeiro",
        _ => kind.ToString()
    };
}
