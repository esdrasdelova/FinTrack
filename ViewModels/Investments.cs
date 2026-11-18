using System.ComponentModel.DataAnnotations;
using FinTrack.Models;

namespace FinTrack.ViewModels;

public class InvestmentListViewModel
{
    public List<InvestmentRowViewModel> Items { get; set; } = new();
    public decimal TotalInvested { get; set; }
    public decimal TotalCurrent { get; set; }
    public decimal TotalProfit { get; set; }
    public decimal TotalDividends { get; set; }
    public string CategoryChartJson { get; set; } = "{}";
    public string EvolutionChartJson { get; set; } = "{}";
}

public class InvestmentRowViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Ticker { get; set; }
    public InvestmentCategory Category { get; set; }
    public string? Institution { get; set; }
    public decimal Quantity { get; set; }
    public decimal AveragePrice { get; set; }
    public decimal CurrentPrice { get; set; }
    public decimal CurrentValue { get; set; }
    public decimal Invested { get; set; }
    public decimal Profit { get; set; }
    public decimal ProfitPercentage { get; set; }
    public string Color { get; set; } = "#06b6d4";
    public int TransactionCount { get; set; }
}

public class InvestmentFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Informe o nome do ativo.")]
    [StringLength(120)]
    public string Name { get; set; } = string.Empty;

    [StringLength(20)]
    public string? Ticker { get; set; }

    public InvestmentCategory Category { get; set; } = InvestmentCategory.FixedIncome;

    [StringLength(100)]
    public string? Institution { get; set; }

    [Range(typeof(decimal), "0", "79228162514264337593543950335", ErrorMessage = "Informe uma quantidade válida.")]
    public decimal Quantity { get; set; } = 1;

    [Range(typeof(decimal), "0", "79228162514264337593543950335")]
    public decimal AveragePrice { get; set; }

    [Range(typeof(decimal), "0", "79228162514264337593543950335")]
    public decimal CurrentPrice { get; set; }

    public DateTime PurchaseDate { get; set; } = DateTime.Today;

    public string Color { get; set; } = "#06b6d4";

    [StringLength(500)]
    public string? Notes { get; set; }
}

public class InvestmentTransactionFormViewModel
{
    public int InvestmentId { get; set; }
    public string InvestmentName { get; set; } = string.Empty;

    public InvestmentTransactionType Type { get; set; } = InvestmentTransactionType.Buy;

    [Range(typeof(decimal), "0.00000001", "79228162514264337593543950335", ErrorMessage = "Informe uma quantidade válida.")]
    public decimal Quantity { get; set; } = 1;

    [Range(typeof(decimal), "0.01", "79228162514264337593543950335", ErrorMessage = "Informe um preço válido.")]
    public decimal Price { get; set; }

    [Range(typeof(decimal), "0", "79228162514264337593543950335")]
    public decimal Amount { get; set; }

    public DateTime Date { get; set; } = DateTime.Today;

    [StringLength(400)]
    public string? Notes { get; set; }
}

public class InvestmentDetailsViewModel
{
    public InvestmentRowViewModel Investment { get; set; } = new();
    public List<InvestmentTransactionRowViewModel> Transactions { get; set; } = new();
    public string EvolutionChartJson { get; set; } = "{}";
}

public class InvestmentTransactionRowViewModel
{
    public int Id { get; set; }
    public InvestmentTransactionType Type { get; set; }
    public decimal Quantity { get; set; }
    public decimal Price { get; set; }
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    public string? Notes { get; set; }
}
