using FsBootStrapper.ViewModels;
using System.Windows;
using Wpf.Ui.Controls;

namespace FsBootStrapper.Views;

public partial class AddOnEditorDialog : FluentWindow
{
    private readonly AddOnEditorViewModel _vm;

    public AddOnEditorDialog(AddOnEditorViewModel vm, Window? owner = null)
    {
        _vm = vm;
        DataContext = vm;
        Owner = owner;
        InitializeComponent();
        Loaded += (_, _) => TxtDisplayName.Focus();
    }

    private void OnOkClicked(object sender, RoutedEventArgs e)
    {
        if (!_vm.IsValid)
        {
            System.Windows.MessageBox.Show(
                "Display name and Executable are required.",
                "Validation",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Warning);
            return;
        }
        DialogResult = true;
    }

    private void OnCancelClicked(object sender, RoutedEventArgs e) =>
        DialogResult = false;
}
