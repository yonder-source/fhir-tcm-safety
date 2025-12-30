using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using SmartFhirApp.Core;
using SmartFhirApp.Web;
using SmartFhirApp.Web.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddAuthorizationCore();
builder.Services.AddOidcAuthentication(options =>
{
    var smart = builder.Configuration.GetSection("Smart").Get<SmartOptions>() ?? new SmartOptions();
    var issuerBaseUrl = string.IsNullOrWhiteSpace(smart.IssuerBaseUrl)
        ? smart.FhirBaseUrl
        : smart.IssuerBaseUrl;

    options.ProviderOptions.Authority = issuerBaseUrl;
    options.ProviderOptions.MetadataUrl = $"{issuerBaseUrl.TrimEnd('/')}/.well-known/smart-configuration";
    options.ProviderOptions.ClientId = smart.ClientId;
    options.ProviderOptions.ResponseType = "code";
    options.ProviderOptions.RedirectUri = $"{builder.HostEnvironment.BaseAddress}authentication/login-callback";
    options.ProviderOptions.PostLogoutRedirectUri = builder.HostEnvironment.BaseAddress;

    options.ProviderOptions.DefaultScopes.Clear();
    foreach (var scope in smart.Scope.Split(' ', StringSplitOptions.RemoveEmptyEntries))
    {
        options.ProviderOptions.DefaultScopes.Add(scope);
    }

    if (!string.IsNullOrWhiteSpace(smart.FhirBaseUrl))
    {
        options.ProviderOptions.AdditionalProviderParameters["aud"] = smart.FhirBaseUrl;
    }
});

builder.Services.AddScoped<IAppStorage, BrowserStorage>();
builder.Services.AddScoped<ISmartTokenStore, WebAccessTokenStore>();
builder.Services.AddScoped(sp =>
{
    var options = builder.Configuration.GetSection("Smart").Get<SmartOptions>() ?? new SmartOptions();
    return options;
});
builder.Services.AddScoped<IFhirClientFactory, FirelyFhirClientFactory>();

await builder.Build().RunAsync();
