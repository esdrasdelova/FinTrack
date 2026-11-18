using System.Globalization;
using FinTrack.Models;
using FinTrack.Repositories;
using FinTrack.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace FinTrack.Services;

public interface IFinanceService
{
    Task<SummaryDto> GetSummaryAsync(PeriodRange range);
    Task<List<MonthlyFlowDto>> GetMonthlyFlowAsync(PeriodRange range);
    Task<List<CategoryTotalDto>> GetExpensesByCategoryAsync(PeriodRange range);
    Task<List<CategoryTotalDto>> GetIncomeByCategoryAsync(PeriodRange range);
    Task<List<MonthlyFlowDto>> GetNetWorthEvolutionAsync(PeriodRange range);
    Task<List<UpcomingItemDto>> GetUpcomingItemsAsync(int days = 30);
    Task<List<BudgetProgressDto>> GetBudgetProgressAsync();
    Task<List<GoalProgressDto>> GetGoalProgressAsync();
    Task<List<RecentTransactionDto>> GetRecentTransactionsAsync(int take = 8, TransactionType? type = null, PeriodRange? range = null);
    Task<RadarIndicatorsDto> GetRadarIndicatorsAsync(PeriodRange range);
    Task<IncomeDashboardDto> GetIncomeDashboardAsync(PeriodRange range);
    Task<ExpenseDashboardDto> GetExpenseDashboardAsync(PeriodRange range);
    Task<ReportDataDto> GetReportDataAsync(ReportKind kind, PeriodRange range);
    Task<Dictionary<string, decimal>> GetInvestmentDistributionAsync();
}

public class FinanceService : IFinanceService
{
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;
    private readonly IPeriodService _periodService;
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    public FinanceService(IUnitOfWork uow, ICurrentUserService currentUser, IPeriodService periodService)
    {
        _uow = uow;
        _currentUser = currentUser;
        _periodService = periodService;
    }

    private string UserId => _currentUser.UserId;

    private IQueryable<Transaction> ValidTransactions(DateTime start, DateTime end) =>
        _uow.Transactions.Query()
            .Where(t => t.Status != TransactionStatus.Cancelled)
            .Where(t => t.Date >= start.Date && t.Date < end.Date.AddDays(1));

