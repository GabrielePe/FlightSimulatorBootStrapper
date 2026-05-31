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
}
