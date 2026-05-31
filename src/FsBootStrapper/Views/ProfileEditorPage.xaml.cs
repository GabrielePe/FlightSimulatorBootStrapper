using System.Windows.Controls;
using FsBootStrapper.ViewModels;

namespace FsBootStrapper.Views;

public partial class ProfileEditorPage : Page
{
    public ProfileEditorPage(ProfileEditorViewModel vm)
    {
        DataContext = vm;
        InitializeComponent();
        Loaded += async (_, _) => await vm.InitializeAsync();
    }
}
