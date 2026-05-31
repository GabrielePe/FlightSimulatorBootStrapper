using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FsBootStrapper.Services;

namespace FsBootStrapper.ViewModels;

public sealed partial class MainViewModel : ViewModelBase
{
    private readonly ISimpleNavigationService _navigation;

    [ObservableProperty] private string _title = "FsBootStrapper";
    [ObservableProperty] private bool _isPaneOpen = true;

    public MainViewModel(ISimpleNavigationService navigation)
    {
        _navigation = navigation;
    }

    [RelayCommand]
    private void NavigateToDashboard() =>
        _navigation.Navigate(typeof(Views.DashboardPage));

    [RelayCommand]
    private void NavigateToProfiles() =>
        _navigation.Navigate(typeof(Views.ProfileEditorPage));

    [RelayCommand]
    private void NavigateToSettings() =>
        _navigation.Navigate(typeof(Views.SettingsPage));
}
