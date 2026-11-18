using System.Globalization;
using FinTrack.Models;

namespace FinTrack.Extensions;

/// <summary>Formatadores compartilhados pelas Razor Views (apenas apresentação).</summary>
public static class FinTrackFormat
{
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    public static string Currency(decimal value) => value.ToString("C2", PtBr);
    public static string Number(decimal value) => value.ToString("N2", PtBr);
    public static string Percent(decimal value) => value.ToString("N1", PtBr) + "%";
    public static string Date(DateTime value) => value.ToString("dd/MM/yyyy", PtBr);
    public static string DateShort(DateTime value) => value.ToString("dd MMM", PtBr).Replace(".", string.Empty);
    public static string DateTime(DateTime value) => value.ToString("dd/MM/yyyy HH:mm", PtBr);

    public static string TimeAgo(DateTime value)
    {
        var diff = System.DateTime.UtcNow - (value.Kind == DateTimeKind.Utc ? value : System.DateTime.SpecifyKind(value, DateTimeKind.Utc));
        if (diff.TotalMinutes < 1) return "agora";
        if (diff.TotalMinutes < 60) return $"há {(int)diff.TotalMinutes} min";
        if (diff.TotalHours < 24) return $"há {(int)diff.TotalHours} h";
        if (diff.TotalDays < 30) return $"há {(int)diff.TotalDays} d";
        return value.ToString("dd/MM/yyyy", PtBr);
    }

    public static string TypeName(TransactionType type) => type == TransactionType.Income ? "Receita" : "Despesa";
    public static string TypeIcon(TransactionType type) => type == TransactionType.Income ? "bi-arrow-down-left" : "bi-arrow-up-right";
    public static string StatusName(TransactionStatus status) => status switch
    {
        TransactionStatus.Completed => "Concluída",
        TransactionStatus.Pending => "Pendente",
        _ => "Cancelada"
    };
    public static string StatusTone(TransactionStatus status) => status switch
    {
        TransactionStatus.Completed => "success",
        TransactionStatus.Pending => "warning",
        _ => "muted"
    };
    public static string AccountTypeName(AccountType type) => type switch
    {
        AccountType.Checking => "Conta corrente",
        AccountType.Savings => "Conta poupança",
        AccountType.Wallet => "Carteira física",
        AccountType.Digital => "Conta digital",
        _ => "Investimentos"
    };
    public static string FrequencyName(RecurrenceFrequency frequency) => frequency switch
    {
        RecurrenceFrequency.Daily => "Diária",
        RecurrenceFrequency.Weekly => "Semanal",
        RecurrenceFrequency.Monthly => "Mensal",
        _ => "Anual"
    };
    public static string InvestmentName(InvestmentCategory category) => category switch
    {
        InvestmentCategory.FixedIncome => "Renda fixa",
        InvestmentCategory.Stocks => "Ações",
        InvestmentCategory.Reits => "Fundos imobiliários",
        InvestmentCategory.Funds => "Fundos de investimento",
        _ => "Criptomoedas"
    };
}
