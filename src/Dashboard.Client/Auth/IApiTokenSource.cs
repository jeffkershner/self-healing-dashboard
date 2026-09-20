using Microsoft.AspNetCore.Components.WebAssembly.Authentication;

namespace Dashboard.Client.Auth;

/// <summary>Supplies the bearer token the SignalR connection sends, if any.</summary>
public interface IApiTokenSource
{
    Task<string?> GetTokenAsync();
}

public sealed class NoTokenSource : IApiTokenSource
{
    public Task<string?> GetTokenAsync() => Task.FromResult<string?>(null);
}

public sealed class MsalTokenSource(IAccessTokenProvider provider) : IApiTokenSource
{
    public async Task<string?> GetTokenAsync()
    {
        var result = await provider.RequestAccessToken();
        return result.TryGetToken(out var token) ? token.Value : null;
    }
}
