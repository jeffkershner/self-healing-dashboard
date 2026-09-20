using System.Net.Http.Json;
using Dashboard.Shared;

namespace Dashboard.Client.Services;

/// <summary>Typed wrapper over the dashboard API.</summary>
public sealed class ApiClient(HttpClient http)
{
    public const string Name = "Api";

    public Task<VersionInfo?> GetVersionAsync() => http.GetFromJsonAsync<VersionInfo>("api/version");

    public Task<MetricsSummary?> GetSummaryAsync(int minutes = 5) =>
        http.GetFromJsonAsync<MetricsSummary>($"api/metrics/summary?minutes={minutes}");

    public Task<List<WidgetDefinition>?> GetWidgetsAsync() =>
        http.GetFromJsonAsync<List<WidgetDefinition>>("api/widgets");

    public Task<WidgetValue?> GetWidgetValueAsync(string id) =>
        http.GetFromJsonAsync<WidgetValue>($"api/widgets/{id}/value");

    public Task<List<ChaosScenario>?> GetChaosScenariosAsync() =>
        http.GetFromJsonAsync<List<ChaosScenario>>("api/chaos/scenarios");

    /// <summary>Calls an arbitrary API path and returns the raw status and body, without throwing.</summary>
    public async Task<(int Status, string Body)> CallAsync(string method, string path)
    {
        using var request = new HttpRequestMessage(new HttpMethod(method), path.TrimStart('/'));
        using var response = await http.SendAsync(request);
        return ((int)response.StatusCode, await response.Content.ReadAsStringAsync());
    }
}
