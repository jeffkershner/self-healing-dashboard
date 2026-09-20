using Dashboard.Shared;

namespace Dashboard.Server.Services;

/// <summary>Random-walk generator so the dashboard looks alive without a real workload.</summary>
public sealed class MetricsGenerator
{
    private readonly Random _random;
    private double _cpu = 35;
    private double _memory = 512;
    private double _latency = 120;

    public MetricsGenerator(int? seed = null)
    {
        _random = seed is null ? Random.Shared : new Random(seed.Value);
    }

    public MetricSample Next(DateTimeOffset timestamp)
    {
        _cpu = Clamp(_cpu + _random.NextDouble() * 10 - 5, 2, 98);
        _memory = Clamp(_memory + _random.NextDouble() * 20 - 10, 256, 2048);
        _latency = Clamp(_latency + _random.NextDouble() * 30 - 15, 20, 900);
        var requests = (int)Clamp(_cpu / 2 + _random.Next(0, 8), 0, 100);
        return new MetricSample(timestamp, Math.Round(_cpu, 1), Math.Round(_memory, 0), requests, Math.Round(_latency, 1));
    }

    private static double Clamp(double value, double min, double max) => Math.Max(min, Math.Min(max, value));
}
