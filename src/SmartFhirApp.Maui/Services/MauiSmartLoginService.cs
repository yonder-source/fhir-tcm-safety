using Duende.IdentityModel.Client;
using Duende.IdentityModel.OidcClient;
using SmartFhirApp.Core;

namespace SmartFhirApp.Maui.Services;

public sealed class MauiSmartLoginService
{
    private readonly SmartDiscoveryService _discoveryService;
    private readonly SmartOptions _options;
    private readonly ISmartTokenStore _tokenStore;

    public MauiSmartLoginService(
        SmartDiscoveryService discoveryService,
        SmartOptions options,
        ISmartTokenStore tokenStore)
    {
        _discoveryService = discoveryService;
        _options = options;
        _tokenStore = tokenStore;
    }

    public async Task<SmartTokenResponse> LoginAsync(CancellationToken cancellationToken = default)
    {
        ValidateOptions();

        var issuerBaseUrl = string.IsNullOrWhiteSpace(_options.IssuerBaseUrl)
            ? _options.FhirBaseUrl
            : _options.IssuerBaseUrl;

        var discovery = await _discoveryService
            .GetDiscoveryAsync(_options.FhirBaseUrl, issuerBaseUrl, cancellationToken)
            .ConfigureAwait(false);

        var clientOptions = new OidcClientOptions
        {
            Authority = issuerBaseUrl,
            ClientId = _options.ClientId,
            Scope = _options.Scope,
            RedirectUri = GetRedirectUri(),
            Browser = new MauiOidcBrowser(),
            ProviderInformation = new ProviderInformation
            {
                IssuerName = discovery.Issuer ?? issuerBaseUrl,
                AuthorizeEndpoint = discovery.AuthorizationEndpoint,
                TokenEndpoint = discovery.TokenEndpoint,
            },
        };

        var client = new OidcClient(clientOptions);
        var loginRequest = new LoginRequest
        {
            FrontChannelExtraParameters = new Parameters
            {
                { "aud", _options.FhirBaseUrl }
            }
        };

        var loginResult = await client.LoginAsync(loginRequest, cancellationToken).ConfigureAwait(false);
        if (loginResult.IsError)
        {
            var message = string.IsNullOrWhiteSpace(loginResult.ErrorDescription)
                ? loginResult.Error
                : $"{loginResult.Error}. {loginResult.ErrorDescription}";
            throw new InvalidOperationException($"SMART 登入失敗。{message}");
        }

        var token = new SmartTokenResponse
        {
            AccessToken = loginResult.AccessToken ?? string.Empty,
            TokenType = "Bearer",
            Scope = loginResult.TokenResponse?.Scope,
            IdToken = loginResult.IdentityToken,
            RefreshToken = loginResult.RefreshToken,
        };

        if (loginResult.TokenResponse?.Json is { } json &&
            json.TryGetProperty("patient", out var patientElement) &&
            patientElement.ValueKind == System.Text.Json.JsonValueKind.String)
        {
            token.Patient = patientElement.GetString();
        }

        if (string.IsNullOrWhiteSpace(token.AccessToken))
        {
            throw new InvalidOperationException("Token 回應為空。");
        }

        await _tokenStore.SaveAsync(token, cancellationToken).ConfigureAwait(false);
        return token;
    }

    private string GetRedirectUri()
    {
        if (string.IsNullOrWhiteSpace(_options.RedirectUri))
        {
            throw new InvalidOperationException("Redirect URI 未設定。");
        }

        return _options.RedirectUri;
    }

    private void ValidateOptions()
    {
        if (string.IsNullOrWhiteSpace(_options.FhirBaseUrl))
        {
            throw new InvalidOperationException("FHIR base URL 未設定。");
        }

        if (string.IsNullOrWhiteSpace(_options.ClientId))
        {
            throw new InvalidOperationException("Client ID 未設定。");
        }
    }
}
