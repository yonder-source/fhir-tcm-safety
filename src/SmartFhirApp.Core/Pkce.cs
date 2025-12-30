using System.Security.Cryptography;
using System.Text;

namespace SmartFhirApp.Core;

public static class Pkce
{
    public static string CreateCodeVerifier(int length = 64)
    {
        if (length < 43 || length > 128)
        {
            throw new ArgumentOutOfRangeException(nameof(length), "PKCE verifier length must be between 43 and 128.");
        }

        var bytes = RandomNumberGenerator.GetBytes(length);
        return Base64UrlEncode(bytes);
    }

    public static string CreateCodeChallenge(string codeVerifier)
    {
        if (string.IsNullOrWhiteSpace(codeVerifier))
        {
            throw new ArgumentException("Code verifier is required.", nameof(codeVerifier));
        }

        var bytes = Encoding.ASCII.GetBytes(codeVerifier);
        var hash = SHA256.HashData(bytes);
        return Base64UrlEncode(hash);
    }

    private static string Base64UrlEncode(byte[] data)
    {
        var text = Convert.ToBase64String(data);
        return text.Replace("+", "-", StringComparison.Ordinal)
            .Replace("/", "_", StringComparison.Ordinal)
            .Replace("=", string.Empty, StringComparison.Ordinal);
    }
}
