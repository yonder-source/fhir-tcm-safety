using System.Net.Http.Json;

namespace SmartFhirApp.Core;

public sealed class SmartDiscoveryService
{
    private readonly HttpClient _httpClient;

    public SmartDiscoveryService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<SmartDiscovery> GetDiscoveryAsync(string fhirBaseUrl, string? issuerBaseUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fhirBaseUrl))
        {
            throw new ArgumentException("FHIR base URL is required.", nameof(fhirBaseUrl));
        }

        var baseUrl = (string.IsNullOrWhiteSpace(issuerBaseUrl) ? fhirBaseUrl : issuerBaseUrl).TrimEnd('/');
        var wellKnownUrl = $"{baseUrl}/.well-known/smart-configuration";
        var discovery = await _httpClient.GetFromJsonAsync<SmartDiscovery>(wellKnownUrl, cancellationToken).ConfigureAwait(false);

        if (discovery is null)
        {
            throw new InvalidOperationException("SMART configuration response is empty.");
        }

        if (string.IsNullOrWhiteSpace(discovery.AuthorizationEndpoint) ||
            string.IsNullOrWhiteSpace(discovery.TokenEndpoint))
        {
            throw new InvalidOperationException("SMART configuration is missing endpoints.");
        }

        return discovery;
    }
}
