namespace FsBootStrapper.Core.Models;

/// <summary>Phase relative to MSFS launch in the startup sequence.</summary>
public enum LaunchPhase
{
    BeforeMsfs,
    AfterMsfs
}

/// <summary>Method used to close an add-on when MSFS shuts down.</summary>
public enum CloseMethod
{
    /// <summary>Send a graceful close message (CloseMainWindow). Falls back to Kill on timeout.</summary>
    GracefulClose,

    /// <summary>Immediately terminate the process (Process.Kill).</summary>
    Kill
}

/// <summary>How a configured shutdown process name is matched against running processes.</summary>
public enum ProcessMatchMode
{
    /// <summary>Exact process image name (case-insensitive, without ".exe").</summary>
    Exact,

    /// <summary>
    /// Any running process whose name contains this text (case-insensitive).
    /// Closes/kills every match - use only when sure no unrelated process on the
    /// machine shares the keyword, since this is applied to ALL matching processes.
    /// </summary>
    Keyword
}

/// <summary>How MSFS 2024 is installed on this machine.</summary>
public enum MsfsInstallationType
{
    /// <summary>Installed via Xbox App / Microsoft Store.</summary>
    Xbox,

    /// <summary>Installed via Steam.</summary>
    Steam,

    /// <summary>Custom executable path specified by the user.</summary>
    Custom
}
