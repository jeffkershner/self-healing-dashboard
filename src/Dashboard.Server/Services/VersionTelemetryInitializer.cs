using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;

namespace Dashboard.Server.Services;

/// <summary>Stamps every telemetry item with the deployed version so a bug says which release threw.</summary>
public sealed class VersionTelemetryInitializer : ITelemetryInitializer
{
    public void Initialize(ITelemetry telemetry)
    {
        telemetry.Context.Component.Version = AppVersion.Version;
        telemetry.Context.Cloud.RoleName = "dashboard-server";
        telemetry.Context.GlobalProperties.TryAdd("commit", AppVersion.Commit);
    }
}
