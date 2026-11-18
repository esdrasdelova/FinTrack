using FinTrack.Services;

namespace FinTrack.ViewModels;

public class DashboardViewModel
{
    public PeriodRange Period { get; set; } = new();
    public List<SelectOption> PeriodOptions { get; set; } = new();
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }

    public SummaryDto Summary { get; set; } = new();
    public List<UpcomingItemDto> Upcoming { get; set; } = new();
    public List<BudgetProgressDto> Budgets { get; set; } = new();
    public List<GoalProgressDto> Goals { get; set; } = new();
    public List<RecentTransactionDto> RecentTransactions { get; set; } = new();

    public string IncomeExpenseChartJson { get; set; } = "{}";
    public string NetWorthChartJson { get; set; } = "{}";
    public string CategoryChartJson { get; set; } = "{}";
    public string CashFlowChartJson { get; set; } = "{}";
    public string RadarChartJson { get; set; } = "{}";
    public string GoalsChartJson { get; set; } = "{}";

    public bool HasFlowData { get; set; }
    public bool HasCategoryData { get; set; }
    public bool HasTransactionData { get; set; }
}
