using Dashboard.Server.Hubs;
using Dashboard.Shared;
using Microsoft.AspNetCore.SignalR;

namespace Dashboard.Server.Services;

/// <summary>Produces one sample per second and pushes it to every connected dashboard.</summary>
public sealed class MetricsPublisher(
    MetricsStore store,
    IHubContext<MetricsHub> hub,
    TimeProvider timeProvider,
    ILogger<MetricsPublisher> logger) : BackgroundService
{
    private readonly MetricsGenerator _generator = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1), timeProvider);
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                var sample = _generator.Next(timeProvider.GetUtcNow());
                store.Add(sample);
                await hub.Clients.All.SendAsync(HubEvents.MetricReceived, sample, stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Metrics publisher stopped.");
        }
    }
}
