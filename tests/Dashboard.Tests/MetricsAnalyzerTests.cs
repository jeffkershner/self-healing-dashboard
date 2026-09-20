using Dashboard.Server.Services;
using Dashboard.Shared;

namespace Dashboard.Tests;

public class MetricsAnalyzerTests
{
    private static MetricSample Sample(double cpu, double latency, int requests) =>
        new(DateTimeOffset.UtcNow, cpu, 512, requests, latency);

    [Fact]
    public void Summarize_averages_cpu_and_latency()
    {
        var samples = new[] { Sample(10, 100, 4), Sample(30, 300, 6) };

        var summary = MetricsAnalyzer.Summarize(samples);

        Assert.Equal(2, summary.SampleCount);
        Assert.Equal(20, summary.AvgCpuPercent);
        Assert.Equal(200, summary.AvgLatencyMs);
        Assert.Equal(10, summary.TotalRequests);
        Assert.Equal(5, summary.AvgRequestsPerSample);
    }

    [Fact]
    public void Percentile_returns_median_for_p50()
    {
        var values = new double[] { 5, 1, 4, 2, 3 };

        Assert.Equal(3, MetricsAnalyzer.Percentile(values, 50));
    }

    [Fact]
    public void Percentile_of_empty_set_is_zero()
    {
        Assert.Equal(0, MetricsAnalyzer.Percentile([], 95));
    }
}
