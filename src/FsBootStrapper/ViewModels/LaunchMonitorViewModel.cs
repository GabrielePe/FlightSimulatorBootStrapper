using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FsBootStrapper.Core.Models;
using FsBootStrapper.Core.Services.Launch;
using FsBootStrapper.Core.Services.Monitor;
using FsBootStrapper.Core.Services.Notifications;
using FsBootStrapper.Core.Services.Settings;
using FsBootStrapper.Services;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.Windows;

namespace FsBootStrapper.ViewModels;

public sealed partial class LaunchMonitorViewModel : ViewModelBase
{
    private readonly ILaunchOrchestrator _orchestrator;
    private readonly IProcessMonitor _monitor;
    private readonly INotificationService _notifications;
    private readonly IAppSettingsService _settings;
    private readonly IAppShutdownService _shutdownService;
    private readonly ILogger<LaunchMonitorViewModel> _logger;

    [ObservableProperty] private ObservableCollection<MonitorItemViewModel> _items = [];
    [ObservableProperty] private string _msfsStatus = "Not running";
    [ObservableProperty] private bool _msfsRunning;

    public LaunchMonitorViewModel(
        ILaunchOrchestrator orchestrator,
        IProcessMonitor monitor,
        INotificationService notifications,
        IAppSettingsService settings,
        IAppShutdownService shutdownService,
        ILogger<LaunchMonitorViewModel> logger)
    {
        _orchestrator = orchestrator;
        _monitor = monitor;
        _notifications = notifications;
        _settings = settings;
        _shutdownService = shutdownService;
        _logger = logger;

        _monitor.ProcessStateChanged += OnProcessStateChanged;
    }

    public void HandleProgressEvent(LaunchProgressEvent evt)
    {
        switch (evt.EventType)
        {
            case LaunchEventType.ItemLaunched:
                if (evt.ItemId.HasValue)
                {
                    var existing = Items.FirstOrDefault(i => i.ItemId == evt.ItemId);
                    if (existing is not null) existing.Status = MonitorItemStatus.Running;
                }
                break;
            case LaunchEventType.ItemFailed:
                if (evt.ItemId.HasValue)
                {
                    var existing = Items.FirstOrDefault(i => i.ItemId == evt.ItemId);
                    if (existing is not null) existing.Status = MonitorItemStatus.Failed;

                    if (_settings.Current.NotifyOnAddonCrash)
                        _notifications.Show("Launch Error", evt.Message, NotificationSeverity.Error);
                }
                break;
            case LaunchEventType.MsfsRunning:
                MsfsStatus = evt.Message;
                MsfsRunning = true;
                break;
            case LaunchEventType.MsfsExited:
                MsfsStatus = "Exited";
                MsfsRunning = false;
                if (_settings.Current.NotifyOnMsfsExit)
                    _notifications.Show("MSFS Exited", "MSFS 2024 has shut down. Closing add-ons...", NotificationSeverity.Info);
                break;
            case LaunchEventType.AutoCloseRequested:
                _shutdownService.RequestShutdown();
                System.Windows.Application.Current.Shutdown();
                break;
        }
    }

    public void AddItem(LaunchItem item)
    {
        Items.Add(new MonitorItemViewModel(item));
    }

    private void OnProcessStateChanged(object? sender, ProcessStateChangedEventArgs e)
    {
        var item = Items.FirstOrDefault(i => i.ItemId == e.ItemId);
        if (item is null) return;

        item.Status = e.IsRunning ? MonitorItemStatus.Running : MonitorItemStatus.Stopped;

        if (!e.IsRunning && e.ExitCode.HasValue && e.ExitCode != 0)
        {
            item.Status = MonitorItemStatus.Crashed;
            _logger.LogWarning("Add-on '{Name}' crashed with exit code {Code}", e.ProcessName, e.ExitCode);

            if (_settings.Current.NotifyOnAddonCrash)
                _notifications.Show(
                    "Add-on Crashed",
                    $"'{e.ProcessName}' exited unexpectedly (code {e.ExitCode})",
                    NotificationSeverity.Warning);
        }
    }

    [RelayCommand]
    private async Task StopAllAsync()
    {
        var stoppedCleanly = await _orchestrator.StopAllAsync();
        foreach (var item in Items)
            item.Status = MonitorItemStatus.Stopped;

        if (!stoppedCleanly)
            _logger.LogWarning("One or more add-ons stopped with warnings");
    }
}

public enum MonitorItemStatus { Pending, Running, Stopped, Failed, Crashed }

public sealed partial class MonitorItemViewModel : ObservableObject
{
    public Guid ItemId { get; }
    public string DisplayName { get; }
    public LaunchPhase Phase { get; }

    [ObservableProperty] private MonitorItemStatus _status = MonitorItemStatus.Pending;

    public MonitorItemViewModel(LaunchItem item)
    {
        ItemId = item.Id;
        DisplayName = item.DisplayName;
        Phase = item.Phase;
    }
}
