using Dashboard.Shared;

namespace Dashboard.Server.Services;

/// <summary>
/// Scenarios the Chaos page can run. Each one calls a real endpoint with an input
/// that the current code does not handle. They exist so the monitoring → bug → agent
/// loop can be demonstrated on demand and repeated after each fix ships.
/// </summary>
public static class ChaosCatalog
{
    public static readonly IReadOnlyList<ChaosScenario> Scenarios =
    [
        new("empty-window", "Summary over an empty window",
            "Requests the KPI summary for the last 0 minutes.",
            "GET", "/api/metrics/summary?minutes=0"),
        new("unknown-widget", "Look up a widget that does not exist",
            "Requests the definition of a widget id that is not in the catalog.",
            "GET", "/api/widgets/disk"),
        new("unconfigured-widget", "Render a widget with no config",
            "Renders the value of the uptime widget, which was added without a rendering config.",
            "GET", "/api/widgets/uptime/value"),
        new("p100", "100th percentile latency",
            "Requests the 100th percentile of recent latency.",
            "GET", "/api/metrics/percentile?p=100"),
    ];
}
