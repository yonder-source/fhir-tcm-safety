namespace SmartFhirApp.Core;

public interface ISmartTokenStore
{
    ValueTask<SmartTokenResponse?> GetAsync(CancellationToken cancellationToken = default);
    ValueTask SaveAsync(SmartTokenResponse token, CancellationToken cancellationToken = default);
    ValueTask ClearAsync(CancellationToken cancellationToken = default);
}
