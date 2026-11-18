namespace FinTrack.ViewModels;

public class PeriodBarViewModel
{
    public PeriodRange Period { get; set; } = new();
    public List<SelectOption> Options { get; set; } = new();
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public string Action { get; set; } = "Index";
    public string Controller { get; set; } = string.Empty;
    public Dictionary<string, string> HiddenFields { get; set; } = new();
}
