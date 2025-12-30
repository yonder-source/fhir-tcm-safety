using SmartFhirApp.Core;

namespace SmartFhirApp.Maui.Services;

public sealed class SecureStorageService : IAppStorage
{
    public async ValueTask SetStringAsync(string key, string value)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return;
        }

        await SecureStorage.Default.SetAsync(key, value).ConfigureAwait(false);
    }

    public async ValueTask<string?> GetStringAsync(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return null;
        }

        return await SecureStorage.Default.GetAsync(key).ConfigureAwait(false);
    }

    public ValueTask RemoveAsync(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return ValueTask.CompletedTask;
        }

        SecureStorage.Default.Remove(key);
        return ValueTask.CompletedTask;
    }
}
