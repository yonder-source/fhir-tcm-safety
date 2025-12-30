using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using SmartFhirApp.Core;

namespace SmartFhirApp.Web.Services;

public sealed class WebAccessTokenStore : ISmartTokenStore
{
    private readonly IAccessTokenProvider _tokenProvider;

    public WebAccessTokenStore(IAccessTokenProvider tokenProvider)
    {
        _tokenProvider = tokenProvider;
    }

    public async ValueTask<SmartTokenResponse?> GetAsync(CancellationToken cancellationToken = default)
    {
        var result = await _tokenProvider.RequestAccessToken().ConfigureAwait(false);
        if (!result.TryGetToken(out var token))
        {
            return null;
        }

        return new SmartTokenResponse
        {
            AccessToken = token.Value,
            TokenType = "Bearer",
            Scope = token.GrantedScopes is { Count: > 0 } ? string.Join(' ', token.GrantedScopes) : null,
        };
    }

    public ValueTask SaveAsync(SmartTokenResponse token, CancellationToken cancellationToken = default)
    {
        return ValueTask.CompletedTask;
    }

    public ValueTask ClearAsync(CancellationToken cancellationToken = default)
    {
        return ValueTask.CompletedTask;
    }
}
