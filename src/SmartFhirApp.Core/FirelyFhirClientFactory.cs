using System.Net.Http.Headers;
using Hl7.Fhir.Rest;

namespace SmartFhirApp.Core;

public sealed class FirelyFhirClientFactory : IFhirClientFactory
{
    private readonly SmartOptions _options;
    private readonly ISmartTokenStore _tokenStore;

    public FirelyFhirClientFactory(SmartOptions options, ISmartTokenStore tokenStore)
    {
        _options = options;
        _tokenStore = tokenStore;
    }

    public async Task<FhirClient> CreateAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.FhirBaseUrl))
        {
            throw new InvalidOperationException("FHIR base URL 未設定。");
        }

        var token = await _tokenStore.GetAsync(cancellationToken).ConfigureAwait(false);
        if (token is not { AccessToken: { Length: > 0 } accessToken })
        {
            throw new InvalidOperationException("尚未取得 SMART access token。");
        }
        var client = new FhirClient(_options.FhirBaseUrl);
        if (client.RequestHeaders is null)
        {
            throw new InvalidOperationException("FHIR client request headers 無法使用。");
        }

        client.RequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        return client;
    }
}
