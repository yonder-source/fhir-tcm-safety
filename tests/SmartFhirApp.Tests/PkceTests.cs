using SmartFhirApp.Core;
using Xunit;

namespace SmartFhirApp.Tests;

public class PkceTests
{
    [Fact]
    public void CreateCodeVerifier_Default_IsUrlSafe()
    {
        var verifier = Pkce.CreateCodeVerifier();

        Assert.False(string.IsNullOrWhiteSpace(verifier));
        foreach (var ch in verifier)
        {
            var isUrlSafe = (ch >= 'a' && ch <= 'z') ||
                            (ch >= 'A' && ch <= 'Z') ||
                            (ch >= '0' && ch <= '9') ||
                            ch == '-' || ch == '_';
            Assert.True(isUrlSafe);
        }
    }

    [Fact]
    public void CreateCodeChallenge_IsDeterministic()
    {
        var verifier = "test-code-verifier-123";
        var challenge1 = Pkce.CreateCodeChallenge(verifier);
        var challenge2 = Pkce.CreateCodeChallenge(verifier);

        Assert.False(string.IsNullOrWhiteSpace(challenge1));
        Assert.Equal(challenge1, challenge2);
    }
}
