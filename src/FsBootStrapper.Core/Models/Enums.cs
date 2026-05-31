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
