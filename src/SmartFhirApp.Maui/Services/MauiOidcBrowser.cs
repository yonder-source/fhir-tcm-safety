using System.Text;
using Duende.IdentityModel.OidcClient.Browser;

namespace SmartFhirApp.Maui.Services;

public sealed class MauiOidcBrowser : Duende.IdentityModel.OidcClient.Browser.IBrowser
{
    public async Task<BrowserResult> InvokeAsync(BrowserOptions options, CancellationToken cancellationToken = default)
    {
        try
        {
            var callbackUri = new Uri(options.EndUrl);
            var result = await WebAuthenticator.Default.AuthenticateAsync(new Uri(options.StartUrl), callbackUri)
                .WaitAsync(cancellationToken)
                .ConfigureAwait(false);

            var responseUrl = BuildCallbackUrl(options.EndUrl, result.Properties);
            return new BrowserResult
            {
                Response = responseUrl,
                ResultType = BrowserResultType.Success,
            };
        }
        catch (TaskCanceledException)
        {
            return new BrowserResult
            {
                ResultType = BrowserResultType.UserCancel,
            };
        }
        catch (Exception ex)
        {
            return new BrowserResult
            {
                ResultType = BrowserResultType.UnknownError,
                Error = ex.Message,
            };
        }
    }

    private static string BuildCallbackUrl(string endUrl, IReadOnlyDictionary<string, string> parameters)
    {
        var sb = new StringBuilder(endUrl);
        var separator = endUrl.Contains('?', StringComparison.Ordinal) ? "&" : "?";
        var first = true;

        foreach (var (key, value) in parameters)
        {
            if (first)
            {
                sb.Append(separator);
                first = false;
            }
            else
            {
                sb.Append('&');
            }

            sb.Append(Uri.EscapeDataString(key));
            sb.Append('=');
            sb.Append(Uri.EscapeDataString(value));
        }

        return sb.ToString();
    }
}
