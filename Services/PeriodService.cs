using FinTrack.Models;
using FinTrack.ViewModels;

namespace FinTrack.Services;

public interface IPeriodService
{
    PeriodRange Resolve(PeriodPreset preset, DateTime? from = null, DateTime? to = null);
    PeriodRange Previous(PeriodRange range);
    List<SelectOption> PresetOptions(PeriodPreset selected);
}

public class PeriodService : IPeriodService
{
    public PeriodRange Resolve(PeriodPreset preset, DateTime? from = null, DateTime? to = null)
    {
        var today = DateTime.Today;
        var start = preset switch
        {
            PeriodPreset.Today => today,
            PeriodPreset.Last7Days => today.AddDays(-6),
            PeriodPreset.Last30Days => today.AddDays(-29),
            PeriodPreset.ThisMonth => new DateTime(today.Year, today.Month, 1),
            PeriodPreset.Last6Months => new DateTime(today.Year, today.Month, 1).AddMonths(-5),
            PeriodPreset.ThisYear => new DateTime(today.Year, 1, 1),
            PeriodPreset.Custom => from?.Date ?? new DateTime(today.Year, today.Month, 1),
            _ => new DateTime(today.Year, today.Month, 1)
        };

        var end = preset switch
        {
            PeriodPreset.Custom => to?.Date ?? today,
            _ => today
        };

        if (end < start)
            (start, end) = (end, start);

        var label = preset == PeriodPreset.Custom
            ? $"{start:dd/MM/yyyy} — {end:dd/MM/yyyy}"
            : PeriodOptions.All.First(o => o.Value == preset.ToString()).Text;

        return new PeriodRange { Preset = preset, Start = start, End = end, Label = label };
    }

    public PeriodRange Previous(PeriodRange range)
    {
        var days = (range.End.Date - range.Start.Date).Days + 1;
        var end = range.Start.AddDays(-1);
        var start = end.AddDays(-(days - 1));
        return new PeriodRange
        {
            Preset = range.Preset,
            Start = start,
            End = end,
            Label = "Período anterior"
        };
    }

    public List<SelectOption> PresetOptions(PeriodPreset selected) => PeriodOptions.All
        .Select(o => new SelectOption
        {
            Value = o.Value,
            Text = o.Text,
            Selected = o.Value == selected.ToString()
        }).ToList();
}
