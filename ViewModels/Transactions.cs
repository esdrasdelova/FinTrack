using System.ComponentModel.DataAnnotations;
using FinTrack.Models;

namespace FinTrack.ViewModels;

public class TransactionFilterViewModel
{
    public string? Search { get; set; }
    public int? CategoryId { get; set; }
    public int? AccountId { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public decimal? MinAmount { get; set; }
    public decimal? MaxAmount { get; set; }
    public TransactionType? Type { get; set; }
    public TransactionStatus? Status { get; set; }
    public string SortBy { get; set; } = "date";
    public string SortDir { get; set; } = "desc";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 12;
}

public class TransactionFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Informe a descrição.")]
    [StringLength(180, ErrorMessage = "A descrição deve ter até 180 caracteres.")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe o valor.")]
    [Range(typeof(decimal), "0.01", "79228162514264337593543950335", ErrorMessage = "O valor deve ser maior que zero.")]
    public decimal Amount { get; set; }

    public TransactionType Type { get; set; } = TransactionType.Expense;
    public TransactionStatus Status { get; set; } = TransactionStatus.Completed;

    [Required(ErrorMessage = "Informe a data.")]
    public DateTime Date { get; set; } = DateTime.Today;

    [StringLength(1000, ErrorMessage = "As observações devem ter até 1000 caracteres.")]
    public string? Notes { get; set; }

    [Required(ErrorMessage = "Selecione uma categoria.")]
    public int CategoryId { get; set; }

    [Required(ErrorMessage = "Selecione uma conta.")]
    public int AccountId { get; set; }

    public int? CreditCardId { get; set; }
    public IFormFile? Attachment { get; set; }
    public string? ExistingAttachmentName { get; set; }
    public bool RemoveAttachment { get; set; }

    public IEnumerable<SelectOption> Categories { get; set; } = new List<SelectOption>();
    public IEnumerable<SelectOption> Accounts { get; set; } = new List<SelectOption>();
    public IEnumerable<SelectOption> Cards { get; set; } = new List<SelectOption>();
}

public class TransactionRowViewModel
{
    public int Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public TransactionType Type { get; set; }
    public TransactionStatus Status { get; set; }
    public DateTime Date { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string CategoryColor { get; set; } = "#6366f1";
    public string CategoryIcon { get; set; } = "bi-tag";
    public string AccountName { get; set; } = string.Empty;
    public string? CreditCardName { get; set; }
    public string? Notes { get; set; }
    public string? AttachmentName { get; set; }
    public int? AttachmentId { get; set; }
}

public class TransactionListViewModel
{
    public PagedResult<TransactionRowViewModel> Items { get; set; } = new();
    public TransactionFilterViewModel Filter { get; set; } = new();
    public IEnumerable<SelectOption> Categories { get; set; } = new List<SelectOption>();
    public IEnumerable<SelectOption> Accounts { get; set; } = new List<SelectOption>();
    public decimal TotalIncome { get; set; }
    public decimal TotalExpense { get; set; }
    public int TotalRecords { get; set; }
}
