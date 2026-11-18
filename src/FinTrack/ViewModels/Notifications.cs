using FinTrack.Models;

namespace FinTrack.ViewModels;

public class NotificationListViewModel
{
    public PagedResult<Notification> Items { get; set; } = new();
    public bool? Unread { get; set; }
    public NotificationCategory? Category { get; set; }
    public int UnreadCount { get; set; }
    public int TotalCount { get; set; }
}

public class NotificationDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public string? ActionUrl { get; set; }
    public string TypeClass { get; set; } = "info";
    public string Icon { get; set; } = "bi-info-circle";
    public string TimeAgo { get; set; } = string.Empty;
}
