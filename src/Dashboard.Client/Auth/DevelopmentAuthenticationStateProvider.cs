using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace Dashboard.Client.Auth;

/// <summary>Always-signed-in provider for local development. Never used when Auth:Mode is AzureAd.</summary>
public sealed class DevelopmentAuthenticationStateProvider : AuthenticationStateProvider
{
    private static readonly AuthenticationState State = new(new ClaimsPrincipal(new ClaimsIdentity(
    [
        new Claim(ClaimTypes.NameIdentifier, "dev-user"),
        new Claim(ClaimTypes.Name, "Local Developer"),
    ], "Development")));

    public override Task<AuthenticationState> GetAuthenticationStateAsync() => Task.FromResult(State);
}
