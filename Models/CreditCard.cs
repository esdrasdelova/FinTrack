using System.ComponentModel.DataAnnotations;

namespace FinTrack.Models;

public class CreditCard : IUserOwnedEntity
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Institution { get; set; }

    [MaxLength(40)]
    public string? Brand { get; set; }

    [MaxLength(4)]
    public string LastFourDigits { get; set; } = "0000";

    public decimal CreditLimit { get; set; }

    [Range(1, 28)]
    public int ClosingDay { get; set; } = 25;

    [Range(1, 28)]
    public int DueDay { get; set; } = 10;

    [MaxLength(9)]
    public string Color { get; set; } = "#8b5cf6";

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<CreditCardInvoice> Invoices { get; set; } = new List<CreditCardInvoice>();
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
