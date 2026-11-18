using System.ComponentModel.DataAnnotations;
using FinTrack.Models;
using FinTrack.Services;

namespace FinTrack.ViewModels;

public class AccountListViewModel
{
    public List<AccountRowViewModel> Items { get; set; } = new();
    public decimal TotalBalance { get; set; }
    public decimal TotalInitial { get; set; }
    public int ActiveCount { get; set; }
}

public class AccountRowViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Institution { get; set; }
    public AccountType Type { get; set; }
    public decimal InitialBalance { get; set; }
    public decimal CurrentBalance { get; set; }
    public string Color { get; set; } = "#6366f1";
    public string Icon { get; set; } = "bi-wallet2";
    public bool IsActive { get; set; }
    public int TransactionCount { get; set; }
}

public class AccountFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Informe o nome da conta.")]
    [StringLength(100, ErrorMessage = "O nome deve ter até 100 caracteres.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(100, ErrorMessage = "A instituição deve ter até 100 caracteres.")]
    public string? Institution { get; set; }

    public AccountType Type { get; set; } = AccountType.Checking;

    [Range(typeof(decimal), "-79228162514264337593543950335", "79228162514264337593543950335")]
    public decimal InitialBalance { get; set; }

    public string Color { get; set; } = "#6366f1";
    public string Icon { get; set; } = "bi-wallet2";
    public bool IsActive { get; set; } = true;
}

public class AccountDetailsViewModel
{
    public AccountRowViewModel Account { get; set; } = new();
    public decimal Income { get; set; }
    public decimal Expenses { get; set; }
    public List<AccountMovementDto> Movements { get; set; } = new();
    public string BalanceChartJson { get; set; } = "{}";
}
