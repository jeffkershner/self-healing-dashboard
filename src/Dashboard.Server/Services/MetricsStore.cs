using Dashboard.Shared;

namespace Dashboard.Server.Services;

/// <summary>In-memory ring buffer of recent samples. No database on purpose.</summary>
public sealed class MetricsStore
{
    private readonly object _gate = new();
    private readonly Queue<MetricSample> _samples = new();
    private readonly int _capacity;

    public MetricsStore(int capacity = 600)
    {
        _capacity = capacity;
    }

    public void Add(MetricSample sample)
    {
        lock (_gate)
        {
            _samples.Enqueue(sample);
            while (_samples.Count > _capacity)
            {
                _samples.Dequeue();
            }
        }
    }

    public IReadOnlyList<MetricSample> Recent(int count)
    {
        lock (_gate)
        {
            return _samples.TakeLast(Math.Max(0, count)).ToList();
        }
    }

    public IReadOnlyList<MetricSample> Since(DateTimeOffset cutoff)
    {
        lock (_gate)
        {
            return _samples.Where(s => s.Timestamp >= cutoff).ToList();
        }
    }

    public MetricSample? Latest()
    {
        lock (_gate)
        {
            return _samples.Count == 0 ? null : _samples.Last();
        }
    }
}
