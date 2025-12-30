using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SmartFhirApp.Core;
using SmartFhirApp.Maui.Services;

namespace SmartFhirApp.Maui;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
			});

		builder.Services.AddMauiBlazorWebView();
		AddConfiguration(builder);
		ConfigureSmartServices(builder);

#if DEBUG
		builder.Services.AddBlazorWebViewDeveloperTools();
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}

	private static void AddConfiguration(MauiAppBuilder builder)
	{
		using var stream = FileSystem.OpenAppPackageFileAsync("appsettings.json").GetAwaiter().GetResult();
		var config = new ConfigurationBuilder()
			.AddJsonStream(stream)
			.Build();
		builder.Configuration.AddConfiguration(config);
	}

	private static void ConfigureSmartServices(MauiAppBuilder builder)
	{
		builder.Services.AddSingleton<HttpClient>();
		builder.Services.AddSingleton<IAppStorage, SecureStorageService>();
		builder.Services.AddSingleton<SmartDiscoveryService>();
		builder.Services.AddSingleton<ISmartTokenStore, SmartTokenStore>();
		builder.Services.AddSingleton(sp =>
		{
			var options = builder.Configuration.GetSection("Smart").Get<SmartOptions>() ?? new SmartOptions();
			return options;
		});
		builder.Services.AddSingleton<IFhirClientFactory, FirelyFhirClientFactory>();
		builder.Services.AddSingleton<MauiSmartLoginService>();
	}
}
