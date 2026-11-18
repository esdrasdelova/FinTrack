using FinTrack.Models;
using FinTrack.Repositories;
using FinTrack.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace FinTrack.Services;

public interface INotificationService
{
    Task<List<Notification>> GetRecentAsync(int take = 6);
    Task<int> GetUnreadCountAsync();
    Task<int> GetTotalCountAsync();
    Task<PagedResult<Notification>> GetPagedAsync(bool? unread, NotificationCategory? category, int page, int pageSize);
    Task CreateAsync(string title, string message, NotificationType type, NotificationCategory category, string? actionUrl = null);
    Task<bool> MarkReadAsync(int id);
    Task<int> MarkAllReadAsync();
    Task<bool> DeleteAsync(int id);
    Task GenerateAlertsAsync();
}

public class NotificationService : INotificationService
{
    private readonly IUnitOfWork _uow;
    private readonly IFinanceService _finance;

    public NotificationService(IUnitOfWork uow, IFinanceService finance)
    {
        _uow = uow;
        _finance = finance;
    }

    public Task<List<Notification>> GetRecentAsync(int take = 6) =>
        _uow.Notifications.Query().OrderByDescending(n => n.CreatedAt).Take(take).ToListAsync();

    public Task<int> GetUnreadCountAsync() => _uow.Notifications.CountAsync(n => !n.IsRead);

    public Task<int> GetTotalCountAsync() => _uow.Notifications.CountAsync();

    public async Task<PagedResult<Notification>> GetPagedAsync(bool? unread, NotificationCategory? category, int page, int pageSize)
    {
        var query = _uow.Notifications.Query();
        if (unread.HasValue)
            query = query.Where(n => n.IsRead == !unread.Value);
        if (category.HasValue)
            query = query.Where(n => n.Category == category.Value);

        var total = await query.CountAsync();
        var items = await query.OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return new PagedResult<Notification>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    public async Task CreateAsync(string title, string message, NotificationType type,
        NotificationCategory category, string? actionUrl = null)
    {
        var today = DateTime.UtcNow.Date;
        var duplicate = await _uow.Notifications.AnyAsync(n =>
            n.Title == title && n.CreatedAt >= today);
        if (duplicate)
            return;

        await _uow.Notifications.AddAsync(new Notification
        {
            Title = title,
            Message = message,
            Type = type,
            Category = category,
            ActionUrl = actionUrl,
            CreatedAt = DateTime.UtcNow
        });
    }

    public async Task<bool> MarkReadAsync(int id)
    {
        var notification = await _uow.Notifications.GetByIdAsync(id);
        if (notification is null)
            return false;
        notification.IsRead = true;
        notification.ReadAt = DateTime.UtcNow;
        await _uow.SaveChangesAsync();
        return true;
    }

    public async Task<int> MarkAllReadAsync()
    {
        var unread = await _uow.Notifications.Query(false).Where(n => !n.IsRead).ToListAsync();
        foreach (var notification in unread)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
        }
        await _uow.SaveChangesAsync();
        return unread.Count;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var notification = await _uow.Notifications.GetByIdAsync(id);
        if (notification is null)
            return false;
        _uow.Notifications.Remove(notification);
        await _uow.SaveChangesAsync();
        return true;
    }

    public async Task GenerateAlertsAsync()
    {
        var today = DateTime.Today;

        var budgets = await _finance.GetBudgetProgressAsync();
        foreach (var budget in budgets.Where(b => b.IsExceeded || b.IsWarning))
        {
            await CreateAsync(
                budget.IsExceeded ? "Orçamento ultrapassado" : "Orçamento próximo do limite",
                $"{budget.CategoryName}: {budget.Percent:0}% do limite utilizado.",
                budget.IsExceeded ? NotificationType.Danger : NotificationType.Warning,
                NotificationCategory.Budget,
                "/Budgets");
        }

        var goals = await _finance.GetGoalProgressAsync();
        foreach (var goal in goals.Where(g => g.IsCompleted || g.DaysRemaining <= 30))
        {
            await CreateAsync(
                goal.IsCompleted ? "Meta financeira alcançada" : "Meta próxima do prazo",
                goal.IsCompleted
                    ? $"Você concluiu a meta \"{goal.Name}\". Parabéns!"
                    : $"A meta \"{goal.Name}\" vence em {Math.Max(0, goal.DaysRemaining)} dias.",
                goal.IsCompleted ? NotificationType.Success : NotificationType.Warning,
                NotificationCategory.Goal,
                "/Goals");
        }

        var invoices = await _uow.CreditCardInvoices.Query()
            .Where(i => i.Status != InvoiceStatus.Paid && i.DueDate >= today && i.DueDate <= today.AddDays(7))
            .Include(i => i.CreditCard)
            .ToListAsync();
        foreach (var invoice in invoices)
        {
            await CreateAsync(
                "Fatura próxima do vencimento",
                $"{invoice.CreditCard?.Name} · vence em {invoice.DueDate:dd/MM/yyyy}.",
                NotificationType.Warning,
                NotificationCategory.Invoice,
                "/Cards");
        }

        var overdue = await _uow.Transactions.CountAsync(t =>
            t.Status == TransactionStatus.Pending && t.Date < today);
        if (overdue > 0)
        {
            await CreateAsync(
                "Transações pendentes",
                $"Existem {overdue} transação(ões) pendente(s) com data vencida.",
                NotificationType.Info,
                NotificationCategory.Alert,
                "/Transactions");
        }

        await _uow.SaveChangesAsync();
    }
}
