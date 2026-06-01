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
    [ObservableProperty] private string _profileName = string.Empty;
    [ObservableProperty] private string _profileDescription = string.Empty;
    [ObservableProperty] private bool _isDirty;
    [ObservableProperty] private string? _statusMessage;

    // Guard to prevent cascading property-change updates
    private bool _syncing;

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

        // Create default profile on first run
        if (!all.Any())
        {
            var def = new AppProfile
            {
                Name = "Default",
                Description = "Default launch profile"
            };
            await _repository.SaveAsync(def);
            all = await _repository.GetAllAsync();
        }

        Profiles = new ObservableCollection<AppProfile>(all);
        SelectedProfile = Profiles.FirstOrDefault();
    }

    // ── Profile name / description two-way sync ───────────────────

    partial void OnSelectedProfileChanged(AppProfile? value)
    {
        if (_syncing) return;
        _syncing = true;
        ProfileName = value?.Name ?? string.Empty;
        ProfileDescription = value?.Description ?? string.Empty;
        _syncing = false;
        IsDirty = false;
        StatusMessage = null;
    }

    partial void OnProfileNameChanged(string value)
    {
        if (_syncing || SelectedProfile is null) return;
        // AppProfile.Name already raises INotifyPropertyChanged, so the ListBox
        // updates automatically — no Remove/Insert hack needed.
        SelectedProfile.Name = value;
        IsDirty = true;
    }

    partial void OnProfileDescriptionChanged(string value)
    {
        if (_syncing || SelectedProfile is null) return;
        SelectedProfile.Description = string.IsNullOrWhiteSpace(value) ? null : value;
        IsDirty = true;
    }

    // ── Profile CRUD ─────────────────────────────────────────────

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
        StatusMessage = $"✓ Profile '{SelectedProfile.Name}' saved";
        _logger.LogInformation("Profile '{Name}' saved from editor", SelectedProfile.Name);
    }

    [RelayCommand]
    private async Task DeleteProfileAsync()
    {
        if (SelectedProfile is null) return;
        var id = SelectedProfile.Id;
        var name = SelectedProfile.Name;
        Profiles.Remove(SelectedProfile);
        await _repository.DeleteAsync(id);
        SelectedProfile = Profiles.FirstOrDefault();
        StatusMessage = $"Profile '{name}' deleted";
    }

    [RelayCommand]
    private void DuplicateProfile(string newName)
    {
        if (SelectedProfile is null) return;

        var copy = new AppProfile
        {
            Name = newName,
            Description = SelectedProfile.Description
        };

        foreach (var item in SelectedProfile.Items)
        {
            copy.Items.Add(new FsBootStrapper.Core.Models.LaunchItem
            {
                DisplayName        = item.DisplayName,
                ExecutablePath     = item.ExecutablePath,
                Arguments          = item.Arguments,
                Phase              = item.Phase,
                SortOrder          = item.SortOrder,
                DelayBeforeLaunchMs = item.DelayBeforeLaunchMs,
                DelayAfterLaunchMs  = item.DelayAfterLaunchMs,
                WaitForProcess     = item.WaitForProcess,
                AutoCloseSupported = item.AutoCloseSupported,
                CloseMethod        = item.CloseMethod,
                GracefulCloseTimeoutMs = item.GracefulCloseTimeoutMs,
                RequiresElevation  = item.RequiresElevation
            });
        }

        Profiles.Add(copy);
        SelectedProfile = copy;
        IsDirty = true;
    }

    // ── Item management (called from code-behind after dialog) ────

    /// <summary>
    /// Creates a new <see cref="LaunchItem"/> pre-populated with a reasonable SortOrder
    /// and ready to be edited in the dialog.
    /// </summary>
    public LaunchItem CreateNewItem() => new()
    {
        DisplayName = string.Empty,
        SortOrder = SelectedProfile?.Items.Count ?? 0
    };

    /// <summary>
    /// Returns the display names of all other executables in the profile
    /// as WaitForProcess suggestions.
    /// </summary>
    public List<string> GetKnownProcessNames()
    {
        var names = new List<string> { "FlightSimulator" };
        if (SelectedProfile is not null)
        {
            names.AddRange(SelectedProfile.Items
                .Where(i => !string.IsNullOrWhiteSpace(i.ExecutablePath))
                .Select(i => System.IO.Path.GetFileNameWithoutExtension(i.ExecutablePath))
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Distinct()
                .OrderBy(n => n));
        }
        return names;
    }

    /// <summary>Adds the item to the selected profile (called after dialog OK).</summary>
    public void CommitAddItem(LaunchItem item)
    {
        if (SelectedProfile is null) return;
        item.SortOrder = SelectedProfile.Items.Count;
        SelectedProfile.Items.Add(item);
        IsDirty = true;
    }

    /// <summary>Marks profile dirty after an existing item was edited.</summary>
    public void OnItemEdited() => IsDirty = true;

    [RelayCommand]
    private void RemoveItem(LaunchItem item)
    {
        SelectedProfile?.Items.Remove(item);
        RenumberItems();
        IsDirty = true;
    }

    [RelayCommand]
    private void MoveItemUp(LaunchItem item)
    {
        if (SelectedProfile is null) return;
        var idx = SelectedProfile.Items.IndexOf(item);
        if (idx <= 0) return;
        SelectedProfile.Items.Move(idx, idx - 1);
        RenumberItems();
        IsDirty = true;
    }

    [RelayCommand]
    private void MoveItemDown(LaunchItem item)
    {
        if (SelectedProfile is null) return;
        var idx = SelectedProfile.Items.IndexOf(item);
        if (idx < 0 || idx >= SelectedProfile.Items.Count - 1) return;
        SelectedProfile.Items.Move(idx, idx + 1);
        RenumberItems();
        IsDirty = true;
    }

    private void RenumberItems()
    {
        if (SelectedProfile is null) return;
        for (int i = 0; i < SelectedProfile.Items.Count; i++)
            SelectedProfile.Items[i].SortOrder = i;
    }
}

