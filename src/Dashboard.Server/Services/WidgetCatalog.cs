using Dashboard.Shared;

namespace Dashboard.Server.Services;

/// <summary>Bound from the "Widgets" section of appsettings.json.</summary>
public sealed class WidgetOptions
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string Unit { get; set; } = "";
    public WidgetConfigOptions Config { get; set; } = default!;
}

public sealed class WidgetConfigOptions
{
    public int Decimals { get; set; }
    public string Color { get; set; } = "#2a78d6";
}

/// <summary>Catalog of dashboard widgets and how to render their current value.</summary>
public sealed class WidgetCatalog
{
    private readonly Dictionary<string, WidgetOptions> _widgets;

    public WidgetCatalog(IEnumerable<WidgetOptions> widgets)
    {
        _widgets = widgets.ToDictionary(w => w.Id, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyCollection<WidgetDefinition> All => _widgets.Values.Select(ToDefinition).ToList();

    public WidgetDefinition Get(string id) => ToDefinition(_widgets[id]);

    public WidgetValue Render(string id, MetricSample? latest, DateTimeOffset startedAt, DateTimeOffset now)
    {
        var widget = _widgets[id];
        var raw = widget.Id.ToLowerInvariant() switch
        {
            "cpu" => latest?.CpuPercent ?? 0,
            "memory" => latest?.MemoryMb ?? 0,
            "latency" => latest?.LatencyMs ?? 0,
            "requests" => latest?.Requests ?? 0,
            "uptime" => (now - startedAt).TotalSeconds,
            _ => 0,
        };

        var display = raw.ToString($"F{widget.Config.Decimals}");
        return new WidgetValue(widget.Id, widget.Title, display, widget.Unit);
    }

    private static WidgetDefinition ToDefinition(WidgetOptions w) =>
        new(w.Id, w.Title, w.Unit, w.Config is null ? null : new WidgetConfig(w.Config.Decimals, w.Config.Color));
}
