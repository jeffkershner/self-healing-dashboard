using Dashboard.Server.Services;
using Dashboard.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Dashboard.Server.Hubs;

[Authorize]
public sealed class MetricsHub(MetricsStore store) : Hub
{
    public override async Task OnConnectedAsync()
    {
        await Clients.Caller.SendAsync(HubEvents.HistoryReceived, store.Recent(120));
        await base.OnConnectedAsync();
    }

    public MetricsSummary GetSummary(int minutes)
    {
        var cutoff = DateTimeOffset.UtcNow.AddMinutes(-minutes);
        return MetricsAnalyzer.Summarize(store.Since(cutoff));
    }
}
