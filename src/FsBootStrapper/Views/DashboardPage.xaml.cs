using System.Windows.Controls;
using FsBootStrapper.ViewModels;

namespace FsBootStrapper.Views;

public partial class DashboardPage : Page
{
    public DashboardPage(DashboardViewModel vm)
    {
        DataContext = vm;
        InitializeComponent();
        Loaded += async (_, _) => await vm.InitializeAsync();
    }

    private async void OnLaunchClicked(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is DashboardViewModel vm)
        {
            try { await vm.LaunchCommand.ExecuteAsync(null); }
            catch (Exception ex) { ShowError(ex); }
        }
    }

    private async void OnStopAllClicked(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is DashboardViewModel vm)
        {
            try { await vm.StopAllCommand.ExecuteAsync(null); }
            catch (Exception ex) { ShowError(ex); }
        }
    }

    private static void ShowError(Exception ex) =>
        System.Windows.MessageBox.Show(
            ex.Message,
            "Error",
            System.Windows.MessageBoxButton.OK,
            System.Windows.MessageBoxImage.Error);
}
