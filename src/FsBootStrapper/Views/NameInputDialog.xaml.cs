using System.Windows;
using Wpf.Ui.Controls;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using Key = System.Windows.Input.Key;

namespace FsBootStrapper.Views;

public partial class NameInputDialog : FluentWindow
{
    public string? ProfileName { get; private set; }

    public NameInputDialog(string initialName, Window owner)
    {
        InitializeComponent();
        Owner = owner;
        TxtName.Text = initialName;
        Loaded += (_, _) => { TxtName.SelectAll(); TxtName.Focus(); };
    }

    private void OnOkClicked(object sender, RoutedEventArgs e) => Confirm();

    private void OnCancelClicked(object sender, RoutedEventArgs e) =>
        DialogResult = false;

    private void OnTextBoxKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) Confirm();
        else if (e.Key == Key.Escape) DialogResult = false;
    }

    private void Confirm()
    {
        var name = TxtName.Text.Trim();
        if (string.IsNullOrEmpty(name)) return;
        ProfileName = name;
        DialogResult = true;
    }
}
