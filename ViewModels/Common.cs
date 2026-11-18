using FinTrack.Models;

namespace FinTrack.ViewModels;

public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = new List<T>();
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public int TotalCount { get; init; }

    public int TotalPages => PageSize <= 0 ? 1 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPrevious => Page > 1;
    public bool HasNext => Page < TotalPages;
    public int FirstItem => TotalCount == 0 ? 0 : (Page - 1) * PageSize + 1;
    public int LastItem => Math.Min(Page * PageSize, TotalCount);

    public IEnumerable<int> PageNumbers(int radius = 2)
    {
        var start = Math.Max(1, Page - radius);
        var end = Math.Min(TotalPages, Page + radius);
        for (var i = start; i <= end; i++)
            yield return i;
    }
}

public class PeriodRange
{
    public PeriodPreset Preset { get; init; } = PeriodPreset.ThisMonth;
    public DateTime Start { get; init; }
    public DateTime End { get; init; }
    public string Label { get; init; } = string.Empty;

    public bool Contains(DateTime date) => date.Date >= Start.Date && date.Date <= End.Date;
}

public class MetricCardViewModel
{
    public string Label { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Icon { get; set; } = "bi-graph-up";
    public string Tone { get; set; } = "accent";
    public string? TrendText { get; set; }
    public string? TrendDirection { get; set; }
    public string? Footer { get; set; }
    public string? ActionUrl { get; set; }
}

public class EmptyStateViewModel
{
    public string Icon { get; set; } = "bi-inbox";
    public string Title { get; set; } = "Nenhum dado encontrado";
    public string Message { get; set; } = string.Empty;
    public string? ActionText { get; set; }
    public string? ActionUrl { get; set; }
}

public class SelectOption
{
    public string Value { get; init; } = string.Empty;
    public string Text { get; init; } = string.Empty;
    public string? Color { get; init; }
    public bool Selected { get; init; }
}

public static class PeriodOptions
{
    public static readonly SelectOption[] All = new[]
    {
        new SelectOption { Value = "Today", Text = "Hoje" },
        new SelectOption { Value = "Last7Days", Text = "Últimos 7 dias" },
        new SelectOption { Value = "Last30Days", Text = "Últimos 30 dias" },
        new SelectOption { Value = "ThisMonth", Text = "Este mês" },
        new SelectOption { Value = "Last6Months", Text = "Últimos 6 meses" },
        new SelectOption { Value = "ThisYear", Text = "Este ano" },
        new SelectOption { Value = "Custom", Text = "Personalizado" }
    };
}
