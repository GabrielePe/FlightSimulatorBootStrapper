namespace FsBootStrapper.Core.Models;

/// <summary>Information about a detected MSFS 2024 installation.</summary>
public sealed class MsfsInstallationInfo
{
    public MsfsInstallationType Type { get; init; }
    public string ExecutablePath { get; init; } = string.Empty;
    public string LaunchUri { get; init; } = string.Empty;
    public string Version { get; init; } = string.Empty;
}
