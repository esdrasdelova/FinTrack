using System.ComponentModel.DataAnnotations;

namespace FinTrack.Models;

public class Transaction : IUserOwnedEntity
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    [Required, MaxLength(180)]
    public string Description { get; set; } = string.Empty;

    /// <summary>Sempre positivo; o sinal é determinado por <see cref="Type"/>.</summary>
    [Range(typeof(decimal), "0.01", "79228162514264337593543950335")]
    public decimal Amount { get; set; }

    public TransactionType Type { get; set; }
    public TransactionStatus Status { get; set; } = TransactionStatus.Completed;

    public DateTime Date { get; set; } = DateTime.Today;

    [MaxLength(1000)]
    public string? Notes { get; set; }

    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    public int AccountId { get; set; }
    public Account Account { get; set; } = null!;

    public int? CreditCardId { get; set; }
    public CreditCard? CreditCard { get; set; }

    public int? AttachmentId { get; set; }
    public Attachment? Attachment { get; set; }

    /// <summary>Identificador da recorrência que originou esta transação, quando aplicável.</summary>
    public int? RecurringTransactionId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
