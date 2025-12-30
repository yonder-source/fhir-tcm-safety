using System.Net;
using System.Net.Http.Json;
using SmartFhirApp.Core;
using Xunit;

namespace SmartFhirApp.Tests;

public class SmartDiscoveryServiceTests
{
    [Fact]
    public async Task GetDiscoveryAsync_UsesIssuerBaseUrl_WhenProvided()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new
            {
                authorization_endpoint = "https://issuer.example.com/auth",
                token_endpoint = "https://issuer.example.com/token",
                issuer = "https://issuer.example.com"
            })
        });

        var client = new HttpClient(handler);
        var service = new SmartDiscoveryService(client);

        await service.GetDiscoveryAsync(
            "https://fhir.example.com/fhir",
            "https://issuer.example.com/fhir");

        Assert.NotNull(handler.LastRequest);
        Assert.Equal(
            "https://issuer.example.com/fhir/.well-known/smart-configuration",
            handler.LastRequest!.RequestUri!.ToString());
    }

    [Fact]
    public async Task GetDiscoveryAsync_Throws_WhenEndpointsMissing()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new { })
        });

        var client = new HttpClient(handler);
        var service = new SmartDiscoveryService(client);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.GetDiscoveryAsync("https://fhir.example.com/fhir", null));
    }

    [Fact]
    public async Task GetDiscoveryAsync_Throws_WhenResponseEmpty()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(string.Empty)
        });

        var client = new HttpClient(handler);
        var service = new SmartDiscoveryService(client);

        await Assert.ThrowsAsync<System.Text.Json.JsonException>(() =>
            service.GetDiscoveryAsync("https://fhir.example.com/fhir", null));
    }

    [Fact]
    public async Task GetDiscoveryAsync_Throws_WhenBaseUrlMissing()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var client = new HttpClient(handler);
        var service = new SmartDiscoveryService(client);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.GetDiscoveryAsync(" ", null));
    }

    [Fact]
    public async Task GetDiscoveryAsync_UsesFhirBaseUrl_WhenIssuerNull()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new
            {
                authorization_endpoint = "https://fhir.example.com/auth",
                token_endpoint = "https://fhir.example.com/token"
            })
        });

        var client = new HttpClient(handler);
        var service = new SmartDiscoveryService(client);

        await service.GetDiscoveryAsync("https://fhir.example.com/fhir", null);

        Assert.NotNull(handler.LastRequest);
        Assert.Equal(
            "https://fhir.example.com/fhir/.well-known/smart-configuration",
            handler.LastRequest!.RequestUri!.ToString());
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responseFactory;

        public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        {
            _responseFactory = responseFactory;
        }

        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(_responseFactory(request));
        }
    }
}
