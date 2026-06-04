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
    private System.Windows.Forms.ToolStripMenuItem? _menuLaunch;
    private System.Windows.Forms.ToolStripMenuItem? _menuStopAll;

    public MainWindow(IServiceProvider services, MainViewModel vm)
    {
        _services = services;
        _shutdownService = services.GetRequiredService<IAppShutdownService>();
        _vm = vm;
        DataContext = vm;
        InitializeComponent();
        InitializeTrayIcon();

        var svc = _services.GetRequiredService<IWindowStateService>();
        svc.MinimizeToTrayRequested += (_, _) => Dispatcher.Invoke(HideToTray);

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
        var iconStream = System.Windows.Application.GetResourceStream(
            new Uri("pack://application:,,,/Resources/Icons/FSBSIcon.ico"))?.Stream;
        var trayIcon = iconStream is not null
            ? new System.Drawing.Icon(iconStream)
            : System.Drawing.SystemIcons.Application;

        _notifyIcon = new System.Windows.Forms.NotifyIcon
        {
            Text = "FsBootStrapper",
            Visible = true,
            Icon = trayIcon
        };

        var uri = new Uri("pack://application:,,,/Resources/Icons/FSBSIcon.ico");
        var stream = System.Windows.Application.GetResourceStream(uri)?.Stream;
        if (stream != null)
            _notifyIcon.Icon = new System.Drawing.Icon(stream);
        else
            _notifyIcon.Icon = System.Drawing.SystemIcons.Application;

        _menuLaunch = new System.Windows.Forms.ToolStripMenuItem("Launch", null, OnMenuLaunchClicked);
        _menuStopAll = new System.Windows.Forms.ToolStripMenuItem("Stop All", null, OnMenuStopAllClicked);

        var ctxMenu = new System.Windows.Forms.ContextMenuStrip();
        ctxMenu.Items.Add(_menuLaunch);
        ctxMenu.Items.Add(_menuStopAll);
        ctxMenu.Items.Add(new System.Windows.Forms.ToolStripSeparator());
        ctxMenu.Items.Add("Open", null, (_, _) => ShowFromTray());
        ctxMenu.Items.Add("Open Logs Folder", null, (_, _) => OpenLogsFolder());
        ctxMenu.Items.Add(new System.Windows.Forms.ToolStripSeparator());
        ctxMenu.Items.Add("Exit without actions", null, (_, _) => ExitApp());
        ctxMenu.Opening += OnContextMenuOpening;
        _notifyIcon.ContextMenuStrip = ctxMenu;
        _notifyIcon.DoubleClick += (_, _) => ShowFromTray();

        // Register tray icon with the notification service
        var notificationSvc = _services.GetRequiredService<Core.Services.Notifications.INotificationService>();
        if (notificationSvc is TrayNotificationService tns)
            tns.SetTrayIcon(_notifyIcon);
    }

    private void OnContextMenuOpening(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        var orchestrator = _services.GetRequiredService<Core.Services.Launch.ILaunchOrchestrator>();
        _menuLaunch!.Enabled = !orchestrator.IsSessionActive && !orchestrator.IsLaunching;
        _menuStopAll!.Enabled = orchestrator.IsSessionActive;
    }

    private void OnMenuLaunchClicked(object? sender, EventArgs e)
    {
        ShowFromTray();
        var vm = _services.GetRequiredService<DashboardViewModel>();
        if (vm.LaunchCommand.CanExecute(null))
            _ = vm.LaunchCommand.ExecuteAsync(null);
    }

    private void OnMenuStopAllClicked(object? sender, EventArgs e)
    {
        var orchestrator = _services.GetRequiredService<Core.Services.Launch.ILaunchOrchestrator>();
        _ = orchestrator.StopAllAsync();
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
