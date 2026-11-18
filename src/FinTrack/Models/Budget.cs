using System.ComponentModel.DataAnnotations;

namespace FinTrack.Models;

public class Budget : IUserOwnedEntity
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    /// <summary>Primeiro dia do mês/período orçado.</summary>
    public DateTime Period { get; set; }

    public decimal LimitAmount { get; set; }

    /// <summary>Percentual (0-100) em que o alerta é disparado.</summary>
    [Range(1, 100)]
    public decimal AlertThreshold { get; set; } = 80;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
