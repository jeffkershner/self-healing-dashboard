using System.Net;
using System.Net.Http.Json;
using Dashboard.Shared;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Dashboard.Tests;

/// <summary>Boots the real server in Development auth mode (fixed local user) and hits the API.</summary>
public class ApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ApiTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.WithWebHostBuilder(b => b.UseEnvironment("Development")).CreateClient();
    }

    [Fact]
    public async Task Version_endpoint_is_anonymous_and_returns_semver()
    {
        var version = await _client.GetFromJsonAsync<VersionInfo>("/api/version");

        Assert.NotNull(version);
        Assert.Matches(@"^\d+\.\d+\.\d+", version.Version);
    }

    [Fact]
    public async Task Widgets_endpoint_lists_widgets()
    {
        var widgets = await _client.GetFromJsonAsync<List<WidgetDefinition>>("/api/widgets");

        Assert.NotNull(widgets);
        Assert.NotEmpty(widgets);
    }

    [Fact]
    public async Task Unknown_widget_id_returns_not_found()
    {
        var response = await _client.GetAsync("/api/widgets/disk");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Unknown_widget_value_returns_not_found()
    {
        var response = await _client.GetAsync("/api/widgets/disk/value");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Chaos_scenarios_are_listed()
    {
        var response = await _client.GetAsync("/api/chaos/scenarios");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
