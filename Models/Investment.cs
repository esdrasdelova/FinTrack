using System.ComponentModel.DataAnnotations;

namespace FinTrack.Models;

public class Investment : IUserOwnedEntity
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    [Required, MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? Ticker { get; set; }

    public InvestmentCategory Category { get; set; } = InvestmentCategory.FixedIncome;

    [MaxLength(100)]
    public string? Institution { get; set; }

    public decimal Quantity { get; set; }
    public decimal AveragePrice { get; set; }

    /// <summary>Preço informado manualmente pelo usuário. Não é uma cotação automática.</summary>
    public decimal CurrentPrice { get; set; }

    public DateTime PurchaseDate { get; set; } = DateTime.Today;

    [MaxLength(9)]
    public string Color { get; set; } = "#06b6d4";

    [MaxLength(500)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public decimal CurrentValue => Quantity * CurrentPrice;
    public decimal InvestedValue => Quantity * AveragePrice;
    public decimal Profit => CurrentValue - InvestedValue;
    public decimal ProfitPercentage => InvestedValue == 0 ? 0 : Profit / InvestedValue * 100;

    public ICollection<InvestmentTransaction> Transactions { get; set; } = new List<InvestmentTransaction>();
}

public class InvestmentTransaction : IUserOwnedEntity
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    public int InvestmentId { get; set; }
    public Investment Investment { get; set; } = null!;

    public InvestmentTransactionType Type { get; set; }
    public decimal Quantity { get; set; }
    public decimal Price { get; set; }
    public decimal Amount { get; set; }
    public DateTime Date { get; set; } = DateTime.Today;

    [MaxLength(400)]
    public string? Notes { get; set; }
}
