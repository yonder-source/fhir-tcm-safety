using Hl7.Fhir.Rest;

namespace SmartFhirApp.Core;

public interface IFhirClientFactory
{
    Task<FhirClient> CreateAsync(CancellationToken cancellationToken = default);
}
