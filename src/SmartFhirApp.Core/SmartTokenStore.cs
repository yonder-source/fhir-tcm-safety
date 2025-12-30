using System.Text.Json;

namespace SmartFhirApp.Core;

public sealed class SmartTokenStore : ISmartTokenStore
{
    private const string TokenStorageKey = "smart-token";
    private readonly IAppStorage _storage;

    public SmartTokenStore(IAppStorage storage)
    {
        _storage = storage;
    }

    public async ValueTask<SmartTokenResponse?> GetAsync(CancellationToken cancellationToken = default)
    {
        var json = await _storage.GetStringAsync(TokenStorageKey).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        return JsonSerializer.Deserialize<SmartTokenResponse>(json);
    }

    public async ValueTask SaveAsync(SmartTokenResponse token, CancellationToken cancellationToken = default)
    {
        if (token is null)
        {
            throw new ArgumentNullException(nameof(token));
        }

        var json = JsonSerializer.Serialize(token, new JsonSerializerOptions
        {
            WriteIndented = true,
        });

        await _storage.SetStringAsync(TokenStorageKey, json).ConfigureAwait(false);
    }

    public ValueTask ClearAsync(CancellationToken cancellationToken = default)
    {
        return _storage.RemoveAsync(TokenStorageKey);
    }
}
