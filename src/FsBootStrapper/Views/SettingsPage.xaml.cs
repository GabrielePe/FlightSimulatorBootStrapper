using System.Windows.Controls;
using FsBootStrapper.ViewModels;

namespace FsBootStrapper.Views;

public partial class SettingsPage : Page
{
    public SettingsPage(SettingsViewModel vm)
    {
        DataContext = vm;
        InitializeComponent();
        Loaded += async (_, _) => await vm.InitializeAsync();
    }
}
