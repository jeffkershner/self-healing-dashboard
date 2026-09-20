using Dashboard.Server.Auth;
using Dashboard.Server.Endpoints;
using Dashboard.Server.Hubs;
using Dashboard.Server.Services;
using Dashboard.Shared;
using Microsoft.ApplicationInsights.Extensibility;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplicationInsightsTelemetry();
builder.Services.AddSingleton<ITelemetryInitializer, VersionTelemetryInitializer>();

builder.Services.AddDashboardAuthentication(builder.Configuration, builder.Environment);
builder.Services.AddSignalR();
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<MetricsStore>();
builder.Services.AddSingleton(new WidgetCatalog(
    builder.Configuration.GetSection("Widgets").Get<List<WidgetOptions>>() ?? []));
builder.Services.AddHostedService<MetricsPublisher>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
    app.MapOpenApi();
}

// Unhandled exceptions become RFC 7807 responses for the client; Application Insights still
// records them (it listens to the diagnostics events the handler raises).
app.UseExceptionHandler();
app.UseStatusCodePages();

app.UseHttpsRedirection();

// Tells the WASM runtime which environment to load appsettings for (it reads this header
// from the framework script responses).
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/_framework"))
    {
        context.Response.Headers["Blazor-Environment"] = app.Environment.EnvironmentName;
    }
    await next();
});
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

// Serves the client's static web assets from the build manifest, so plain names such as
// _framework/blazor.webassembly.js resolve to the fingerprinted, precompressed files.
app.MapStaticAssets();
app.MapDashboardApi();
app.MapHub<MetricsHub>(HubPaths.Metrics);
app.MapFallbackToFile("index.html");

app.Run();

public partial class Program;
