using FinTrack.Models;

namespace FinTrack.Services;

public class SummaryDto
{
    public decimal TotalBalance { get; set; }
    public decimal Income { get; set; }
    public decimal Expenses { get; set; }
    public decimal NetResult => Income - Expenses;
    public decimal PreviousIncome { get; set; }
    public decimal PreviousExpenses { get; set; }
    public decimal SavingsRate => Income <= 0 ? 0 : NetResult / Income * 100;
    public decimal IncomeChangePct => PreviousIncome <= 0 ? 0 : (Income - PreviousIncome) / PreviousIncome * 100;
    public decimal ExpenseChangePct => PreviousExpenses <= 0 ? 0 : (Expenses - PreviousExpenses) / PreviousExpenses * 100;
    public decimal NetChangePct
    {
        get
        {
            var previous = PreviousIncome - PreviousExpenses;
            return previous <= 0 ? 0 : (NetResult - previous) / Math.Abs(previous) * 100;
        }
    }

    public int TransactionCount { get; set; }
}

public class MonthlyFlowDto
{
    public string Label { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public decimal Income { get; set; }
    public decimal Expenses { get; set; }
    public decimal Net => Income - Expenses;
    public decimal Balance { get; set; }
}

public class CategoryTotalDto
{
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = "#6366f1";
    public decimal Value { get; set; }
    public int Count { get; set; }
}

public class UpcomingItemDto
{
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string Icon { get; set; } = "bi-calendar-event";
    public string Color { get; set; } = "#6366f1";
    public string Type { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Url { get; set; } = "#";
}

public class BudgetProgressDto
{
    public int Id { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string CategoryIcon { get; set; } = "bi-tag";
    public string Color { get; set; } = "#6366f1";
    public decimal Limit { get; set; }
    public decimal Used { get; set; }
    public decimal Remaining => Limit - Used;
    public decimal Percent => Limit <= 0 ? 0 : Math.Min(Used / Limit * 100, 100);
    public bool IsWarning { get; set; }
    public bool IsExceeded { get; set; }
    public DateTime Period { get; set; }
}

public class GoalProgressDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Icon { get; set; } = "bi-stars";
    public string Color { get; set; } = "#10b981";
    public decimal Target { get; set; }
    public decimal Current { get; set; }
    public decimal Percent => Target <= 0 ? 0 : Math.Min(Current / Target * 100, 100);
    public DateTime Deadline { get; set; }
    public int DaysRemaining => (Deadline.Date - DateTime.Today).Days;
    public bool IsCompleted { get; set; }
}

public class RecentTransactionDto
{
    public int Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string CategoryColor { get; set; } = "#6366f1";
    public string CategoryIcon { get; set; } = "bi-tag";
    public string AccountName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public TransactionType Type { get; set; }
    public TransactionStatus Status { get; set; }
    public DateTime Date { get; set; }
    public string Notes { get; set; } = string.Empty;
}

public class IncomeDashboardDto
{
    public decimal TotalReceived { get; set; }
    public decimal Pending { get; set; }
    public decimal RecurringTotal { get; set; }
    public List<MonthlyFlowDto> MonthlyIncome { get; set; } = new();
    public List<CategoryTotalDto> Sources { get; set; } = new();
    public List<RecentTransactionDto> History { get; set; } = new();
    public List<UpcomingItemDto> Forecast { get; set; } = new();
}

public class ExpenseDashboardDto
{
    public decimal TotalSpent { get; set; }
    public decimal FixedTotal { get; set; }
    public decimal VariableTotal { get; set; }
    public decimal PreviousTotal { get; set; }
    public List<CategoryTotalDto> ByCategory { get; set; } = new();
    public List<MonthlyFlowDto> MonthlyExpenses { get; set; } = new();
    public List<RecentTransactionDto> TopExpenses { get; set; } = new();
    public List<CategoryTotalDto> LargestExpenses { get; set; } = new();
}

public class RadarIndicatorsDto
{
    public decimal Savings { get; set; }
    public decimal BudgetControl { get; set; }
    public decimal GoalProgress { get; set; }
    public decimal InvestmentDiversity { get; set; }
    public decimal CashFlow { get; set; }
    public decimal Consistency { get; set; }
}

public class ChartSeriesDto
{
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = "#6366f1";
    public List<decimal> Data { get; set; } = new();
}

public class ReportDataDto
{
    public ReportKind Kind { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ChartType { get; set; } = "line";
    public List<string> Labels { get; set; } = new();
    public List<ChartSeriesDto> Series { get; set; } = new();
    public List<RecentTransactionDto> Details { get; set; } = new();
    public decimal Total { get; set; }
}

public class AccountMovementDto
{
    public int Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public TransactionType Type { get; set; }
    public DateTime Date { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string CategoryColor { get; set; } = "#6366f1";
}
