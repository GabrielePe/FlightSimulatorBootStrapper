using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FsBootStrapper.Core.Models;
using FsBootStrapper.Core.Services.Detection;
using FsBootStrapper.Core.Services.Launch;
using FsBootStrapper.Core.Services.Profiles;
using FsBootStrapper.Core.Services.Settings;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;

namespace FsBootStrapper.ViewModels;

public sealed partial class DashboardViewModel : ViewModelBase
{
    private readonly IProfileRepository _profiles;
    private readonly IAppSettingsService _settings;
    private readonly IMsfsDetectionService _detection;
    private readonly ILaunchOrchestrator _orchestrator;
    private readonly ILogger<DashboardViewModel> _logger;

    [ObservableProperty] private ObservableCollection<AppProfile> _availableProfiles = [];
    [ObservableProperty] private AppProfile? _selectedProfile;
    [ObservableProperty] private MsfsInstallationInfo? _msfsInstallation;
    [ObservableProperty] private bool _isLaunching;
    [ObservableProperty] private string _statusMessage = "Ready";
    [ObservableProperty] private ObservableCollection<LaunchProgressItemViewModel> _launchProgress = [];

    public DashboardViewModel(
        IProfileRepository profiles,
        IAppSettingsService settings,
        IMsfsDetectionService detection,
        ILaunchOrchestrator orchestrator,
        ILogger<DashboardViewModel> logger)
    {
        _profiles = profiles;
        _settings = settings;
        _detection = detection;
        _orchestrator = orchestrator;
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        await LoadProfilesAsync();
        await DetectMsfsAsync();
    }

    private async Task LoadProfilesAsync()
    {
        var all = await _profiles.GetAllAsync();

        // Create default profile on first run
        if (!all.Any())
        {
            var def = new AppProfile
            {
                Name = "Default",
                Description = "Default launch profile. Add your add-ons in the Profiles page."
            };
            await _profiles.SaveAsync(def);
            all = await _profiles.GetAllAsync();
        }

        AvailableProfiles = new ObservableCollection<AppProfile>(all);

        if (_settings.Current.LastUsedProfileId.HasValue)
            SelectedProfile = AvailableProfiles.FirstOrDefault(p => p.Id == _settings.Current.LastUsedProfileId);

        SelectedProfile ??= AvailableProfiles.FirstOrDefault();
    }

    private async Task DetectMsfsAsync()
    {
        var installations = await _detection.DetectInstallationsAsync();
        MsfsInstallation = installations.FirstOrDefault(i =>
            i.Type == _settings.Current.MsfsInstallationType)
            ?? installations.FirstOrDefault();
    }

    [RelayCommand(CanExecute = nameof(CanLaunch))]
    private async Task LaunchAsync(CancellationToken ct)
    {
        if (SelectedProfile is null || MsfsInstallation is null) return;

        IsLaunching = true;
        LaunchProgress.Clear();
        StatusMessage = "Launching...";

        try
        {
            _settings.Current.LastUsedProfileId = SelectedProfile.Id;
            await _settings.SaveAsync(ct);

            var progress = new Progress<Core.Models.LaunchProgressEvent>(evt =>
            {
                LaunchProgress.Add(new LaunchProgressItemViewModel(evt));
                StatusMessage = evt.Message;
            });

            await _orchestrator.LaunchAsync(SelectedProfile, MsfsInstallation, _settings.Current, progress, ct);
            StatusMessage = "All items launched successfully";
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Launch cancelled";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Launch failed");
            StatusMessage = $"Launch failed: {ex.Message}";
        }
        finally
        {
            IsLaunching = false;
        }
    }

    private bool CanLaunch() => SelectedProfile is not null && MsfsInstallation is not null && !IsLaunching;

    [RelayCommand]
    private async Task StopAllAsync()
    {
        await _orchestrator.StopAllAsync();
        StatusMessage = "All processes stopped";
    }

    partial void OnIsLaunchingChanged(bool value) => LaunchCommand.NotifyCanExecuteChanged();
    partial void OnSelectedProfileChanged(AppProfile? value) => LaunchCommand.NotifyCanExecuteChanged();
    partial void OnMsfsInstallationChanged(MsfsInstallationInfo? value) => LaunchCommand.NotifyCanExecuteChanged();
}

public sealed partial class LaunchProgressItemViewModel : ObservableObject
{
    public LaunchProgressItemViewModel(Core.Models.LaunchProgressEvent evt)
    {
        Timestamp = evt.Timestamp;
        Message = evt.Message;
        EventType = evt.EventType;
        HasError = evt.Exception is not null;
    }

    [ObservableProperty] private DateTime _timestamp;
    [ObservableProperty] private string _message = string.Empty;
    [ObservableProperty] private Core.Models.LaunchEventType _eventType;
    [ObservableProperty] private bool _hasError;
}
