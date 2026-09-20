namespace Dashboard.Shared;

/// <summary>One second of synthetic telemetry produced by the server.</summary>
public sealed record MetricSample(
    DateTimeOffset Timestamp,
    double CpuPercent,
    double MemoryMb,
    int Requests,
    double LatencyMs);

/// <summary>Aggregate view over a time window, shown as the KPI tiles.</summary>
public sealed record MetricsSummary(
    int SampleCount,
    double AvgCpuPercent,
    double AvgLatencyMs,
    int TotalRequests,
    int AvgRequestsPerSample,
    double P95LatencyMs);

public sealed record WidgetConfig(int Decimals, string Color);

public sealed record WidgetDefinition(string Id, string Title, string Unit, WidgetConfig? Config);

public sealed record WidgetValue(string Id, string Title, string Display, string Unit);

public sealed record VersionInfo(string Version, string Commit, string Environment, DateTimeOffset StartedAt);

/// <summary>A scripted way to hit a real endpoint with an input that exposes a defect.</summary>
public sealed record ChaosScenario(string Id, string Title, string Description, string Method, string Path);

public static class HubPaths
{
    public const string Metrics = "/hubs/metrics";
}

public static class HubEvents
{
    public const string MetricReceived = "MetricReceived";
    public const string HistoryReceived = "HistoryReceived";
}
