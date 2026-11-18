using System.ComponentModel.DataAnnotations;
using FinTrack.Models;

namespace FinTrack.ViewModels;

public class CardListViewModel
{
    public List<CardRowViewModel> Cards { get; set; } = new();
    public List<InvoiceRowViewModel> Invoices { get; set; } = new();
    public decimal TotalLimit { get; set; }
    public decimal TotalUsed { get; set; }
    public decimal TotalInvoices { get; set; }
    public string UsageChartJson { get; set; } = "{}";
}

public class CardRowViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Institution { get; set; }
    public string? Brand { get; set; }
    public string LastFourDigits { get; set; } = "0000";
    public decimal CreditLimit { get; set; }
    public decimal Used { get; set; }
    public int ClosingDay { get; set; }
    public int DueDay { get; set; }
    public string Color { get; set; } = "#8b5cf6";
    public bool IsActive { get; set; }
    public decimal Available => CreditLimit - Used;
    public decimal UsagePercent => CreditLimit <= 0 ? 0 : Used / CreditLimit * 100;
}

public class InvoiceRowViewModel
{
    public int Id { get; set; }
    public int CreditCardId { get; set; }
    public string CardName { get; set; } = string.Empty;
    public string CardColor { get; set; } = "#8b5cf6";
    public string Reference { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public InvoiceStatus Status { get; set; }
    public decimal Remaining => TotalAmount - PaidAmount;
    public int DaysToDue => (DueDate.Date - DateTime.Today).Days;
}

public class CardFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Informe o nome do cartão.")]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(100)]
    public string? Institution { get; set; }

    [StringLength(40)]
    public string? Brand { get; set; }

    [Required(ErrorMessage = "Informe os últimos 4 dígitos.")]
    [StringLength(4, MinimumLength = 4, ErrorMessage = "Informe exatamente 4 dígitos.")]
    [RegularExpression(@"^\d{4}$", ErrorMessage = "Use apenas números.")]
    public string LastFourDigits { get; set; } = "0000";

    [Range(typeof(decimal), "1", "79228162514264337593543950335", ErrorMessage = "Informe um limite válido.")]
    public decimal CreditLimit { get; set; }

    [Range(1, 28, ErrorMessage = "O dia de fechamento deve estar entre 1 e 28.")]
    public int ClosingDay { get; set; } = 25;

    [Range(1, 28, ErrorMessage = "O dia de vencimento deve estar entre 1 e 28.")]
    public int DueDay { get; set; } = 10;

    public string Color { get; set; } = "#8b5cf6";
    public bool IsActive { get; set; } = true;
}

public class PayInvoiceViewModel
{
    public int InvoiceId { get; set; }

    [Required(ErrorMessage = "Informe o valor.")]
    [Range(typeof(decimal), "0.01", "79228162514264337593543950335", ErrorMessage = "Informe um valor válido.")]
    public decimal Amount { get; set; }

    public DateTime Date { get; set; } = DateTime.Today;

    [Required(ErrorMessage = "Selecione a conta de débito.")]
    public int? AccountId { get; set; }

    public List<SelectOption> Accounts { get; set; } = new();
}
