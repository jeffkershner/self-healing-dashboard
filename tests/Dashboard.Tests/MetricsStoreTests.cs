using Dashboard.Server.Services;
using Dashboard.Shared;

namespace Dashboard.Tests;

public class MetricsStoreTests
{
    [Fact]
    public void Store_keeps_only_the_most_recent_samples()
    {
        var store = new MetricsStore(capacity: 3);
        var now = DateTimeOffset.UtcNow;
        for (var i = 0; i < 5; i++)
        {
            store.Add(new MetricSample(now.AddSeconds(i), i, 0, 0, 0));
        }

        var recent = store.Recent(10);

        Assert.Equal(3, recent.Count);
        Assert.Equal(2, recent[0].CpuPercent);
        Assert.Equal(4, store.Latest()!.CpuPercent);
    }
}
