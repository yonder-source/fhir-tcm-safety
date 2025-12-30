namespace SmartFhirApp.Core;

public sealed class SmartOptions
{
    public string FhirBaseUrl { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string RedirectUri { get; set; } = string.Empty;
    public string Scope { get; set; } = "launch/patient openid fhirUser profile offline_access";
    public string? IssuerBaseUrl { get; set; }
}
