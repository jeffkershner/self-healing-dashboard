using Dashboard.Server.Services;
using Dashboard.Shared;

namespace Dashboard.Tests;

public class WidgetCatalogTests
{
    private static WidgetCatalog Catalog() => new(
    [
        new WidgetOptions { Id = "cpu", Title = "CPU", Unit = "%", Config = new WidgetConfigOptions { Decimals = 1 } },
        new WidgetOptions { Id = "uptime", Title = "Uptime", Unit = "s" },
    ]);

    [Fact]
    public void Known_widget_renders_with_configured_decimals()
    {
        var catalog = Catalog();
        var latest = new MetricSample(DateTimeOffset.UtcNow, 42.345, 512, 7, 99.9);

        var value = catalog.Render("cpu", latest, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);

        Assert.Equal("42.3", value.Display);
        Assert.Equal("%", value.Unit);
    }

    [Fact]
    public void Catalog_lists_all_widgets()
    {
        var catalog = Catalog();

        Assert.Contains(catalog.All, w => w.Id == "cpu");
        Assert.Contains(catalog.All, w => w.Id == "uptime");
    }
}