    public async Task<SummaryDto> GetSummaryAsync(PeriodRange range)
    {
        var previous = _periodService.Previous(range);
        var current = await ValidTransactions(range.Start, range.End).ToListAsync();
        var prior = await ValidTransactions(previous.Start, previous.End).ToListAsync();

        var income = current.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount);
        var expenses = current.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount);
        var balance = await _uow.Accounts.Query().Where(a => a.IsActive).SumAsync(a => a.CurrentBalance);

        return new SummaryDto
        {
            TotalBalance = balance,
            Income = income,
            Expenses = expenses,
            PreviousIncome = prior.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount),
            PreviousExpenses = prior.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount),
            TransactionCount = current.Count
        };
    }

    public async Task<List<MonthlyFlowDto>> GetMonthlyFlowAsync(PeriodRange range)
    {
        var transactions = await ValidTransactions(range.Start, range.End).ToListAsync();
        var months = new List<DateTime>();
        var cursor = new DateTime(range.Start.Year, range.Start.Month, 1);
        while (cursor <= range.End)
        {
            months.Add(cursor);
            cursor = cursor.AddMonths(1);
        }

        var result = new List<MonthlyFlowDto>();
        decimal running = await _uow.Accounts.Query().SumAsync(a => a.InitialBalance);

        foreach (var month in months)
        {
            var inMonth = transactions.Where(t => t.Date.Year == month.Year && t.Date.Month == month.Month).ToList();
            var income = inMonth.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount);
            var expenses = inMonth.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount);
            running += income - expenses;
            result.Add(new MonthlyFlowDto
            {
                Label = month.ToString("MMM", PtBr).Replace(".", string.Empty),
                Date = month,
                Income = income,
                Expenses = expenses,
                Balance = running
            });
        }

        return result;
    }

    public async Task<List<CategoryTotalDto>> GetExpensesByCategoryAsync(PeriodRange range)
    {
        var start = range.Start.Date;
        var end = range.End.Date.AddDays(1);
        return await (from t in _uow.Transactions.Query()
                      join c in _uow.Categories.Query() on t.CategoryId equals c.Id
                      where t.Type == TransactionType.Expense && t.Status != TransactionStatus.Cancelled
                            && t.Date >= start && t.Date < end
                      group new { t, c } by new { c.Id, c.Name, c.Color } into g
                      orderby g.Sum(x => x.t.Amount) descending
                      select new CategoryTotalDto
                      {
                          Name = g.Key.Name,
                          Color = g.Key.Color,
                          Value = g.Sum(x => x.t.Amount),
                          Count = g.Count()
                      }).ToListAsync();
    }

    public async Task<List<CategoryTotalDto>> GetIncomeByCategoryAsync(PeriodRange range)
    {
        var start = range.Start.Date;
        var end = range.End.Date.AddDays(1);
        return await (from t in _uow.Transactions.Query()
                      join c in _uow.Categories.Query() on t.CategoryId equals c.Id
                      where t.Type == TransactionType.Income && t.Status != TransactionStatus.Cancelled
                            && t.Date >= start && t.Date < end
                      group new { t, c } by new { c.Id, c.Name, c.Color } into g
                      orderby g.Sum(x => x.t.Amount) descending
                      select new CategoryTotalDto
                      {
                          Name = g.Key.Name,
                          Color = g.Key.Color,
                          Value = g.Sum(x => x.t.Amount),
                          Count = g.Count()
                      }).ToListAsync();
    }

    public async Task<List<MonthlyFlowDto>> GetNetWorthEvolutionAsync(PeriodRange range)
    {
        var cash = await GetMonthlyFlowAsync(range);
        var monthlyInvestment = await _uow.InvestmentTransactions.Query()
            .Where(i => i.Date >= range.Start.Date && i.Date < range.End.Date.AddDays(1))
            .GroupBy(i => new { i.Date.Year, i.Date.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Total = g.Sum(i => i.Type == InvestmentTransactionType.Sell ? -i.Amount : i.Amount) })
            .ToListAsync();

        foreach (var point in cash)
        {
            var inv = monthlyInvestment.FirstOrDefault(i => i.Year == point.Date.Year && i.Month == point.Date.Month);
            point.Balance += inv?.Total ?? 0;
        }

        return cash;
    }

    public async Task<List<UpcomingItemDto>> GetUpcomingItemsAsync(int days = 30)
    {
        var today = DateTime.Today;
        var limit = today.AddDays(days);
        var items = new List<UpcomingItemDto>();

        var invoices = await _uow.CreditCardInvoices.Query()
            .Where(i => i.Status != InvoiceStatus.Paid && i.DueDate >= today && i.DueDate <= limit)
            .Include(i => i.CreditCard)
            .ToListAsync();
        items.AddRange(invoices.Select(i => new UpcomingItemDto
        {
            Title = $"Fatura {i.ReferenceMonth:MM/yyyy}",
            Subtitle = i.CreditCard?.Name ?? "Cartão",
            Date = i.DueDate,
            Icon = "bi-credit-card",
            Color = "#8b5cf6",
            Type = "Fatura",
            Amount = i.TotalAmount - i.PaidAmount,
            Url = "/Cards"
        }));

        var recurring = await _uow.RecurringTransactions.Query()
            .Where(r => r.IsActive && r.NextRunDate >= today && r.NextRunDate <= limit)
            .ToListAsync();
        items.AddRange(recurring.Select(r => new UpcomingItemDto
        {
            Title = r.Description,
            Subtitle = r.Type == TransactionType.Income ? "Receita recorrente" : "Despesa recorrente",
            Date = r.NextRunDate,
            Icon = r.Type == TransactionType.Income ? "bi-arrow-down-left-circle" : "bi-arrow-up-right-circle",
            Color = r.Type == TransactionType.Income ? "#10b981" : "#f43f5e",
            Type = "Recorrente",
            Amount = r.Amount,
            Url = "/Transactions"
        }));

        var pending = await _uow.Transactions.Query()
            .Where(t => t.Status == TransactionStatus.Pending && t.Date >= today && t.Date <= limit)
            .ToListAsync();
        items.AddRange(pending.Select(t => new UpcomingItemDto
        {
            Title = t.Description,
            Subtitle = "Transação pendente",
            Date = t.Date,
            Icon = "bi-hourglass-split",
            Color = "#f59e0b",
            Type = "Pendente",
            Amount = t.Amount,
            Url = "/Transactions"
        }));

        return items.OrderBy(i => i.Date).Take(8).ToList();
    }

    public async Task<List<BudgetProgressDto>> GetBudgetProgressAsync()
    {
        var month = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        var budgets = await _uow.Budgets.Query()
            .Where(b => b.Period <= month)
            .Include(b => b.Category)
            .OrderByDescending(b => b.Period)
            .ToListAsync();

        var result = new List<BudgetProgressDto>();
        foreach (var budget in budgets.GroupBy(b => b.CategoryId).Select(g => g.First()))
        {
            var used = await _uow.Transactions.Query()
                .Where(t => t.CategoryId == budget.CategoryId && t.Type == TransactionType.Expense
                            && t.Status != TransactionStatus.Cancelled
                            && t.Date >= month && t.Date < month.AddMonths(1))
                .SumAsync(t => t.Amount);

            result.Add(new BudgetProgressDto
            {
                Id = budget.Id,
                CategoryName = budget.Category?.Name ?? "Categoria",
                CategoryIcon = budget.Category?.Icon ?? "bi-tag",
                Color = budget.Category?.Color ?? "#6366f1",
                Limit = budget.LimitAmount,
                Used = used,
                IsWarning = used >= budget.LimitAmount * budget.AlertThreshold / 100 && used < budget.LimitAmount,
                IsExceeded = used > budget.LimitAmount,
                Period = budget.Period
            });
        }

        return result.OrderByDescending(b => b.Percent).ToList();
    }

    public async Task<List<GoalProgressDto>> GetGoalProgressAsync()
    {
        var goals = await _uow.Goals.Query().OrderBy(g => g.Deadline).ToListAsync();
        return goals.Select(g => new GoalProgressDto
        {
            Id = g.Id,
            Name = g.Name,
            Icon = g.Icon,
            Color = g.Color,
            Target = g.TargetAmount,
            Current = g.CurrentAmount,
            Deadline = g.Deadline,
            IsCompleted = g.IsCompleted || g.CurrentAmount >= g.TargetAmount
        }).ToList();
    }

    public async Task<List<RecentTransactionDto>> GetRecentTransactionsAsync(int take = 8, TransactionType? type = null, PeriodRange? range = null)
    {
        var query = from t in _uow.Transactions.Query()
                    join c in _uow.Categories.Query() on t.CategoryId equals c.Id
                    join a in _uow.Accounts.Query() on t.AccountId equals a.Id
                    select new RecentTransactionDto
                    {
                        Id = t.Id,
                        Description = t.Description,
                        CategoryName = c.Name,
                        CategoryColor = c.Color,
                        CategoryIcon = c.Icon,
                        AccountName = a.Name,
                        Amount = t.Amount,
                        Type = t.Type,
                        Status = t.Status,
                        Date = t.Date,
                        Notes = t.Notes ?? string.Empty
                    };

        if (type.HasValue)
            query = query.Where(t => t.Type == type.Value);

        if (range is not null)
            query = query.Where(t => t.Date >= range.Start.Date && t.Date < range.End.Date.AddDays(1));

        return await query.OrderByDescending(t => t.Date).ThenByDescending(t => t.Id).Take(take).ToListAsync();
    }

    public async Task<RadarIndicatorsDto> GetRadarIndicatorsAsync(PeriodRange range)
    {
        var summary = await GetSummaryAsync(range);
        var budgets = await GetBudgetProgressAsync();
        var goals = await GetGoalProgressAsync();
        var investments = await _uow.Investments.Query().ToListAsync();
        var flow = await GetMonthlyFlowAsync(range);

        return new RadarIndicatorsDto
        {
            Savings = Math.Min(100, Math.Max(0, summary.SavingsRate * 2)),
            BudgetControl = budgets.Count == 0 ? 50 : budgets.Count(b => !b.IsWarning && !b.IsExceeded) * 100m / budgets.Count,
            GoalProgress = goals.Count == 0 ? 0 : goals.Average(g => g.Percent),
            InvestmentDiversity = investments.Count == 0 ? 0 : investments.Select(i => i.Category).Distinct().Count() * 20m,
            CashFlow = summary.Income <= 0 ? 0 : Math.Min(100, Math.Max(0, summary.NetResult / summary.Income * 200)),
            Consistency = flow.Count == 0 ? 0 : flow.Count(f => f.Net >= 0) * 100m / flow.Count
        };
    }

    public async Task<IncomeDashboardDto> GetIncomeDashboardAsync(PeriodRange range)
    {
        var summary = await GetSummaryAsync(range);
        var flow = await GetMonthlyFlowAsync(range);
        var pending = await _uow.Transactions.Query()
            .Where(t => t.Type == TransactionType.Income && t.Status == TransactionStatus.Pending
                        && t.Date >= range.Start.Date && t.Date < range.End.Date.AddDays(1))
            .SumAsync(t => t.Amount);
        var recurring = await _uow.RecurringTransactions.Query()
            .Where(r => r.IsActive && r.Type == TransactionType.Income)
            .SumAsync(r => r.Amount);
        var recurringItems = await _uow.RecurringTransactions.Query()
            .Where(r => r.IsActive && r.Type == TransactionType.Income)
            .ToListAsync();

        return new IncomeDashboardDto
        {
            TotalReceived = summary.Income,
            Pending = pending,
            RecurringTotal = recurring,
            MonthlyIncome = flow,
            Sources = await GetIncomeByCategoryAsync(range),
            History = await GetRecentTransactionsAsync(20, TransactionType.Income),
            Forecast = recurringItems.Select(r => new UpcomingItemDto
            {
                Title = r.Description,
                Subtitle = "Receita recorrente",
                Date = r.NextRunDate,
                Icon = "bi-arrow-repeat",
                Color = "#10b981",
                Type = "Recorrente",
                Amount = r.Amount,
                Url = "/Income"
            }).OrderBy(f => f.Date).Take(6).ToList()
        };
    }

    public async Task<ExpenseDashboardDto> GetExpenseDashboardAsync(PeriodRange range)
    {
        var previous = _periodService.Previous(range);
        var summary = await GetSummaryAsync(range);
        var byCategory = await GetExpensesByCategoryAsync(range);
        var fixedIds = await _uow.Categories.Query().Where(c => c.Type == TransactionType.Expense && c.IsFixed).Select(c => c.Id).ToListAsync();
        var expenses = await ValidTransactions(range.Start, range.End)
            .Where(t => t.Type == TransactionType.Expense)
            .Include(t => t.Category)
            .ToListAsync();

        var fixedTotal = expenses.Where(t => fixedIds.Contains(t.CategoryId)).Sum(t => t.Amount);
        var previousTotal = await ValidTransactions(previous.Start, previous.End)
            .Where(t => t.Type == TransactionType.Expense).SumAsync(t => t.Amount);

        return new ExpenseDashboardDto
        {
            TotalSpent = summary.Expenses,
            FixedTotal = fixedTotal,
            VariableTotal = summary.Expenses - fixedTotal,
            PreviousTotal = previousTotal,
            ByCategory = byCategory,
            MonthlyExpenses = await GetMonthlyFlowAsync(range),
            TopExpenses = await GetRecentTransactionsAsync(8, TransactionType.Expense),
            LargestExpenses = expenses.OrderByDescending(t => t.Amount).Take(6)
                .Select(t => new CategoryTotalDto
                {
                    Name = t.Description,
                    Color = t.Category?.Color ?? "#f43f5e",
                    Value = t.Amount,
                    Count = 1
                }).ToList()
        };
    }

    public async Task<ReportDataDto> GetReportDataAsync(ReportKind kind, PeriodRange range)
    {
        var flow = await GetMonthlyFlowAsync(range);
        var labels = flow.Select(f => f.Label).ToList();
        var dto = new ReportDataDto { Kind = kind, Labels = labels, Title = kind.ToString() };

        switch (kind)
        {
            case ReportKind.IncomeByPeriod:
                dto.ChartType = "bar";
                dto.Title = "Receitas por período";
                dto.Series.Add(new ChartSeriesDto { Name = "Receitas", Color = "#10b981", Data = flow.Select(f => f.Income).ToList() });
                dto.Details = await GetRecentTransactionsAsync(100, TransactionType.Income, range);
                dto.Total = flow.Sum(f => f.Income);
                break;
            case ReportKind.ExpensesByPeriod:
                dto.ChartType = "bar";
                dto.Title = "Despesas por período";
                dto.Series.Add(new ChartSeriesDto { Name = "Despesas", Color = "#f43f5e", Data = flow.Select(f => f.Expenses).ToList() });
                dto.Details = await GetRecentTransactionsAsync(100, TransactionType.Expense, range);
                dto.Total = flow.Sum(f => f.Expenses);
                break;
            case ReportKind.CashFlow:
                dto.Title = "Fluxo de caixa";
                dto.Series.Add(new ChartSeriesDto { Name = "Receitas", Color = "#10b981", Data = flow.Select(f => f.Income).ToList() });
                dto.Series.Add(new ChartSeriesDto { Name = "Despesas", Color = "#f43f5e", Data = flow.Select(f => f.Expenses).ToList() });
                dto.Series.Add(new ChartSeriesDto { Name = "Saldo", Color = "#6366f1", Data = flow.Select(f => f.Net).ToList() });
                dto.Total = flow.Sum(f => f.Net);
                break;
            case ReportKind.MonthlyResult:
                dto.Title = "Resultado mensal";
                dto.Series.Add(new ChartSeriesDto { Name = "Resultado", Color = "#06b6d4", Data = flow.Select(f => f.Net).ToList() });
                dto.Total = flow.Sum(f => f.Net);
                break;
            case ReportKind.ExpensesByCategory:
                dto.ChartType = "doughnut";
                dto.Title = "Gastos por categoria";
                var categories = await GetExpensesByCategoryAsync(range);
                dto.Labels = categories.Select(c => c.Name).ToList();
                dto.Series.Add(new ChartSeriesDto { Name = "Gasto", Color = "#f43f5e", Data = categories.Select(c => c.Value).ToList() });
                dto.Total = categories.Sum(c => c.Value);
                break;
            case ReportKind.NetWorthEvolution:
                dto.Title = "Evolução patrimonial";
                dto.ChartType = "area";
                var evolution = await GetNetWorthEvolutionAsync(range);
                dto.Series.Add(new ChartSeriesDto { Name = "Patrimônio", Color = "#8b5cf6", Data = evolution.Select(e => e.Balance).ToList() });
                dto.Total = evolution.LastOrDefault()?.Balance ?? 0;
                break;
            case ReportKind.YearlyComparison:
                dto.ChartType = "bar";
                dto.Title = "Comparativo anual";
                dto.Series.Add(new ChartSeriesDto { Name = "Receitas", Color = "#10b981", Data = flow.Select(f => f.Income).ToList() });
                dto.Series.Add(new ChartSeriesDto { Name = "Despesas", Color = "#f43f5e", Data = flow.Select(f => f.Expenses).ToList() });
                break;
            case ReportKind.Performance:
                dto.ChartType = "radar";
                dto.Title = "Desempenho financeiro";
                var radar = await GetRadarIndicatorsAsync(range);
                dto.Labels = new List<string> { "Economia", "Orçamento", "Metas", "Investimentos", "Fluxo de caixa", "Consistência" };
                dto.Series.Add(new ChartSeriesDto
                {
                    Name = "Índice",
                    Color = "#6366f1",
                    Data = new List<decimal> { radar.Savings, radar.BudgetControl, radar.GoalProgress, radar.InvestmentDiversity, radar.CashFlow, radar.Consistency }
                });
                break;
        }

        return dto;
    }

    public async Task<Dictionary<string, decimal>> GetInvestmentDistributionAsync()
    {
        return await _uow.Investments.Query()
            .GroupBy(i => i.Category)
            .Select(g => new { Category = g.Key.ToString(), Value = g.Sum(i => i.Quantity * i.CurrentPrice) })
            .ToDictionaryAsync(x => x.Category, x => x.Value);
    }
}
