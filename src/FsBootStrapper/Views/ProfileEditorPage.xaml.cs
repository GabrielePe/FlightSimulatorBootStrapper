using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using FsBootStrapper.Core.Models;
using FsBootStrapper.Core.Services.Import;
using FsBootStrapper.ViewModels;

namespace FsBootStrapper.Views;

/// <summary>Converts a delay in milliseconds to a display string: "—" when 0, otherwise "Xs" (seconds).</summary>
[ValueConversion(typeof(int), typeof(string))]
public sealed class DelayMsToStringConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is int ms && ms > 0 ? $"{ms / 1000} s" : "—";

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => System.Windows.Data.Binding.DoNothing;
}

/// <summary>Returns <c>true</c> when the bound value is not null, <c>false</c> when null.</summary>
[ValueConversion(typeof(object), typeof(bool))]
public sealed class NullToBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is not null;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => System.Windows.Data.Binding.DoNothing;
}

public partial class ProfileEditorPage : Page
{
    private readonly ProfileEditorViewModel _vm;

    public ProfileEditorPage(ProfileEditorViewModel vm)
    {
        _vm = vm;
        DataContext = vm;
        InitializeComponent();
        Loaded += async (_, _) => await vm.InitializeAsync();
    }

    // ── Profile CRUD handlers ─────────────────────────────────────
    private void OnNewProfileClicked(object sender, RoutedEventArgs e)
    {
        try { _vm.NewProfileCommand.Execute(null); }
        catch (Exception ex) { ShowError(ex); }
    }

    private async void OnSaveProfileClicked(object sender, RoutedEventArgs e)
    {
        try { await _vm.SaveProfileCommand.ExecuteAsync(null); }
        catch (Exception ex) { ShowError(ex); }
    }

    private async void OnDeleteProfileClicked(object sender, RoutedEventArgs e)
    {
        if (_vm.SelectedProfile is null) return;
        var confirm = System.Windows.MessageBox.Show(
            $"Delete profile '{_vm.SelectedProfile.Name}'?",
            "Confirm delete",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);
        if (confirm != MessageBoxResult.Yes) return;
        try { await _vm.DeleteProfileCommand.ExecuteAsync(null); }
        catch (Exception ex) { ShowError(ex); }
    }

    private static void ShowError(Exception ex) =>
        System.Windows.MessageBox.Show(ex.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);

    // ── Import from exe.xml ───────────────────────────────────────
    private void OnImportExeXmlClicked(object sender, RoutedEventArgs e)
    {
        if (_vm.SelectedProfile is null) return;

        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Select MSFS exe.xml",
            Filter = "exe.xml|exe.xml|XML files (*.xml)|*.xml|All files (*.*)|*.*",
            FileName = "exe.xml"
        };

        if (dlg.ShowDialog() != true) return;

        IReadOnlyList<FsBootStrapper.Core.Models.LaunchItem> imported;
        try
        {
            imported = ExeXmlImporter.Import(dlg.FileName);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                ex.Message,
                "Import failed",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            return;
        }

