using FinTrack.Models;

namespace FinTrack.ViewModels;

public class CalendarViewModel
{
    public int Year { get; set; }
    public int Month { get; set; }
    public string MonthLabel { get; set; } = string.Empty;
    public List<CalendarEventViewModel> Events { get; set; } = new();
    public decimal Income { get; set; }
    public decimal Expenses { get; set; }
}

public class CalendarEventViewModel
{
    public int Day { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string Tone { get; set; } = "accent";
    public string Icon { get; set; } = "bi-circle";
    public string? Url { get; set; }
    public decimal? Amount { get; set; }
    public string Kind { get; set; } = string.Empty;
}
