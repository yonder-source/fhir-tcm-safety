using Moq;
using SmartFhirApp.Core;
using Xunit;

namespace SmartFhirApp.Tests;

public class FirelyFhirClientFactoryTests
{
    [Fact]
    public async Task CreateAsync_Throws_WhenMissingBaseUrl()
    {
        var options = new SmartOptions
        {
            FhirBaseUrl = string.Empty,
            ClientId = "client",
            RedirectUri = "app://callback"
        };
        var tokenStore = new Mock<ISmartTokenStore>();
        tokenStore.Setup(s => s.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SmartTokenResponse { AccessToken = "token" });

        var factory = new FirelyFhirClientFactory(options, tokenStore.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() => factory.CreateAsync());
    }

    [Fact]
    public async Task CreateAsync_Throws_WhenMissingToken()
    {
        var options = new SmartOptions
        {
            FhirBaseUrl = "https://example.org/fhir",
            ClientId = "client",
            RedirectUri = "app://callback"
        };
        var tokenStore = new Mock<ISmartTokenStore>();
        tokenStore.Setup(s => s.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((SmartTokenResponse?)null);

        var factory = new FirelyFhirClientFactory(options, tokenStore.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() => factory.CreateAsync());
    }
}
