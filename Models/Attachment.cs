using System.ComponentModel.DataAnnotations;

namespace FinTrack.Models;

public class Attachment : IUserOwnedEntity
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    [Required, MaxLength(260)]
    public string FileName { get; set; } = string.Empty;

    [Required, MaxLength(400)]
    public string StoredFileName { get; set; } = string.Empty;

    [MaxLength(120)]
    public string ContentType { get; set; } = "application/octet-stream";

    public long FileSize { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}
