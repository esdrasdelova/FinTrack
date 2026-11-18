using System.ComponentModel.DataAnnotations;

namespace FinTrack.Models;

public class Account : IUserOwnedEntity
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Institution { get; set; }

    public AccountType Type { get; set; } = AccountType.Checking;

    public decimal InitialBalance { get; set; }
    public decimal CurrentBalance { get; set; }

    [MaxLength(9)]
    public string Color { get; set; } = "#6366f1";

    [MaxLength(60)]
    public string Icon { get; set; } = "bi-wallet2";

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    public ICollection<RecurringTransaction> RecurringTransactions { get; set; } = new List<RecurringTransaction>();
}
