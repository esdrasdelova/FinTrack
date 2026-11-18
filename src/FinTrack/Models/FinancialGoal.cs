using System.ComponentModel.DataAnnotations;

namespace FinTrack.Models;

public class FinancialGoal : IUserOwnedEntity
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    [Required, MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(600)]
    public string? Description { get; set; }

    public decimal TargetAmount { get; set; }
    public decimal CurrentAmount { get; set; }
    public DateTime Deadline { get; set; } = DateTime.Today.AddMonths(12);

    [MaxLength(400)]
    public string? ImageUrl { get; set; }

    [MaxLength(9)]
    public string Color { get; set; } = "#10b981";

    [MaxLength(60)]
    public string Icon { get; set; } = "bi-stars";

    public bool IsCompleted { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<GoalContribution> Contributions { get; set; } = new List<GoalContribution>();
}

public class GoalContribution : IUserOwnedEntity
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    public int FinancialGoalId { get; set; }
    public FinancialGoal Goal { get; set; } = null!;

    public decimal Amount { get; set; }
    public DateTime Date { get; set; } = DateTime.Today;

    [MaxLength(300)]
    public string? Notes { get; set; }
}
