using FinTrack.Models;
using FinTrack.Services;

namespace FinTrack.ViewModels;

public class ReportViewModel
{
    public ReportKind Kind { get; set; } = ReportKind.CashFlow;
    public PeriodRange Period { get; set; } = new();
    public List<SelectOption> PeriodOptions { get; set; } = new();
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public ReportDataDto Data { get; set; } = new();
    public string ChartJson { get; set; } = "{}";
    public List<SelectOption> ReportOptions { get; set; } = new();
}
