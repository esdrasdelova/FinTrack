using System.ComponentModel.DataAnnotations;

namespace FinTrack.Models;

public class CreditCardInvoice : IUserOwnedEntity
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    public int CreditCardId { get; set; }
    public CreditCard CreditCard { get; set; } = null!;

    /// <summary>Primeiro dia do mês de referência da fatura.</summary>
    public DateTime ReferenceMonth { get; set; }

    public DateTime DueDate { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Open;
    public DateTime? PaidAt { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
