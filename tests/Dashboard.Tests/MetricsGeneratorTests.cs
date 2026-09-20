using Dashboard.Server.Services;

namespace Dashboard.Tests;

public class MetricsGeneratorTests
{
    [Fact]
    public void Generated_samples_stay_within_bounds()
    {
        var generator = new MetricsGenerator(seed: 42);
        var now = DateTimeOffset.UtcNow;

        for (var i = 0; i < 1000; i++)
        {
            var sample = generator.Next(now.AddSeconds(i));
            Assert.InRange(sample.CpuPercent, 0, 100);
            Assert.InRange(sample.MemoryMb, 256, 2048);
            Assert.InRange(sample.LatencyMs, 20, 900);
            Assert.InRange(sample.Requests, 0, 100);
        }
    }
}
