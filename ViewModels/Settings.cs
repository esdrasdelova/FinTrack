using System.ComponentModel.DataAnnotations;

namespace FinTrack.ViewModels;

public class SettingsViewModel
{
    public ProfileFormViewModel Profile { get; set; } = new();
    public PasswordFormViewModel Password { get; set; } = new();
    public Dictionary<string, int> Counters { get; set; } = new();
    public bool IsDemo { get; set; }
    public string MemberSince { get; set; } = string.Empty;
}

public class ProfileFormViewModel
{
    [Required(ErrorMessage = "Informe seu nome.")]
    [StringLength(120)]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe o e-mail.")]
    [EmailAddress(ErrorMessage = "E-mail inválido.")]
    public string Email { get; set; } = string.Empty;

    [StringLength(260)]
    public string? AvatarUrl { get; set; }

    public string Currency { get; set; } = "BRL";
    public string DateFormat { get; set; } = "dd/MM/yyyy";
    public string Theme { get; set; } = "dark";
    public bool EmailNotifications { get; set; } = true;
    public bool InAppNotifications { get; set; } = true;
    public bool WeeklySummary { get; set; } = true;
    public bool SecurityAlerts { get; set; } = true;
}

public class PasswordFormViewModel
{
    [Required(ErrorMessage = "Informe a senha atual.")]
    [DataType(DataType.Password)]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe a nova senha.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "A senha deve ter entre 6 e 100 caracteres.")]
    [DataType(DataType.Password)]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirme a nova senha.")]
    [Compare(nameof(NewPassword), ErrorMessage = "As senhas não coincidem.")]
    [DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = string.Empty;
}
