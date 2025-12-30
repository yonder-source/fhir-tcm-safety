using System.Text.Json;
using Duende.IdentityModel;
using Duende.IdentityModel.Client;

namespace SmartFhirApp.Core;

public sealed class SmartAuthService
{
    private const string SessionStorageKey = "smart-auth-session";
    private readonly SmartDiscoveryService _discoveryService;
    private readonly HttpClient _httpClient;
    private readonly IAppStorage _storage;
    private readonly SmartOptions _options;

    public SmartAuthService(
        SmartDiscoveryService discoveryService,
        HttpClient httpClient,
        IAppStorage storage,
        SmartOptions options)
    {
        _discoveryService = discoveryService;
        _httpClient = httpClient;
        _storage = storage;
        _options = options;
    }

    public async Task<string> BuildAuthorizeUrlAsync(string? launchContext = null, CancellationToken cancellationToken = default)
    {
        ValidateOptions(_options);

        var discovery = await _discoveryService.GetDiscoveryAsync(_options.FhirBaseUrl, _options.IssuerBaseUrl, cancellationToken)
            .ConfigureAwait(false);
        var state = Guid.NewGuid().ToString("N");
        var codeVerifier = Pkce.CreateCodeVerifier();
        var codeChallenge = Pkce.CreateCodeChallenge(codeVerifier);

        var session = new SmartAuthSession
        {
            State = state,
            CodeVerifier = codeVerifier,
            RedirectUri = _options.RedirectUri,
        };

        await _storage.SetStringAsync(SessionStorageKey, JsonSerializer.Serialize(session)).ConfigureAwait(false);

        var query = new Dictionary<string, string?>
        {
            ["response_type"] = "code",
            ["client_id"] = _options.ClientId,
            ["redirect_uri"] = _options.RedirectUri,
            ["scope"] = _options.Scope,
            ["state"] = state,
            ["aud"] = _options.FhirBaseUrl,
            ["code_challenge"] = codeChallenge,
            ["code_challenge_method"] = "S256",
        };

        if (!string.IsNullOrWhiteSpace(launchContext))
        {
            query["launch"] = launchContext;
        }

        var url = new RequestUrl(discovery.AuthorizationEndpoint).Create(query!);
        return url;
    }

    public async Task<SmartTokenResponse> ExchangeCodeAsync(string code, string? state, CancellationToken cancellationToken = default)
    {
        ValidateOptions(_options);

        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Authorization code is required.", nameof(code));
        }

        var session = await GetSessionAsync().ConfigureAwait(false);

        if (session is null)
        {
            throw new InvalidOperationException("Missing SMART session. Please start login again.");
        }

        if (!string.IsNullOrWhiteSpace(state) && !string.Equals(state, session.State, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("State mismatch. Please restart login.");
        }

        var discovery = await _discoveryService.GetDiscoveryAsync(_options.FhirBaseUrl, _options.IssuerBaseUrl, cancellationToken)
            .ConfigureAwait(false);

        var tokenResponse = await _httpClient.RequestAuthorizationCodeTokenAsync(
                new AuthorizationCodeTokenRequest
                {
                    Address = discovery.TokenEndpoint,
                    ClientId = _options.ClientId,
                    Code = code,
                    RedirectUri = session.RedirectUri,
                    CodeVerifier = session.CodeVerifier,
                },
                cancellationToken)
            .ConfigureAwait(false);

        if (tokenResponse.IsError)
        {
            var message = string.IsNullOrWhiteSpace(tokenResponse.ErrorDescription)
                ? tokenResponse.Error
                : $"{tokenResponse.Error}. {tokenResponse.ErrorDescription}";
            throw new InvalidOperationException($"Token exchange failed. {message}");
        }

        var token = new SmartTokenResponse
        {
            AccessToken = tokenResponse.AccessToken ?? string.Empty,
            TokenType = tokenResponse.TokenType ?? string.Empty,
            ExpiresIn = tokenResponse.ExpiresIn,
            Scope = tokenResponse.Scope,
            IdToken = tokenResponse.IdentityToken,
            RefreshToken = tokenResponse.RefreshToken,
        };

        if (tokenResponse.Json.TryGetProperty("patient", out var patientElement) &&
            patientElement.ValueKind == JsonValueKind.String)
        {
            token.Patient = patientElement.GetString();
        }

        if (string.IsNullOrWhiteSpace(token.AccessToken))
        {
            throw new InvalidOperationException("Token response is empty.");
        }

        await _storage.RemoveAsync(SessionStorageKey).ConfigureAwait(false);
        return token;
    }

    private static void ValidateOptions(SmartOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.FhirBaseUrl))
        {
            throw new InvalidOperationException("FHIR base URL is not configured.");
        }

        if (string.IsNullOrWhiteSpace(options.ClientId))
        {
            throw new InvalidOperationException("Client ID is not configured.");
        }

        if (string.IsNullOrWhiteSpace(options.RedirectUri))
        {
            throw new InvalidOperationException("Redirect URI is not configured.");
        }
    }

    private async Task<SmartAuthSession?> GetSessionAsync()
    {
        var json = await _storage.GetStringAsync(SessionStorageKey).ConfigureAwait(false);
        return string.IsNullOrWhiteSpace(json) ? null : JsonSerializer.Deserialize<SmartAuthSession>(json);
    }

}
