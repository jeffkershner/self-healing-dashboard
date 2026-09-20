using Dashboard.Server.Services;
using Dashboard.Shared;

namespace Dashboard.Server.Endpoints;

public static class ApiEndpoints
{
    public static IEndpointRouteBuilder MapDashboardApi(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api");

        api.MapGet("/version", (IHostEnvironment env) => AppVersion.Describe(env.EnvironmentName))
            .AllowAnonymous()
            .WithName("GetVersion");

        var metrics = api.MapGroup("/metrics").RequireAuthorization();

        metrics.MapGet("/recent", (MetricsStore store, int count = 120) => store.Recent(count))
            .WithName("GetRecentMetrics");

        metrics.MapGet("/summary", (MetricsStore store, TimeProvider time, int minutes = 5) =>
            {
                var cutoff = time.GetUtcNow().AddMinutes(-minutes);
                return MetricsAnalyzer.Summarize(store.Since(cutoff));
            })
            .WithName("GetMetricsSummary");

        metrics.MapGet("/percentile", (MetricsStore store, double p = 95, int count = 120) =>
            {
                var latencies = store.Recent(count).Select(s => s.LatencyMs);
                return new { Percentile = p, LatencyMs = MetricsAnalyzer.Percentile(latencies, p) };
            })
            .WithName("GetLatencyPercentile");

        var widgets = api.MapGroup("/widgets").RequireAuthorization();

        widgets.MapGet("/", (WidgetCatalog catalog) => catalog.All)
            .WithName("ListWidgets");

        widgets.MapGet("/{id}", (string id, WidgetCatalog catalog) => catalog.Get(id))
            .WithName("GetWidget");

        widgets.MapGet("/{id}/value", (string id, WidgetCatalog catalog, MetricsStore store, TimeProvider time) =>
                catalog.Render(id, store.Latest(), AppVersion.Describe("n/a").StartedAt, time.GetUtcNow()))
            .WithName("GetWidgetValue");

        api.MapGet("/chaos/scenarios", () => ChaosCatalog.Scenarios)
            .RequireAuthorization()
            .WithName("ListChaosScenarios");

        api.MapGet("/me", (System.Security.Claims.ClaimsPrincipal user) => new
            {
                Name = user.Identity?.Name ?? user.FindFirst("preferred_username")?.Value ?? "unknown",
                Claims = user.Claims.Select(c => new { c.Type, c.Value }),
            })
            .RequireAuthorization()
            .WithName("GetMe");

        return app;
    }
}
