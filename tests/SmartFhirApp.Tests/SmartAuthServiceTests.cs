using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Moq;
using SmartFhirApp.Core;
using Xunit;

namespace SmartFhirApp.Tests;

public class SmartAuthServiceTests
{
    [Fact]
    public async Task BuildAuthorizeUrlAsync_Throws_WhenOptionsMissing()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var client = new HttpClient(handler);
        var discovery = new SmartDiscoveryService(client);
        var storage = new Mock<IAppStorage>();
        var options = new SmartOptions
        {
            FhirBaseUrl = string.Empty,
            ClientId = "client",
            RedirectUri = "http://localhost/callback"
        };

        var auth = new SmartAuthService(discovery, client, storage.Object, options);

        await Assert.ThrowsAsync<InvalidOperationException>(() => auth.BuildAuthorizeUrlAsync());
    }

    [Fact]
    public async Task BuildAuthorizeUrlAsync_PersistsSession()
    {
        var handler = new StubHttpMessageHandler(request =>
        {
            if (request.RequestUri!.AbsolutePath.EndsWith("/.well-known/smart-configuration", StringComparison.Ordinal))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(new
                    {
                        authorization_endpoint = "https://issuer.example.com/auth",
                        token_endpoint = "https://issuer.example.com/token",
                        issuer = "https://issuer.example.com"
                    })
                };
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        var client = new HttpClient(handler);
        var discovery = new SmartDiscoveryService(client);
        var storage = new Mock<IAppStorage>();
        storage.Setup(s => s.SetStringAsync("smart-auth-session", It.IsAny<string>()))
            .Returns(ValueTask.CompletedTask);

        var options = new SmartOptions
        {
            FhirBaseUrl = "https://fhir.example.com/fhir",
            ClientId = "client",
            RedirectUri = "http://localhost/callback",
            Scope = "launch/patient openid"
        };

        var auth = new SmartAuthService(discovery, client, storage.Object, options);
        var url = await auth.BuildAuthorizeUrlAsync("launch-context");

        Assert.Contains("aud=", url, StringComparison.Ordinal);
        Assert.Contains("launch=launch-context", url, StringComparison.Ordinal);
        storage.Verify(s => s.SetStringAsync("smart-auth-session", It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task ExchangeCodeAsync_Throws_WhenCodeMissing()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var client = new HttpClient(handler);
        var discovery = new SmartDiscoveryService(client);
        var storage = new Mock<IAppStorage>();
        var options = new SmartOptions
        {
            FhirBaseUrl = "https://fhir.example.com/fhir",
            ClientId = "client",
            RedirectUri = "http://localhost/callback"
        };

        var auth = new SmartAuthService(discovery, client, storage.Object, options);
        await Assert.ThrowsAsync<ArgumentException>(() => auth.ExchangeCodeAsync(" ", null));
    }

    [Fact]
    public async Task ExchangeCodeAsync_Throws_WhenSessionMissing()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var client = new HttpClient(handler);
        var discovery = new SmartDiscoveryService(client);
        var storage = new Mock<IAppStorage>();
        storage.Setup(s => s.GetStringAsync("smart-auth-session"))
            .Returns(ValueTask.FromResult<string?>(null));

        var options = new SmartOptions
        {
            FhirBaseUrl = "https://fhir.example.com/fhir",
            ClientId = "client",
            RedirectUri = "http://localhost/callback"
        };

        var auth = new SmartAuthService(discovery, client, storage.Object, options);
        await Assert.ThrowsAsync<InvalidOperationException>(() => auth.ExchangeCodeAsync("code", "state"));
    }

    [Fact]
    public async Task ExchangeCodeAsync_Throws_WhenStateMismatch()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var client = new HttpClient(handler);
        var discovery = new SmartDiscoveryService(client);
        var storage = new Mock<IAppStorage>();
        storage.Setup(s => s.GetStringAsync("smart-auth-session"))
            .Returns(ValueTask.FromResult<string?>(JsonSerializer.Serialize(new SmartAuthSession
            {
                State = "expected",
                CodeVerifier = "verifier",
                RedirectUri = "http://localhost/callback"
            })));

        var options = new SmartOptions
        {
            FhirBaseUrl = "https://fhir.example.com/fhir",
            ClientId = "client",
            RedirectUri = "http://localhost/callback"
        };

        var auth = new SmartAuthService(discovery, client, storage.Object, options);
        await Assert.ThrowsAsync<InvalidOperationException>(() => auth.ExchangeCodeAsync("code", "wrong"));
    }

    [Fact]
    public async Task ExchangeCodeAsync_Throws_WhenTokenEndpointReturnsError()
    {
        var handler = new StubHttpMessageHandler(request =>
        {
            if (request.RequestUri!.AbsolutePath.EndsWith("/.well-known/smart-configuration", StringComparison.Ordinal))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(new
                    {
                        authorization_endpoint = "https://issuer.example.com/auth",
                        token_endpoint = "https://issuer.example.com/token"
                    })
                };
            }

            if (request.RequestUri!.ToString() == "https://issuer.example.com/token")
            {
                return new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = JsonContent.Create(new
                    {
                        error = "invalid_grant",
                        error_description = "bad code"
                    })
                };
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        var client = new HttpClient(handler);
        var discovery = new SmartDiscoveryService(client);
        var storage = new Mock<IAppStorage>();
        storage.Setup(s => s.GetStringAsync("smart-auth-session"))
            .Returns(ValueTask.FromResult<string?>(JsonSerializer.Serialize(new SmartAuthSession
            {
                State = "state",
                CodeVerifier = "verifier",
                RedirectUri = "http://localhost/callback"
            })));

        var options = new SmartOptions
        {
            FhirBaseUrl = "https://fhir.example.com/fhir",
            ClientId = "client",
            RedirectUri = "http://localhost/callback"
        };

        var auth = new SmartAuthService(discovery, client, storage.Object, options);
        await Assert.ThrowsAsync<InvalidOperationException>(() => auth.ExchangeCodeAsync("code", "state"));
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responseFactory;

        public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        {
            _responseFactory = responseFactory;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(_responseFactory(request));
        }
    }
}
