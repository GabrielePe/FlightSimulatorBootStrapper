using FsBootStrapper.Services;
using FsBootStrapper.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using Wpf.Ui.Controls;

namespace FsBootStrapper;

public partial class MainWindow : FluentWindow
{
    private readonly IServiceProvider _services;
    private readonly MainViewModel _vm;
    private System.Windows.Forms.NotifyIcon? _notifyIcon;
    private bool _forceClose;

    public MainWindow(IServiceProvider services, MainViewModel vm)
    {
        _services = services;
        _vm = vm;
        DataContext = vm;
        InitializeComponent();
        InitializeTrayIcon();

        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Register the content frame with the navigation service
        var navService = _services.GetRequiredService<ISimpleNavigationService>() as NavigationService;
        navService?.SetFrame(ContentFrame);

        // Wire NavigationView selection to the content Frame
        NavigationRoot.SelectionChanged += (_, _) =>
        {
            if (NavigationRoot.SelectedItem is Wpf.Ui.Controls.NavigationViewItem navItem &&
                navItem.TargetPageType is not null)
            {
                navService?.Navigate(navItem.TargetPageType);
            }
        };

        // Navigate to Dashboard on startup
        navService?.Navigate(typeof(Views.DashboardPage));
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
        if (!_forceClose)
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
        _forceClose = true;
        System.Windows.Application.Current.Shutdown();
    }
}
