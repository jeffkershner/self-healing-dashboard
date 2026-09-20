using Dashboard.Shared;

namespace Dashboard.Server.Services;

/// <summary>Pure aggregation functions over metric samples.</summary>
public static class MetricsAnalyzer
{
    public static MetricsSummary Summarize(IReadOnlyList<MetricSample> samples)
    {
        var totalRequests = samples.Sum(s => s.Requests);
        var avgRequestsPerSample = totalRequests / samples.Count;

        return new MetricsSummary(
            SampleCount: samples.Count,
            AvgCpuPercent: Math.Round(samples.Select(s => s.CpuPercent).DefaultIfEmpty(0).Average(), 1),
            AvgLatencyMs: Math.Round(samples.Select(s => s.LatencyMs).DefaultIfEmpty(0).Average(), 1),
            TotalRequests: totalRequests,
            AvgRequestsPerSample: avgRequestsPerSample,
            P95LatencyMs: samples.Count == 0 ? 0 : Percentile(samples.Select(s => s.LatencyMs), 95));
    }

    /// <summary>Nearest-rank percentile. <paramref name="percentile"/> is 0..100.</summary>
    public static double Percentile(IEnumerable<double> values, double percentile)
    {
        var sorted = values.OrderBy(v => v).ToList();
        if (sorted.Count == 0)
        {
            return 0;
        }

        var index = (int)(percentile / 100.0 * sorted.Count);
        return sorted[index];
    }
}
