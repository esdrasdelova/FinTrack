using System.ComponentModel.DataAnnotations;
using FinTrack.Models;

namespace FinTrack.ViewModels;

public class GoalListViewModel
{
    public List<GoalRowViewModel> Items { get; set; } = new();
    public decimal TotalTarget { get; set; }
    public decimal TotalCurrent { get; set; }
    public int CompletedCount { get; set; }
    public string ChartJson { get; set; } = "{}";
}

public class GoalRowViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Icon { get; set; } = "bi-stars";
    public string Color { get; set; } = "#10b981";
    public decimal Target { get; set; }
    public decimal Current { get; set; }
    public DateTime Deadline { get; set; }
    public int DaysRemaining => (Deadline.Date - DateTime.Today).Days;
    public decimal Percent => Target <= 0 ? 0 : Math.Min(Current / Target * 100, 100);
    public bool IsCompleted { get; set; }
    public decimal Missing => Math.Max(0, Target - Current);
    public int ContributionCount { get; set; }
    public DateTime? LastContribution { get; set; }
}

public class GoalFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Informe o nome da meta.")]
    [StringLength(120)]
    public string Name { get; set; } = string.Empty;

    [StringLength(600)]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Informe o valor alvo.")]
    [Range(typeof(decimal), "0.01", "79228162514264337593543950335")]
    public decimal TargetAmount { get; set; }

    [Range(typeof(decimal), "0", "79228162514264337593543950335")]
    public decimal CurrentAmount { get; set; }

    public DateTime Deadline { get; set; } = DateTime.Today.AddMonths(12);
    public string Color { get; set; } = "#10b981";
    public string Icon { get; set; } = "bi-stars";
    public string? ImageUrl { get; set; }
}

public class GoalDetailsViewModel
{
    public GoalRowViewModel Goal { get; set; } = new();
    public List<ContributionRowViewModel> Contributions { get; set; } = new();
    public string EvolutionChartJson { get; set; } = "{}";
}

public class ContributionRowViewModel
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    public string? Notes { get; set; }
}

public class ContributionFormViewModel
{
    public int GoalId { get; set; }

    [Required(ErrorMessage = "Informe o valor do aporte.")]
    [Range(typeof(decimal), "0.01", "79228162514264337593543950335")]
    public decimal Amount { get; set; }

    public DateTime Date { get; set; } = DateTime.Today;

    [StringLength(300)]
    public string? Notes { get; set; }

    public string GoalName { get; set; } = string.Empty;
}
