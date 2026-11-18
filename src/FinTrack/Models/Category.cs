using System.ComponentModel.DataAnnotations;

namespace FinTrack.Models;

public class Category : IUserOwnedEntity
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    [Required, MaxLength(80)]
    public string Name { get; set; } = string.Empty;

    public TransactionType Type { get; set; }

    [MaxLength(9)]
    public string Color { get; set; } = "#10b981";

    [MaxLength(60)]
    public string Icon { get; set; } = "bi-tag";

    /// <summary>Categorias padrão criadas automaticamente para novos usuários.</summary>
    public bool IsDefault { get; set; }

    /// <summary>Distingue despesas fixas de variáveis nos relatórios.</summary>
    public bool IsFixed { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    public ICollection<Budget> Budgets { get; set; } = new List<Budget>();
}
