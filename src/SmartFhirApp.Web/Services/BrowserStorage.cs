using Microsoft.JSInterop;
using SmartFhirApp.Core;

namespace SmartFhirApp.Web.Services;

public sealed class BrowserStorage : IAppStorage
{
    private readonly IJSRuntime _jsRuntime;

    public BrowserStorage(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public ValueTask SetStringAsync(string key, string value)
    {
        return _jsRuntime.InvokeVoidAsync("smartStorage.set", key, value);
    }

    public async ValueTask<string?> GetStringAsync(string key)
    {
        return await _jsRuntime.InvokeAsync<string?>("smartStorage.get", key).ConfigureAwait(false);
    }

    public ValueTask RemoveAsync(string key)
    {
        return _jsRuntime.InvokeVoidAsync("smartStorage.remove", key);
    }
}
