using FsBootStrapper.Services;
using FsBootStrapper.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using Wpf.Ui.Controls;

namespace FsBootStrapper;

public partial class MainWindow : FluentWindow
{
    private readonly IServiceProvider _services;
    private readonly IAppShutdownService _shutdownService;
    private readonly MainViewModel _vm;
    private System.Windows.Forms.NotifyIcon? _notifyIcon;

    public MainWindow(IServiceProvider services, MainViewModel vm)
    {
        _services = services;
        _shutdownService = services.GetRequiredService<IAppShutdownService>();
        _vm = vm;
        DataContext = vm;
        InitializeComponent();
        InitializeTrayIcon();

        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Give the NavigationView a DI service provider so it can resolve pages
        NavigationRoot.SetServiceProvider(_services);

        // Give our custom navigation service a reference to the NavigationView
        var navService = _services.GetRequiredService<ISimpleNavigationService>() as NavigationService;
        navService?.SetNavigationView(NavigationRoot);

        // Navigate to Dashboard on startup
        NavigationRoot.Navigate(typeof(Views.DashboardPage), null);
    }

    private void InitializeTrayIcon()
    {
        _notifyIcon = new System.Windows.Forms.NotifyIcon
        {
            Text = "FsBootStrapper",
            Visible = true,
            // Use a default icon until a real one is bundled
            Icon = System.Drawing.SystemIcons.Application
        };

        var ctxMenu = new System.Windows.Forms.ContextMenuStrip();
        ctxMenu.Items.Add("Show", null, (_, _) => ShowFromTray());
        ctxMenu.Items.Add("Open Logs Folder", null, (_, _) => OpenLogsFolder());
        ctxMenu.Items.Add(new System.Windows.Forms.ToolStripSeparator());
        ctxMenu.Items.Add("Exit", null, (_, _) => ExitApp());
        _notifyIcon.ContextMenuStrip = ctxMenu;
        _notifyIcon.DoubleClick += (_, _) => ShowFromTray();

        // Register tray icon with the notification service
        var notificationSvc = _services.GetRequiredService<Core.Services.Notifications.INotificationService>();
        if (notificationSvc is TrayNotificationService tns)
            tns.SetTrayIcon(_notifyIcon);
    }

    protected override void OnStateChanged(EventArgs e)
    {
        base.OnStateChanged(e);
        var settings = _services.GetRequiredService<Core.Services.Settings.IAppSettingsService>();
        if (WindowState == WindowState.Minimized && settings.Current.MinimizeToTray)
            HideToTray();
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        if (!_shutdownService.ShutdownRequested)
        {
            var settings = _services.GetRequiredService<Core.Services.Settings.IAppSettingsService>();
            if (settings.Current.MinimizeToTray)
            {
                e.Cancel = true;
                HideToTray();
                return;
            }
        }
        _notifyIcon?.Dispose();
        base.OnClosing(e);
    }

    private void HideToTray()
    {
        Hide();
        WindowState = WindowState.Minimized;
        _notifyIcon!.ShowBalloonTip(2000, "FsBootStrapper", "Running in background", System.Windows.Forms.ToolTipIcon.Info);
    }

    private void ShowFromTray()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }

    private void OpenLogsFolder()
    {
        var logDir = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "FsBootStrapper", "logs");
        System.Diagnostics.Process.Start("explorer.exe", logDir);
    }

    private void ExitApp()
    {
        _shutdownService.RequestShutdown();
        System.Windows.Application.Current.Shutdown();
    }
}
