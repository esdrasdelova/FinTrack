using FinTrack.Services;

namespace FinTrack.ViewModels;

public class IncomeDashboardViewModel
{
    public PeriodRange Period { get; set; } = new();
    public List<SelectOption> PeriodOptions { get; set; } = new();
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public IncomeDashboardDto Data { get; set; } = new();
    public string MonthlyChartJson { get; set; } = "{}";
    public string SourcesChartJson { get; set; } = "{}";
    public string ForecastChartJson { get; set; } = "{}";
}

public class ExpenseDashboardViewModel
{
    public PeriodRange Period { get; set; } = new();
    public List<SelectOption> PeriodOptions { get; set; } = new();
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public ExpenseDashboardDto Data { get; set; } = new();
    public string MonthlyChartJson { get; set; } = "{}";
    public string CategoryChartJson { get; set; } = "{}";
    public string FixedVariableChartJson { get; set; } = "{}";
    public string LargestChartJson { get; set; } = "{}";
}
