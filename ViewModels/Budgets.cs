using System.ComponentModel.DataAnnotations;
using FinTrack.Models;

namespace FinTrack.ViewModels;

public class BudgetListViewModel
{
    public List<BudgetRowViewModel> Items { get; set; } = new();
    public decimal TotalLimit { get; set; }
    public decimal TotalUsed { get; set; }
    public int ExceededCount { get; set; }
    public string ChartJson { get; set; } = "{}";
}

public class BudgetRowViewModel
{
    public int Id { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string CategoryIcon { get; set; } = "bi-tag";
    public string Color { get; set; } = "#6366f1";
    public string Period { get; set; } = string.Empty;
    public decimal Limit { get; set; }
    public decimal Used { get; set; }
    public decimal AlertThreshold { get; set; }
    public decimal Percent => Limit <= 0 ? 0 : Used / Limit * 100;
    public bool IsWarning => Percent >= AlertThreshold && Percent <= 100;
    public bool IsExceeded => Percent > 100;
    public decimal Remaining => Limit - Used;
}

public class BudgetFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Selecione uma categoria.")]
    public int CategoryId { get; set; }

    public DateTime Period { get; set; } = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

    [Required(ErrorMessage = "Informe o limite.")]
    [Range(typeof(decimal), "0.01", "79228162514264337593543950335", ErrorMessage = "Informe um limite válido.")]
    public decimal LimitAmount { get; set; }

    [Range(1, 100, ErrorMessage = "O alerta deve estar entre 1 e 100%.")]
    public decimal AlertThreshold { get; set; } = 80;

    public List<SelectOption> Categories { get; set; } = new();
}
