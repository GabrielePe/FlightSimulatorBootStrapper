using FsBootStrapper.Core.Abstractions;
using FsBootStrapper.Core.Infrastructure;
using FsBootStrapper.Core.Services.Detection;
using FsBootStrapper.Core.Services.Launch;
using FsBootStrapper.Core.Services.Monitor;
using FsBootStrapper.Core.Services.Notifications;
using FsBootStrapper.Core.Services.Profiles;
using FsBootStrapper.Core.Services.Settings;
using FsBootStrapper.Services;
using FsBootStrapper.ViewModels;
using FsBootStrapper.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using System.IO;
using System.Windows;

namespace FsBootStrapper;

public partial class App : System.Windows.Application
{
    private IHost? _host;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        ConfigureSerilog();

        _host = Host.CreateDefaultBuilder()
            .UseSerilog()
            .ConfigureServices(ConfigureServices)
            .Build();

        await _host.StartAsync();

        var settings = _host.Services.GetRequiredService<IAppSettingsService>();
        await settings.LoadAsync();

        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host is not null)
        {
            await _host.StopAsync(TimeSpan.FromSeconds(5));
            _host.Dispose();
        }
        Log.CloseAndFlush();
        base.OnExit(e);
    }

    private static void ConfigureSerilog()
    {
        var logDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "FsBootStrapper", "logs");
        Directory.CreateDirectory(logDir);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.Debug(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
            .WriteTo.File(
                path: Path.Combine(logDir, "fsbs-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
            .CreateLogger();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // Infrastructure
        services.AddSingleton<IFileSystem, PhysicalFileSystem>();
        services.AddSingleton<IRegistryReader, WindowsRegistryReader>();

        // Core services
        services.AddSingleton<IAppSettingsService, JsonAppSettingsService>();
        services.AddSingleton<IProfileRepository, JsonProfileRepository>();
        services.AddSingleton<IMsfsDetectionService, MsfsDetectionService>();
        services.AddSingleton<IProcessMonitor, ProcessMonitor>();
        services.AddSingleton<ILaunchOrchestrator, LaunchOrchestrator>();

        // App services
        services.AddSingleton<INotificationService, TrayNotificationService>();
        services.AddSingleton<IAppShutdownService, AppShutdownService>();
        services.AddSingleton<ISimpleNavigationService, NavigationService>();
        services.AddSingleton<IWindowStateService, WindowStateService>();

        // ViewModels
        services.AddTransient<MainViewModel>();
        services.AddSingleton<DashboardViewModel>();
        services.AddSingleton<ProfileEditorViewModel>();
        services.AddTransient<LaunchMonitorViewModel>();
        services.AddTransient<SettingsViewModel>();

        // Views
        services.AddSingleton<MainWindow>();
        services.AddSingleton<DashboardPage>();
        services.AddSingleton<ProfileEditorPage>();
        services.AddTransient<LaunchMonitorPage>();
        services.AddTransient<SettingsPage>();
    }
}

