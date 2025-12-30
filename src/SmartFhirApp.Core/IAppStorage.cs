namespace SmartFhirApp.Core;

public interface IAppStorage
{
    ValueTask SetStringAsync(string key, string value);
    ValueTask<string?> GetStringAsync(string key);
    ValueTask RemoveAsync(string key);
}
