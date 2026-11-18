using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace FinTrack.Models;

public class ApplicationUser : IdentityUser
{
    [MaxLength(120)]
    public string FullName { get; set; } = string.Empty;

    [MaxLength(260)]
    public string? AvatarUrl { get; set; }

    [MaxLength(8)]
    public string Currency { get; set; } = "BRL";

    [MaxLength(20)]
    public string DateFormat { get; set; } = "dd/MM/yyyy";

    [MaxLength(10)]
    public string Theme { get; set; } = "dark";

    public bool EmailNotifications { get; set; } = true;
    public bool InAppNotifications { get; set; } = true;
    public bool WeeklySummary { get; set; } = true;
    public bool SecurityAlerts { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Identifica contas criadas pelo semeador de demonstração.</summary>
    public bool IsDemo { get; set; }

    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    public ICollection<Account> Accounts { get; set; } = new List<Account>();
    public ICollection<Category> Categories { get; set; } = new List<Category>();
    public ICollection<FinancialGoal> Goals { get; set; } = new List<FinancialGoal>();
    public ICollection<Investment> Investments { get; set; } = new List<Investment>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}
