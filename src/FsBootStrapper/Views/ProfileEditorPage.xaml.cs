using System.Windows;
using System.Windows.Controls;
using FsBootStrapper.Core.Models;
using FsBootStrapper.ViewModels;

namespace FsBootStrapper.Views;

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

        var dlg = new AddOnEditorDialog(editorVm, Window.GetWindow(this))
        {
            Title = isNew ? "Add Add-on" : "Edit Add-on"
        };

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
        AutoCloseSupported = src.AutoCloseSupported,
        CloseMethod = src.CloseMethod,
        GracefulCloseTimeoutMs = src.GracefulCloseTimeoutMs,
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
        dst.AutoCloseSupported = src.AutoCloseSupported;
        dst.CloseMethod = src.CloseMethod;
        dst.GracefulCloseTimeoutMs = src.GracefulCloseTimeoutMs;
        dst.WaitForProcess = src.WaitForProcess;
    }
}

