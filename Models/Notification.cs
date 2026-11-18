using System.ComponentModel.DataAnnotations;

namespace FinTrack.Models;

public class Notification : IUserOwnedEntity
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    [Required, MaxLength(160)]
    public string Title { get; set; } = string.Empty;

    [Required, MaxLength(600)]
    public string Message { get; set; } = string.Empty;

    public NotificationType Type { get; set; } = NotificationType.Info;
    public NotificationCategory Category { get; set; } = NotificationCategory.Alert;

    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(300)]
    public string? ActionUrl { get; set; }
}
