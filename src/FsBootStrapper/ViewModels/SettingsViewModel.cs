using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FsBootStrapper.Core.Models;
using FsBootStrapper.Core.Services.Detection;
using FsBootStrapper.Core.Services.Settings;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;

namespace FsBootStrapper.ViewModels;

public sealed partial class SettingsViewModel : ViewModelBase
{
    private readonly IAppSettingsService _settingsService;
    private readonly IMsfsDetectionService _detection;
    private readonly ILogger<SettingsViewModel> _logger;

    [ObservableProperty] private ObservableCollection<MsfsInstallationInfo> _detectedInstallations = [];
    [ObservableProperty] private MsfsInstallationInfo? _selectedInstallation;
    [ObservableProperty] private bool _minimizeToTray;
    [ObservableProperty] private bool _notifyOnAddonCrash;
    [ObservableProperty] private bool _notifyOnMsfsExit;
    [ObservableProperty] private string? _customMsfsPath;
    [ObservableProperty] private string _statusMessage = string.Empty;

    public SettingsViewModel(
        IAppSettingsService settingsService,
        IMsfsDetectionService detection,
        ILogger<SettingsViewModel> logger)
    {
        _settingsService = settingsService;
        _detection = detection;
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        var settings = _settingsService.Current;
        MinimizeToTray = settings.MinimizeToTray;
        NotifyOnAddonCrash = settings.NotifyOnAddonCrash;
        NotifyOnMsfsExit = settings.NotifyOnMsfsExit;
        CustomMsfsPath = settings.MsfsCustomExecutablePath;

        var installations = await _detection.DetectInstallationsAsync();
        DetectedInstallations = new ObservableCollection<MsfsInstallationInfo>(installations);
        SelectedInstallation = DetectedInstallations.FirstOrDefault(i =>
            i.Type == settings.MsfsInstallationType)
            ?? DetectedInstallations.FirstOrDefault();
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        var s = _settingsService.Current;
        s.MinimizeToTray = MinimizeToTray;
        s.NotifyOnAddonCrash = NotifyOnAddonCrash;
        s.NotifyOnMsfsExit = NotifyOnMsfsExit;
        s.MsfsCustomExecutablePath = CustomMsfsPath;

        if (SelectedInstallation is not null)
            s.MsfsInstallationType = SelectedInstallation.Type;

        await _settingsService.SaveAsync();
        StatusMessage = "Settings saved";
        _logger.LogInformation("Settings saved");
    }

    [RelayCommand]
    private async Task DetectMsfsAsync()
    {
        var installations = await _detection.DetectInstallationsAsync();
        DetectedInstallations = new ObservableCollection<MsfsInstallationInfo>(installations);
        StatusMessage = installations.Count == 0
            ? "No MSFS 2024 installation found"
            : $"Found {installations.Count} installation(s)";
    }
}
