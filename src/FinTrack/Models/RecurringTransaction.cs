using System.ComponentModel.DataAnnotations;

namespace FinTrack.Models;

public class RecurringTransaction : IUserOwnedEntity
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    [Required, MaxLength(180)]
    public string Description { get; set; } = string.Empty;

    public decimal Amount { get; set; }
    public TransactionType Type { get; set; }
    public RecurrenceFrequency Frequency { get; set; } = RecurrenceFrequency.Monthly;

    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    public int AccountId { get; set; }
    public Account Account { get; set; } = null!;

    public DateTime StartDate { get; set; } = DateTime.Today;
    public DateTime? EndDate { get; set; }
    public DateTime NextRunDate { get; set; } = DateTime.Today;
    public DateTime? LastGeneratedDate { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
