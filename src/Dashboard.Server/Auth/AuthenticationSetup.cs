using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;

namespace Dashboard.Server.Auth;

public static class AuthenticationSetup
{
    public static IServiceCollection AddDashboardAuthentication(
        this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        var mode = configuration["Auth:Mode"] ?? "AzureAd";

        if (string.Equals(mode, "Development", StringComparison.OrdinalIgnoreCase))
        {
            if (!environment.IsDevelopment())
            {
                throw new InvalidOperationException(
                    "Auth:Mode=Development is only allowed when ASPNETCORE_ENVIRONMENT=Development.");
            }

            services.AddAuthentication(DevelopmentAuthHandler.SchemeName)
                .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, DevelopmentAuthHandler>(
                    DevelopmentAuthHandler.SchemeName, _ => { });
            services.AddAuthorization();
            return services;
        }

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddMicrosoftIdentityWebApi(configuration.GetSection("AzureAd"));

        // SignalR over WebSockets cannot send headers, so the client passes the bearer token
        // as ?access_token= on the hub path only.
        services.Configure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
        {
            var inner = options.Events?.OnMessageReceived;
            options.Events ??= new JwtBearerEvents();
            options.Events.OnMessageReceived = async context =>
            {
                if (inner is not null)
                {
                    await inner(context);
                }

                var accessToken = context.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(accessToken) &&
                    context.HttpContext.Request.Path.StartsWithSegments(Dashboard.Shared.HubPaths.Metrics))
                {
                    context.Token = accessToken;
                }
            };
        });

        services.AddAuthorization();
        return services;
    }
}
