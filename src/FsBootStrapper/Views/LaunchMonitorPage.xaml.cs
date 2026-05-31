using System.Windows.Controls;
using FsBootStrapper.ViewModels;

namespace FsBootStrapper.Views;

public partial class LaunchMonitorPage : Page
{
    public LaunchMonitorPage(LaunchMonitorViewModel vm)
    {
        DataContext = vm;
        InitializeComponent();
    }
}
