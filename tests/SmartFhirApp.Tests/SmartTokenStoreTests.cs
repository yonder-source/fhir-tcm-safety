using Moq;
using SmartFhirApp.Core;
using Xunit;

namespace SmartFhirApp.Tests;

public class SmartTokenStoreTests
{
    [Fact]
    public async Task SaveAndGet_RoundTrip()
    {
        var stored = string.Empty;
        var storage = new Mock<IAppStorage>();
        storage.Setup(s => s.SetStringAsync("smart-token", It.IsAny<string>()))
            .Callback<string, string>((_, value) => stored = value)
            .Returns(ValueTask.CompletedTask);
        storage.Setup(s => s.GetStringAsync("smart-token"))
            .Returns(() => new ValueTask<string?>(stored));

        var store = new SmartTokenStore(storage.Object);
        var token = new SmartTokenResponse
        {
            AccessToken = "access-token",
            TokenType = "Bearer",
            Scope = "openid",
        };

        await store.SaveAsync(token);
        var result = await store.GetAsync();

        Assert.NotNull(result);
        Assert.Equal("access-token", result!.AccessToken);
        Assert.Equal("Bearer", result.TokenType);
        Assert.Equal("openid", result.Scope);
    }

    [Fact]
    public async Task Clear_RemovesToken()
    {
        var storage = new Mock<IAppStorage>();
        storage.Setup(s => s.RemoveAsync("smart-token"))
            .Returns(ValueTask.CompletedTask);

        var store = new SmartTokenStore(storage.Object);
        await store.ClearAsync();

        storage.Verify(s => s.RemoveAsync("smart-token"), Times.Once);
    }
}
