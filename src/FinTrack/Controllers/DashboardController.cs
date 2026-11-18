using FinTrack.Models;
using FinTrack.Services;
using FinTrack.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace FinTrack.Controllers;

public class DashboardController : Controller
{
    private readonly IFinanceService _finance;
    private readonly IPeriodService _periodService;
    private readonly IRecurringService _recurring;
    private readonly INotificationService _notifications;

    public DashboardController(IFinanceService finance, IPeriodService periodService,
        IRecurringService recurring, INotificationService notifications)
    {
        _finance = finance;
        _periodService = periodService;
        _recurring = recurring;
        _notifications = notifications;
    }

    public async Task<IActionResult> Index(PeriodPreset preset = PeriodPreset.ThisMonth, DateTime? from = null, DateTime? to = null)
    {
        var range = _periodService.Resolve(preset, from, to);

        // Processamento idempotente: só gera o que está vencido e notificações únicas por dia.
        await _recurring.ProcessDueAsync();
        await _notifications.GenerateAlertsAsync();

        var summary = await _finance.GetSummaryAsync(range);
        var flow = await _finance.GetMonthlyFlowAsync(range);
        var categories = await _finance.GetExpensesByCategoryAsync(range);
        var netWorth = await _finance.GetNetWorthEvolutionAsync(range);
        var radar = await _finance.GetRadarIndicatorsAsync(range);
        var goals = await _finance.GetGoalProgressAsync();

        var model = new DashboardViewModel
        {
            Period = range,
            From = from,
            To = to,
            PeriodOptions = _periodService.PresetOptions(preset),
            Summary = summary,
            Upcoming = await _finance.GetUpcomingItemsAsync(30),
            Budgets = await _finance.GetBudgetProgressAsync(),
            Goals = goals,
            RecentTransactions = await _finance.GetRecentTransactionsAsync(7),
            HasFlowData = flow.Any(f => f.Income > 0 || f.Expenses > 0),
            HasCategoryData = categories.Count > 0,
            HasTransactionData = summary.TransactionCount > 0,
            IncomeExpenseChartJson = ChartBuilder.Bar(
                flow.Select(f => f.Label),
                new[]
                {
                    new ChartSeriesDto { Name = "Receitas", Color = "#10b981", Data = flow.Select(f => f.Income).ToList() },
                    new ChartSeriesDto { Name = "Despesas", Color = "#f43f5e", Data = flow.Select(f => f.Expenses).ToList() }
                }),
            NetWorthChartJson = ChartBuilder.Area(
                netWorth.Select(f => f.Label),
                new[] { new ChartSeriesDto { Name = "Patrimônio", Color = "#8b5cf6", Data = netWorth.Select(f => f.Balance).ToList() } }),
            CategoryChartJson = ChartBuilder.Doughnut(
                categories.Select(c => c.Name),
                categories.Select(c => c.Value),
                categories.Select(c => c.Color)),
            CashFlowChartJson = ChartBuilder.Line(
                flow.Select(f => f.Label),
                new[]
                {
                    new ChartSeriesDto { Name = "Receitas", Color = "#10b981", Data = flow.Select(f => f.Income).ToList() },
                    new ChartSeriesDto { Name = "Despesas", Color = "#f43f5e", Data = flow.Select(f => f.Expenses).ToList() },
                    new ChartSeriesDto { Name = "Resultado", Color = "#06b6d4", Data = flow.Select(f => f.Net).ToList() }
                }),
            RadarChartJson = ChartBuilder.Radar(
                new[] { "Economia", "Orçamento", "Metas", "Investimentos", "Fluxo", "Consistência" },
                new[] { radar.Savings, radar.BudgetControl, radar.GoalProgress, radar.InvestmentDiversity, radar.CashFlow, radar.Consistency }),
            GoalsChartJson = ChartBuilder.Progress(
                goals.Select(g => g.Name),
                goals.Select(g => g.Percent),
                goals.Select(g => g.Color))
        };

        ViewData["Title"] = "Dashboard";
        return View(model);
    }
}
