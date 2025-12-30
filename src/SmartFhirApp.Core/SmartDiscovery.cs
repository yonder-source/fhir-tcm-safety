namespace SmartFhirApp.Core;

public sealed class SmartDiscovery
{
    public string AuthorizationEndpoint { get; set; } = string.Empty;
    public string TokenEndpoint { get; set; } = string.Empty;
    public string? Issuer { get; set; }
}
