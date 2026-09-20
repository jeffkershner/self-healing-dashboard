using Dashboard.Client;
using Dashboard.Client.Auth;
using Dashboard.Client.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var baseAddress = new Uri(builder.HostEnvironment.BaseAddress);
var authMode = builder.Configuration["Auth:Mode"] ?? "AzureAd";

if (string.Equals(authMode, "Development", StringComparison.OrdinalIgnoreCase))
{
    // Local-only: a fixed signed-in user and no bearer tokens. The server runs the matching mode.
    builder.Services.AddAuthorizationCore();
    builder.Services.AddScoped<AuthenticationStateProvider, DevelopmentAuthenticationStateProvider>();
    builder.Services.AddScoped<IApiTokenSource, NoTokenSource>();
    builder.Services.AddHttpClient(ApiClient.Name, client => client.BaseAddress = baseAddress);
}
else
{
    builder.Services.AddMsalAuthentication(options =>
    {
        builder.Configuration.Bind("AzureAd", options.ProviderOptions.Authentication);
        var scope = builder.Configuration["AzureAd:ApiScope"];
        if (!string.IsNullOrWhiteSpace(scope))
        {
            options.ProviderOptions.DefaultAccessTokenScopes.Add(scope);
        }
        options.ProviderOptions.LoginMode = "redirect";
    });
    builder.Services.AddScoped<IApiTokenSource, MsalTokenSource>();
    builder.Services.AddHttpClient(ApiClient.Name, client => client.BaseAddress = baseAddress)
        .AddHttpMessageHandler<BaseAddressAuthorizationMessageHandler>();
}

builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient(ApiClient.Name));
builder.Services.AddScoped<ApiClient>();
builder.Services.AddScoped<MetricsHubClient>();

await builder.Build().RunAsync();
