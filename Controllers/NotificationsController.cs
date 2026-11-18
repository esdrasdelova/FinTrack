using FinTrack.Models;
using FinTrack.Services;
using FinTrack.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace FinTrack.Controllers;

public class NotificationsController : Controller
{
    private readonly INotificationService _notifications;

    public NotificationsController(INotificationService notifications)
    {
        _notifications = notifications;
    }

    public async Task<IActionResult> Index(bool? unread, NotificationCategory? category, int page = 1)
    {
        page = Math.Max(1, page);
        var model = new NotificationListViewModel
        {
            Items = await _notifications.GetPagedAsync(unread, category, page, 12),
            Unread = unread,
            Category = category,
            UnreadCount = await _notifications.GetUnreadCountAsync(),
            TotalCount = await _notifications.GetTotalCountAsync()
        };
        ViewData["Title"] = "Notificações";
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Recent()
    {
        var items = await _notifications.GetRecentAsync(6);
        var result = items.Select(n => new Dictionary<string, object>
        {
            ["id"] = n.Id,
            ["title"] = n.Title,
            ["message"] = n.Message,
            ["isRead"] = n.IsRead,
            ["actionUrl"] = n.ActionUrl ?? string.Empty,
            ["typeClass"] = TypeClass(n.Type),
            ["icon"] = Icon(n.Type, n.Category),
            ["timeAgo"] = TimeAgo(n.CreatedAt)
        }).ToList();
        return Json(result);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkRead(int id)
    {
        var success = await _notifications.MarkReadAsync(id);
        return Json(new { success, unreadCount = await _notifications.GetUnreadCountAsync() });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllRead()
    {
        var count = await _notifications.MarkAllReadAsync();
        return Json(new { success = true, count, message = $"{count} notificação(ões) marcada(s) como lida(s)." });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var success = await _notifications.DeleteAsync(id);
        return Json(new { success, message = success ? "Notificação excluída." : "Notificação não encontrada." });
    }

    private static string TypeClass(NotificationType type) => type switch
    {
        NotificationType.Success => "success",
        NotificationType.Warning => "warning",
        NotificationType.Danger => "danger",
        _ => "info"
    };

    private static string Icon(NotificationType type, NotificationCategory category) => category switch
    {
        NotificationCategory.Budget => "bi-pie-chart",
        NotificationCategory.Goal => "bi-trophy",
        NotificationCategory.Invoice => "bi-credit-card",
        NotificationCategory.Transaction => "bi-arrow-left-right",
        _ => type switch
        {
            NotificationType.Success => "bi-check-circle",
            NotificationType.Warning => "bi-exclamation-triangle",
            NotificationType.Danger => "bi-x-octagon",
            _ => "bi-info-circle"
        }
    };

    private static string TimeAgo(DateTime createdAt)
    {
        var diff = DateTime.UtcNow - createdAt;
        if (diff.TotalMinutes < 1) return "agora";
        if (diff.TotalMinutes < 60) return $"há {(int)diff.TotalMinutes} min";
        if (diff.TotalHours < 24) return $"há {(int)diff.TotalHours} h";
        if (diff.TotalDays < 30) return $"há {(int)diff.TotalDays} d";
        return createdAt.ToString("dd/MM/yyyy");
    }
}
