using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PowerMauiApp.Data;
using PowerMauiApp.Services;
using System.Reflection;

namespace PowerMauiApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        var a = Assembly.GetExecutingAssembly();
        using var stream = a.GetManifestResourceStream("PowerMauiApp.appsettings.json");
        var config = new ConfigurationBuilder()
            .AddJsonStream(stream)
            .Build();

        builder.Configuration.AddConfiguration(config);

        builder.UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        builder.Services.AddMauiBlazorWebView();

#if DEBUG
		builder.Services.AddBlazorWebViewDeveloperTools();
		builder.Logging.AddDebug();
#endif

        builder.Services.AddSingleton<WeatherForecastService>();
        builder.Services.AddSingleton<AadService>();
        builder.Services.AddSingleton<PbiEmbedService>();
        builder.Services.AddSingleton<RouterData>();

        builder.Services.AddSingleton<HttpClient>();

        return builder.Build();
    }
}


public class RouterData
{
    public bool IsReport { get; set;}

    public bool IsReportActive { get; set; }
}