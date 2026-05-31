namespace FsBootStrapper.Core.Models;

/// <summary>Global application settings persisted across sessions.</summary>
public sealed class AppSettings
{
    /// <summary>ID of the last profile the user selected, restored on next launch.</summary>
    public Guid? LastUsedProfileId { get; set; }

    /// <summary>How MSFS 2024 is installed on this machine.</summary>
    public MsfsInstallationType MsfsInstallationType { get; set; } = MsfsInstallationType.Xbox;

    /// <summary>
    /// Full path to the MSFS executable when <see cref="MsfsInstallationType"/> is Custom.
    /// </summary>
    public string? MsfsCustomExecutablePath { get; set; }

    /// <summary>
    /// Name of the main MSFS process (without .exe) used for process detection.
    /// </summary>
    public string MsfsProcessName { get; set; } = "FlightSimulator2024";

    /// <summary>
    /// Seconds to wait for the MSFS process to appear after launch before giving up.
    /// </summary>
    public int MsfsProcessWaitTimeoutSeconds { get; set; } = 120;

    /// <summary>
    /// Seconds to poll between process-existence checks while waiting for MSFS.
    /// </summary>
    public int MsfsProcessPollingIntervalMs { get; set; } = 3_000;

    /// <summary>Whether the application window should minimise to the system tray.</summary>
    public bool MinimizeToTray { get; set; } = true;

    /// <summary>Whether to show a balloon notification when an add-on crashes.</summary>
    public bool NotifyOnAddonCrash { get; set; } = true;

    /// <summary>Whether to show a balloon notification when MSFS exits.</summary>
    public bool NotifyOnMsfsExit { get; set; } = true;
}
