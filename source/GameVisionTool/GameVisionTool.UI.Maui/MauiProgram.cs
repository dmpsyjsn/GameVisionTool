using CommunityToolkit.Maui;
using GameVisionTool.Logic.CrossCuttingConcerns;
using GameVisionTool.Logic.CompositionRoot;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using ILogger = Serilog.ILogger;

namespace GameVisionTool.UI.Maui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
#if DEBUG
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");

#elif RELEASE
    Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Production");
#endif
        Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);

        Log.Logger = CreateLogger();

        Log.Logger.Information("Starting up");

        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            })
            .Logging.AddSerilog(Log.Logger);

#if WINDOWS
        Platforms.Windows.EditorScrollBars.Enable();
#endif

        builder.Services.Bootstrap(FileSystem.AppDataDirectory);

        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

        if (string.IsNullOrEmpty(environment)) throw new Exception("Environment not set!");

        if (environment.Equals("Development"))
        {
            builder.Logging.AddDebug();

            builder.ConfigureContainer(new DefaultServiceProviderFactory(new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true
            }));
        }

        var mauiBuild = builder.Build();

        return mauiBuild;
    }

    private static ILogger CreateLogger()
    {
        return new LoggerConfiguration()
#if DEBUG
            .MinimumLevel.Debug()
#else
            .MinimumLevel.Information()
#endif
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .Destructure.With<SensitiveDataDestructuringPolicy>()
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", "GameVisionTool")
            .WriteTo.File(
                Path.Combine(FileSystem.AppDataDirectory, "logs", "log-.txt"),
                rollingInterval: RollingInterval.Day)
            .CreateLogger();
    }

}
