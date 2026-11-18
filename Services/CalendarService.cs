using FinTrack.Extensions;
using FinTrack.Models;
using FinTrack.Repositories;
using FinTrack.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace FinTrack.Services;

public interface ICalendarService
{
    Task<CalendarViewModel> GetMonthAsync(int year, int month);
}

public class CalendarService : ICalendarService
{
    private readonly IUnitOfWork _uow;

    public CalendarService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<CalendarViewModel> GetMonthAsync(int year, int month)
    {
        var start = new DateTime(year, month, 1);
        var end = start.AddMonths(1);
        var events = new List<CalendarEventViewModel>();

        var transactions = await _uow.Transactions.Query()
            .Include(t => t.Category)
            .Where(t => t.Date >= start && t.Date < end)
            .OrderBy(t => t.Date)
            .ToListAsync();
        events.AddRange(transactions.Select(t => new CalendarEventViewModel
        {
            Day = t.Date.Day,
            Title = t.Description,
            Subtitle = t.Category?.Name ?? string.Empty,
            Tone = t.Status == TransactionStatus.Cancelled ? "muted" : t.Type == TransactionType.Income ? "success" : "danger",
            Icon = t.Type == TransactionType.Income ? "bi-arrow-down-left-circle" : "bi-arrow-up-right-circle",
            Amount = t.Amount,
            Url = "/Transactions/Index",
            Kind = "Transação"
        }));

        var recurring = await _uow.RecurringTransactions.Query()
            .Where(r => r.IsActive)
            .ToListAsync();
        foreach (var r in recurring)
        {
            var date = NextOccurrence(r.NextRunDate, start, end, r.Frequency);
            if (date.HasValue)
            {
                events.Add(new CalendarEventViewModel
                {
                    Day = date.Value.Day,
                    Title = r.Description,
                    Subtitle = "Recorrente · " + FrequencyName(r.Frequency),
                    Tone = r.Type == TransactionType.Income ? "success" : "warning",
                    Icon = "bi-arrow-repeat",
                    Amount = r.Amount,
                    Kind = "Recorrência"
                });
            }
        }

        var invoices = await _uow.CreditCardInvoices.Query()
            .Include(i => i.CreditCard)
            .Where(i => i.DueDate >= start && i.DueDate < end && i.Status != InvoiceStatus.Paid)
            .ToListAsync();
        events.AddRange(invoices.Select(i => new CalendarEventViewModel
        {
            Day = i.DueDate.Day,
            Title = "Fatura " + (i.CreditCard?.Name ?? "cartão"),
            Subtitle = "Referência " + i.ReferenceMonth.ToString("MM/yyyy"),
            Tone = "accent",
            Icon = "bi-credit-card",
            Amount = i.TotalAmount - i.PaidAmount,
            Url = "/Cards",
            Kind = "Fatura"
        }));

        var goals = await _uow.Goals.Query().Where(g => !g.IsCompleted && g.Deadline >= start && g.Deadline < end).ToListAsync();
        events.AddRange(goals.Select(g => new CalendarEventViewModel
        {
            Day = g.Deadline.Day,
            Title = "Prazo: " + g.Name,
            Subtitle = "Meta financeira",
            Tone = "info",
            Icon = "bi-trophy",
            Url = "/Goals/Details/" + g.Id,
            Amount = Math.Max(0, g.TargetAmount - g.CurrentAmount),
            Kind = "Meta"
        }));

        return new CalendarViewModel
        {
            Year = year,
            Month = month,
            MonthLabel = new DateTime(year, month, 1).ToString("MMMM 'de' yyyy", CultureInfoPtBr),
            Events = events.OrderBy(e => e.Day).ThenBy(e => e.Title).ToList(),
            Income = transactions.Where(t => t.Type == TransactionType.Income && t.Status != TransactionStatus.Cancelled).Sum(t => t.Amount),
            Expenses = transactions.Where(t => t.Type == TransactionType.Expense && t.Status != TransactionStatus.Cancelled).Sum(t => t.Amount)
        };
    }

    private static readonly System.Globalization.CultureInfo CultureInfoPtBr = System.Globalization.CultureInfo.GetCultureInfo("pt-BR");

    private static DateTime? NextOccurrence(DateTime nextRun, DateTime start, DateTime end, RecurrenceFrequency frequency)
    {
        var date = nextRun;
        var guard = 0;
        while (date < start && guard++ < 800)
            date = frequency switch
            {
                RecurrenceFrequency.Daily => date.AddDays(1),
                RecurrenceFrequency.Weekly => date.AddDays(7),
                RecurrenceFrequency.Yearly => date.AddYears(1),
                _ => date.AddMonths(1)
            };
        return date < end ? date : null;
    }

    private static string FrequencyName(RecurrenceFrequency frequency) => frequency switch
    {
        RecurrenceFrequency.Daily => "diária",
        RecurrenceFrequency.Weekly => "semanal",
        RecurrenceFrequency.Monthly => "mensal",
        _ => "anual"
    };
}
