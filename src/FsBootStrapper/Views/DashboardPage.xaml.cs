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
}
