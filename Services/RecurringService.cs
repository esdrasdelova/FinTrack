using FinTrack.Models;
using FinTrack.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FinTrack.Services;

public interface IRecurringService
{
    Task<int> ProcessDueAsync();
    Task<DateTime> NextDateAsync(DateTime from, RecurrenceFrequency frequency);
}

public class RecurringService : IRecurringService
{
    private readonly IUnitOfWork _uow;
    private readonly INotificationService _notifications;

    public RecurringService(IUnitOfWork uow, INotificationService notifications)
    {
        _uow = uow;
        _notifications = notifications;
    }

    public async Task<int> ProcessDueAsync()
    {
        var today = DateTime.Today;
        var due = await _uow.RecurringTransactions.Query(false)
            .Where(r => r.IsActive && r.NextRunDate <= today && (!r.EndDate.HasValue || r.NextRunDate <= r.EndDate.Value))
            .ToListAsync();

        var created = 0;
        foreach (var recurring in due)
        {
            var transaction = new Transaction
            {
                Description = recurring.Description,
                Amount = recurring.Amount,
                Type = recurring.Type,
                Status = TransactionStatus.Completed,
                Date = recurring.NextRunDate.Date,
                CategoryId = recurring.CategoryId,
                AccountId = recurring.AccountId,
                RecurringTransactionId = recurring.Id,
                Notes = "Transação gerada automaticamente por recorrência."
            };
            await _uow.Transactions.AddAsync(transaction);

            recurring.LastGeneratedDate = recurring.NextRunDate;
            recurring.NextRunDate = await NextDateAsync(recurring.NextRunDate, recurring.Frequency);
            if (recurring.EndDate.HasValue && recurring.NextRunDate > recurring.EndDate.Value)
                recurring.IsActive = false;

            created++;
        }

        if (created > 0)
        {
            await _uow.SaveChangesAsync();
            await _notifications.CreateAsync(
                "Transações recorrentes processadas",
                $"{created} transação(ões) recorrente(s) foram lançada(s) automaticamente.",
                NotificationType.Info,
                NotificationCategory.Recurring,
                "/Transactions");
            await _uow.SaveChangesAsync();
        }

        return created;
    }

    public Task<DateTime> NextDateAsync(DateTime from, RecurrenceFrequency frequency)
    {
        var next = frequency switch
        {
            RecurrenceFrequency.Daily => from.AddDays(1),
            RecurrenceFrequency.Weekly => from.AddDays(7),
            RecurrenceFrequency.Monthly => from.AddMonths(1),
            RecurrenceFrequency.Yearly => from.AddYears(1),
            _ => from.AddMonths(1)
        };
        return Task.FromResult(next);
    }
}
