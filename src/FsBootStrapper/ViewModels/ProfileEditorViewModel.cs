using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FsBootStrapper.Core.Models;
using FsBootStrapper.Core.Services.Profiles;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;

namespace FsBootStrapper.ViewModels;

public sealed partial class ProfileEditorViewModel : ViewModelBase
{
    private readonly IProfileRepository _repository;
    private readonly ILogger<ProfileEditorViewModel> _logger;

    [ObservableProperty] private ObservableCollection<AppProfile> _profiles = [];
    [ObservableProperty] private AppProfile? _selectedProfile;
    [ObservableProperty] private bool _isDirty;
    [ObservableProperty] private string? _statusMessage;

    public ProfileEditorViewModel(
        IProfileRepository repository,
        ILogger<ProfileEditorViewModel> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        var all = await _repository.GetAllAsync();
        Profiles = new ObservableCollection<AppProfile>(all);
        SelectedProfile = Profiles.FirstOrDefault();
    }

    [RelayCommand]
    private void NewProfile()
    {
        var profile = new AppProfile { Name = "New Profile" };
        Profiles.Add(profile);
        SelectedProfile = profile;
        IsDirty = true;
    }

    [RelayCommand]
    private async Task SaveProfileAsync()
    {
        if (SelectedProfile is null) return;
        await _repository.SaveAsync(SelectedProfile);
        IsDirty = false;
        StatusMessage = $"Profile '{SelectedProfile.Name}' saved";
        _logger.LogInformation("Profile '{Name}' saved from editor", SelectedProfile.Name);
    }

    [RelayCommand]
    private async Task DeleteProfileAsync()
    {
        if (SelectedProfile is null) return;
        var id = SelectedProfile.Id;
        Profiles.Remove(SelectedProfile);
        await _repository.DeleteAsync(id);
        SelectedProfile = Profiles.FirstOrDefault();
        StatusMessage = "Profile deleted";
    }

    [RelayCommand]
    private void AddItem()
    {
        if (SelectedProfile is null) return;
        var item = new LaunchItem
        {
            DisplayName = "New Add-on",
            SortOrder = SelectedProfile.Items.Count
        };
        SelectedProfile.Items.Add(item);
        IsDirty = true;
    }

    [RelayCommand]
    private void RemoveItem(LaunchItem item)
    {
        SelectedProfile?.Items.Remove(item);
        IsDirty = true;
    }

    [RelayCommand]
    private void MoveItemUp(LaunchItem item)
    {
        if (SelectedProfile is null) return;
        var idx = SelectedProfile.Items.IndexOf(item);
        if (idx <= 0) return;
        SelectedProfile.Items.RemoveAt(idx);
        SelectedProfile.Items.Insert(idx - 1, item);
        RenumberItems();
        IsDirty = true;
    }

    [RelayCommand]
    private void MoveItemDown(LaunchItem item)
    {
        if (SelectedProfile is null) return;
        var idx = SelectedProfile.Items.IndexOf(item);
        if (idx < 0 || idx >= SelectedProfile.Items.Count - 1) return;
        SelectedProfile.Items.RemoveAt(idx);
        SelectedProfile.Items.Insert(idx + 1, item);
        RenumberItems();
        IsDirty = true;
    }

    private void RenumberItems()
    {
        if (SelectedProfile is null) return;
        for (int i = 0; i < SelectedProfile.Items.Count; i++)
            SelectedProfile.Items[i].SortOrder = i;
    }

    partial void OnSelectedProfileChanged(AppProfile? value) => IsDirty = false;
}