        if (imported.Count == 0)
        {
            System.Windows.MessageBox.Show(
                "No enabled add-ons were found in the selected file.",
                "Nothing to import",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        // Ask the user whether to append or replace existing items
        bool hasExisting = _vm.SelectedProfile.Items.Count > 0;
        bool append = true;

        if (hasExisting)
        {
            var answer = System.Windows.MessageBox.Show(
                $"Found {imported.Count} enabled add-on(s).\n\n" +
                "Click Yes to append them to the existing list,\n" +
                "or No to replace the list.",
                "Import exe.xml",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Question);

            if (answer == MessageBoxResult.Cancel) return;
            append = answer == MessageBoxResult.Yes;
        }

        if (!append)
            _vm.SelectedProfile.Items.Clear();

        int baseOrder = _vm.SelectedProfile.Items.Count;
        foreach (var item in imported)
        {
            item.SortOrder = baseOrder++;
            _vm.SelectedProfile.Items.Add(item);
        }

        _vm.OnItemEdited();

        // Ask whether to clean the original exe.xml
        var cleanAnswer = System.Windows.MessageBox.Show(
            $"{imported.Count} add-on(s) imported successfully.\n\n" +
            "Do you want FSBS to clean the original exe.xml?\n\n" +
            "A timestamped backup will be saved in the same folder before any changes are made.\n" +
            "The Launch.Addon entries will be removed so MSFS no longer launches them directly.",
            "Clean exe.xml?",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (cleanAnswer == MessageBoxResult.Yes)
        {
            try
            {
                var backupPath = ExeXmlImporter.BackupAndClean(dlg.FileName);
                System.Windows.MessageBox.Show(
                    $"exe.xml cleaned successfully.\nBackup saved to:\n{backupPath}",
                    "Done",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Import succeeded, but cleaning failed:\n{ex.Message}",
                    "Cleanup error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }
        else
        {
            System.Windows.MessageBox.Show(
                $"{imported.Count} add-on(s) imported. The original exe.xml was not modified.",
                "Import complete",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
    }

    // ── Add new add-on ────────────────────────────────────────────
    private void OnAddItemClicked(object sender, RoutedEventArgs e)
    {
        var item = _vm.CreateNewItem();
        if (OpenEditorDialog(item, isNew: true))
            _vm.CommitAddItem(item);
    }

    // ── Edit existing add-on ──────────────────────────────────────
    private void OnEditItemClicked(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: LaunchItem item }) return;

        // Work on a snapshot; apply only if user confirms
        var snapshot = CloneItem(item);
        if (OpenEditorDialog(snapshot, isNew: false))
        {
            ApplyItem(snapshot, item);
            _vm.OnItemEdited();

            // LaunchItem doesn't implement INotifyPropertyChanged, so the DataGrid
            // won't pick up the changed properties on its own. Force a refresh by
            // replacing the item in the ObservableCollection.
            var items = _vm.SelectedProfile!.Items;
            var idx = items.IndexOf(item);
            if (idx >= 0)
            {
                items.RemoveAt(idx);
                items.Insert(idx, item);
            }
        }
    }

    // ── Shared dialog helper ──────────────────────────────────────
    private bool OpenEditorDialog(LaunchItem item, bool isNew)
    {
        var editorVm = new AddOnEditorViewModel();
        editorVm.KnownProcessNames.Clear();
        foreach (var name in _vm.GetKnownProcessNames())
            editorVm.KnownProcessNames.Add(name);

        editorVm.LoadFrom(item);

        var dlg = new AddOnEditorDialog(editorVm, owner: null)
        {
            Title = isNew ? "Add Add-on" : "Edit Add-on"
        };
        dlg.Owner = Window.GetWindow(this);

        if (dlg.ShowDialog() == true)
        {
            editorVm.ApplyTo(item);
            return true;
        }
        return false;
    }

    // ── Helpers ───────────────────────────────────────────────────
    private static LaunchItem CloneItem(LaunchItem src) => new()
    {
        Id = src.Id,
        DisplayName = src.DisplayName,
        ExecutablePath = src.ExecutablePath,
        Arguments = src.Arguments,
        Phase = src.Phase,
        SortOrder = src.SortOrder,
        DelayBeforeLaunchMs = src.DelayBeforeLaunchMs,
        DelayAfterLaunchMs = src.DelayAfterLaunchMs,
        AutoCloseSupported = src.AutoCloseSupported,
        CloseMethod = src.CloseMethod,
        GracefulCloseTimeoutMs = src.GracefulCloseTimeoutMs,
        RequiresElevation = src.RequiresElevation,
        WaitForProcess = src.WaitForProcess is null ? null : new()
        {
            ProcessName = src.WaitForProcess.ProcessName,
            TimeoutSeconds = src.WaitForProcess.TimeoutSeconds,
            PollingIntervalMs = src.WaitForProcess.PollingIntervalMs
        }
    };

    private static void ApplyItem(LaunchItem src, LaunchItem dst)
    {
        dst.DisplayName = src.DisplayName;
        dst.ExecutablePath = src.ExecutablePath;
        dst.Arguments = src.Arguments;
        dst.Phase = src.Phase;
        dst.DelayBeforeLaunchMs = src.DelayBeforeLaunchMs;
        dst.DelayAfterLaunchMs = src.DelayAfterLaunchMs;
        dst.RequiresElevation = src.RequiresElevation;
        dst.AutoCloseSupported = src.AutoCloseSupported;
        dst.CloseMethod = src.CloseMethod;
        dst.GracefulCloseTimeoutMs = src.GracefulCloseTimeoutMs;
        dst.WaitForProcess = src.WaitForProcess;
    }
}

