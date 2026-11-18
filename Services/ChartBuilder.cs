using System.Text.Json;
using FinTrack.Services;

namespace FinTrack.Services;

/// <summary>
/// Constrói configurações Chart.js válidas no servidor, mantendo as Views livres de lógica.
/// As cores de texto/grade são aplicadas globalmente pelo módulo charts.js (tema claro/escuro).
/// </summary>
public static class ChartBuilder
{
    public static readonly string[] Palette =
    {
        "#6366f1", "#10b981", "#f43f5e", "#8b5cf6", "#06b6d4", "#f59e0b", "#ec4899", "#14b8a6"
    };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = null,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public static string Line(IEnumerable<string> labels, IEnumerable<ChartSeriesDto> series, bool fill = false)
    {
        var datasets = series.Select(s => new
        {
            label = s.Name,
            data = s.Data.Select(d => (double)d),
            borderColor = s.Color,
            backgroundColor = fill ? HexToRgba(s.Color, 0.16) : s.Color,
            borderWidth = 2.5,
            pointBackgroundColor = s.Color,
            pointBorderColor = "transparent",
            pointRadius = 3,
            pointHoverRadius = 6,
            tension = 0.38,
            fill
        });

        return Serialize(new
        {
            type = "line",
            data = new { labels, datasets },
            options = BaseOptions(true)
        });
    }

    public static string Area(IEnumerable<string> labels, IEnumerable<ChartSeriesDto> series) => Line(labels, series, true);

    public static string Bar(IEnumerable<string> labels, IEnumerable<ChartSeriesDto> series, bool horizontal = false, bool stacked = false)
    {
        var datasets = series.Select((s, index) => new
        {
            label = s.Name,
            data = s.Data.Select(d => (double)d),
            backgroundColor = stacked ? HexToRgba(s.Color, 0.85) : s.Color,
            borderColor = s.Color,
            borderWidth = stacked ? 0 : 1,
            borderRadius = 7,
            borderSkipped = false,
            barPercentage = 0.72,
            categoryPercentage = 0.82,
            yAxisID = stacked ? "y" : null,
            xAxisID = stacked ? "x" : null,
            stack = stacked ? "total" : null
        });

        var x = new Dictionary<string, object> { ["grid"] = new { display = false }, ["stacked"] = stacked };
        var y = new Dictionary<string, object> { ["beginAtZero"] = true, ["grid"] = new { drawOnChartArea = true }, ["stacked"] = stacked };
        if (horizontal)
        {
            x["beginAtZero"] = true;
            y["beginAtZero"] = false;
        }

        return Serialize(new
        {
            type = "bar",
            data = new { labels, datasets },
            options = new
            {
                responsive = true,
                maintainAspectRatio = false,
                indexAxis = horizontal ? "y" : "x",
                animation = Animation,
                interaction = new { mode = "index", intersect = false },
                plugins = PluginOptions(),
                scales = new { x, y }
            }
        });
    }

    public static string Doughnut(IEnumerable<string> labels, IEnumerable<decimal> values, IEnumerable<string>? colors = null)
    {
        var items = values.ToList();
        return Serialize(new
        {
            type = "doughnut",
            data = new
            {
                labels,
                datasets = new[]
                {
                    new
                    {
                        label = "Participação",
                        data = items.Select(v => (double)v),
                        backgroundColor = colors?.ToArray() ?? Palette.Take(items.Count).ToArray(),
                        borderColor = "transparent",
                        borderWidth = 0,
                        hoverOffset = 8,
                        spacing = 3
                    }
                }
            },
            options = new
            {
                responsive = true,
                maintainAspectRatio = false,
                cutout = "68%",
                animation = Animation,
                plugins = new
                {
                    legend = new { position = "right", labels = new { usePointStyle = true, pointStyle = "circle", boxWidth = 8, padding = 14 } },
                    tooltip = TooltipOptions()
                }
            }
        });
    }

    public static string Pie(IEnumerable<string> labels, IEnumerable<decimal> values, IEnumerable<string>? colors = null)
    {
        var items = values.ToList();
        return Serialize(new
        {
            type = "pie",
            data = new
            {
                labels,
                datasets = new[]
                {
                    new
                    {
                        data = items.Select(v => (double)v),
                        backgroundColor = colors?.ToArray() ?? Palette.Take(items.Count).ToArray(),
                        borderColor = "transparent",
                        borderWidth = 0,
                        hoverOffset = 8
                    }
                }
            },
            options = new
            {
                responsive = true,
                maintainAspectRatio = false,
                animation = Animation,
                plugins = new
                {
                    legend = new { position = "right", labels = new { usePointStyle = true, pointStyle = "circle", boxWidth = 8, padding = 14 } },
                    tooltip = TooltipOptions()
                }
            }
        });
    }

