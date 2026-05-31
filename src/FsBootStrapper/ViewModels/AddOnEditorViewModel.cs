using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FsBootStrapper.Core.Infrastructure;
using FsBootStrapper.Core.Models;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.IO;

namespace FsBootStrapper.ViewModels;

public sealed partial class AddOnEditorViewModel : ObservableObject
{
    // ── Basic fields ──────────────────────────────────────────────
    [ObservableProperty] private string _displayName = string.Empty;
    [ObservableProperty] private string _executablePath = string.Empty;
    [ObservableProperty] private string _arguments = string.Empty;
    [ObservableProperty] private LaunchPhase _phase = LaunchPhase.AfterMsfs;

    // ── Delay ─────────────────────────────────────────────────────
    /// <summary>Delay before launch expressed in seconds (UX); stored internally as ms.</summary>
    [ObservableProperty] private int _delaySeconds;
    /// <summary>Delay after launch expressed in seconds (UX); stored internally as ms.</summary>
    [ObservableProperty] private int _delayAfterSeconds;

    // ── Wait-for-process ─────────────────────────────────────────
    [ObservableProperty] private bool _waitForProcessEnabled;
    [ObservableProperty] private string _waitProcessName = string.Empty;
    /// <summary>-1 = wait indefinitely.</summary>
    [ObservableProperty] private int _waitTimeoutSeconds = 60;
    // ── Privileges ────────────────────────────────────────────
    [ObservableProperty] private bool _requiresElevation;
    // ── Shutdown ─────────────────────────────────────────────────
    [ObservableProperty] private bool _autoCloseSupported;
    [ObservableProperty] private CloseMethod _closeMethod = CloseMethod.GracefulClose;

    // ── ComboBox sources ─────────────────────────────────────────
    public LaunchPhase[] Phases { get; } = Enum.GetValues<LaunchPhase>();
    public CloseMethod[] CloseMethods { get; } = Enum.GetValues<CloseMethod>();

    /// <summary>
    /// Process name suggestions for the "Wait for" combo (populated with
    /// MSFS + executables of the other items in the current profile).
    /// </summary>
    public ObservableCollection<string> KnownProcessNames { get; } = [];

    // ── Load / Apply ─────────────────────────────────────────────
    public void LoadFrom(LaunchItem item)
    {
        DisplayName = item.DisplayName;
        ExecutablePath = item.ExecutablePath;
        Arguments = item.Arguments ?? string.Empty;
        Phase = item.Phase;
        DelaySeconds = item.DelayBeforeLaunchMs / 1000;
        DelayAfterSeconds = item.DelayAfterLaunchMs / 1000;
        RequiresElevation = item.RequiresElevation;
        AutoCloseSupported = item.AutoCloseSupported;
        CloseMethod = item.CloseMethod;

        if (item.WaitForProcess is not null)
        {
            WaitForProcessEnabled = true;
            WaitProcessName = item.WaitForProcess.ProcessName;
            WaitTimeoutSeconds = item.WaitForProcess.TimeoutSeconds;
        }
        else
        {
            WaitForProcessEnabled = false;
            WaitProcessName = string.Empty;
            WaitTimeoutSeconds = 60;
        }
    }

    public void ApplyTo(LaunchItem item)
    {
        item.DisplayName = DisplayName.Trim();
        item.ExecutablePath = ExecutablePath.Trim();
        item.Arguments = string.IsNullOrWhiteSpace(Arguments) ? null : Arguments.Trim();
        item.Phase = Phase;
        item.DelayBeforeLaunchMs = DelaySeconds * 1000;
        item.DelayAfterLaunchMs = DelayAfterSeconds * 1000;
        item.RequiresElevation = RequiresElevation;
        item.AutoCloseSupported = AutoCloseSupported;
        item.CloseMethod = CloseMethod;
        item.WaitForProcess = WaitForProcessEnabled && !string.IsNullOrWhiteSpace(WaitProcessName)
            ? new Core.Models.WaitCondition
            {
                ProcessName = WaitProcessName.Trim(),
                TimeoutSeconds = WaitTimeoutSeconds
            }
            : null;
    }

    public bool IsValid =>
        !string.IsNullOrWhiteSpace(DisplayName) &&
        !string.IsNullOrWhiteSpace(ExecutablePath);

    // ── Commands ─────────────────────────────────────────────────
    [RelayCommand]
    private void BrowseExecutable()
    {
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Select Add-on Executable",
            Filter = "Executables (*.exe)|*.exe|All files (*.*)|*.*",
            CheckFileExists = true
        };

        if (!string.IsNullOrWhiteSpace(ExecutablePath))
        {
            try { dlg.InitialDirectory = Path.GetDirectoryName(ExecutablePath); }
            catch { /* ignore invalid path */ }
        }

        if (dlg.ShowDialog() == true)
        {
            ExecutablePath = dlg.FileName;
            // Auto-fill DisplayName if still empty
            if (string.IsNullOrWhiteSpace(DisplayName))
                DisplayName = Path.GetFileNameWithoutExtension(dlg.FileName);
        }
    }

    // Auto-derive process-name suggestion from executable path
    partial void OnExecutablePathChanged(string value)
    {
        if (!WaitForProcessEnabled || !string.IsNullOrWhiteSpace(WaitProcessName)) return;
        if (!string.IsNullOrWhiteSpace(value))
            WaitProcessName = Path.GetFileNameWithoutExtension(value);
    }

    // Auto-detect UAC elevation requirement from the executable manifest
    partial void OnExecutablePathChanging(string value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            RequiresElevation = ExecutableManifestReader.RequiresElevation(value);
    }
}
