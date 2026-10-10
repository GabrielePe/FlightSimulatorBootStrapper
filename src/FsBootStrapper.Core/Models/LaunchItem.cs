using System.ComponentModel.DataAnnotations;

namespace FsBootStrapper.Core.Models;

/// <summary>A single executable entry within a launch profile.</summary>
public sealed class LaunchItem
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, MaxLength(200)]
    public string DisplayName { get; set; } = string.Empty;

    [Required]
    public string ExecutablePath { get; set; } = string.Empty;

    /// <summary>Optional command-line arguments passed to the executable.</summary>
    public string? Arguments { get; set; }

    /// <summary>Whether this item launches before or after MSFS.</summary>
    public LaunchPhase Phase { get; set; } = LaunchPhase.AfterMsfs;

    /// <summary>Zero-based position within the phase. Lower numbers launch first.</summary>
    public int SortOrder { get; set; }

    /// <summary>Milliseconds to wait before launching this item. 0 = no delay.</summary>
    public int DelayBeforeLaunchMs { get; set; }

    /// <summary>
    /// Milliseconds to wait after this item has launched before the next item
    /// (or MSFS itself) is started. Summed with the next item's DelayBeforeLaunchMs.
    /// 0 = no delay.
    /// </summary>
    public int DelayAfterLaunchMs { get; set; }

    /// <summary>
    /// If set, the orchestrator will poll until this process is running
    /// before launching this item.
    /// </summary>
    public WaitCondition? WaitForProcess { get; set; }

    /// <summary>
    /// True if the add-on handles its own shutdown (e.g. auto-closes when MSFS exits).
    /// FSBS will not attempt to close it.
    /// </summary>
    public bool AutoCloseSupported { get; set; }

    /// <summary>How FSBS should close this item when AutoCloseSupported is false.</summary>
    public CloseMethod CloseMethod { get; set; } = CloseMethod.GracefulClose;

    /// <summary>Timeout in milliseconds for graceful close before falling back to Kill.</summary>
    public int GracefulCloseTimeoutMs { get; set; } = 5_000;

    /// <summary>
    /// Optional: process image name (without .exe) FSBS should actually look for and
    /// close when MSFS exits. Use this when ExecutablePath is a launcher/updater stub
    /// that spawns the real application under a different process - the stub's own
    /// PID is useless for closing since it has typically already exited by then.
    /// Resolved against currently-running processes at shutdown time, independent of
    /// the PID returned when the item was launched.
    /// </summary>
    public string? ShutdownProcessName { get; set; }

    /// <summary>
    /// How ShutdownProcessName is matched. Exact targets the single process with that
    /// exact name; Keyword closes every running process whose name contains the text
    /// - powerful but the user's responsibility to scope correctly.
    /// </summary>
    public ProcessMatchMode ShutdownProcessMatchMode { get; set; } = ProcessMatchMode.Exact;

    /// <summary>
    /// Whether this add-on requires administrator privileges to run.
    /// When true and FSBS is already elevated, the process inherits the elevated token (no UAC prompt).
    /// When false and FSBS is elevated, the process is de-elevated to medium integrity
    /// so it can communicate with non-elevated processes (e.g. MSFS via Xbox App).
    /// </summary>
    public bool RequiresElevation { get; set; }
}