    public static string Radar(IEnumerable<string> labels, IEnumerable<decimal> values, string? color = null)
    {
        var tone = color ?? "#6366f1";
        return Serialize(new
        {
            type = "radar",
            data = new
            {
                labels,
                datasets = new[]
                {
                    new
                    {
                        label = "Índice",
                        data = values.Select(v => (double)v),
                        backgroundColor = HexToRgba(tone, 0.22),
                        borderColor = tone,
                        borderWidth = 2,
                        pointBackgroundColor = tone,
                        pointBorderColor = tone,
                        pointRadius = 3
                    }
                }
            },
            options = new
            {
                responsive = true,
                maintainAspectRatio = false,
                animation = Animation,
                plugins = new { legend = new { display = false }, tooltip = TooltipOptions() },
                scales = new
                {
                    r = new
                    {
                        min = 0,
                        max = 100,
                        ticks = new { display = false, stepSize = 25 },
                        grid = new { circular = true },
                        angleLines = new { display = true },
                        pointLabels = new { font = new { size = 11 } }
                    }
                }
            }
        });
    }

    public static string Progress(IEnumerable<string> labels, IEnumerable<decimal> values, IEnumerable<string>? colors = null)
    {
        var items = values.ToList();
        return Serialize(new
        {
            type = "bar",
            data = new
            {
                labels,
                datasets = new[]
                {
                    new
                    {
                        label = "Progresso (%)",
                        data = items.Select(v => (double)v),
                        backgroundColor = colors?.ToArray() ?? Palette.Take(items.Count).ToArray(),
                        borderRadius = 8,
                        borderSkipped = false,
                        barPercentage = 0.55
                    }
                }
            },
            options = new
            {
                responsive = true,
                maintainAspectRatio = false,
                indexAxis = "y",
                animation = Animation,
                plugins = new { legend = new { display = false }, tooltip = TooltipOptions() },
                scales = new
                {
                    x = new { min = 0, max = 100, grid = new { display = false }, ticks = new { maxTicksLimit = 6 } },
                    y = new { grid = new { display = false } }
                }
            }
        });
    }

    private static object BaseOptions(bool withScales) => new
    {
        responsive = true,
        maintainAspectRatio = false,
        animation = Animation,
        interaction = new { mode = "index", intersect = false },
        plugins = PluginOptions(),
        scales = withScales
            ? new
            {
                x = new { grid = new { display = false }, ticks = new { maxRotation = 0, autoSkip = true } },
                y = new { beginAtZero = true, grid = new { drawOnChartArea = true }, ticks = new { maxTicksLimit = 6 } }
            }
            : null
    };

    private static object Animation => new { duration = 750, easing = "easeOutQuart" };

    private static object PluginOptions() => new
    {
        legend = new { position = "top", labels = new { usePointStyle = true, pointStyle = "circle", boxWidth = 8, padding = 16 } },
        tooltip = TooltipOptions()
    };

    private static object TooltipOptions() => new
    {
        enabled = true,
        backgroundColor = "rgba(15, 23, 42, 0.96)",
        titleColor = "#f8fafc",
        bodyColor = "#cbd5e1",
        borderColor = "rgba(148,163,184,0.22)",
        borderWidth = 1,
        padding = 12,
        cornerRadius = 10,
        displayColors = true,
        boxPadding = 5,
        titleFont = new { size = 12, weight = "600" },
        bodyFont = new { size = 12 }
    };

    private static string Serialize(object value) => JsonSerializer.Serialize(value, JsonOptions);

    private static string HexToRgba(string hex, double alpha)
    {
        if (string.IsNullOrWhiteSpace(hex) || hex.Length < 7)
            return hex;
        var r = Convert.ToInt32(hex[1..3], 16);
        var g = Convert.ToInt32(hex[3..5], 16);
        var b = Convert.ToInt32(hex[5..7], 16);
        return $"rgba({r}, {g}, {b}, {alpha.ToString(System.Globalization.CultureInfo.InvariantCulture)})";
    }
}
