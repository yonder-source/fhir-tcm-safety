using System.Text.Json.Serialization;

namespace SmartFhirApp.Core;

public sealed class SmartDiscovery
{
    [JsonPropertyName("authorization_endpoint")]
    public string AuthorizationEndpoint { get; set; } = string.Empty;

    [JsonPropertyName("token_endpoint")]
    public string TokenEndpoint { get; set; } = string.Empty;

    [JsonPropertyName("issuer")]
    public string? Issuer { get; set; }
}
