namespace FsBootStrapper.Core.Models;

/// <summary>
/// Defines when and how to wait for a specific process to be running
/// before launching the next item in the sequence.
/// </summary>
public sealed class WaitCondition
{
    /// <summary>
    /// Name of the process to wait for (without .exe extension).
    /// </summary>
    public string ProcessName { get; set; } = string.Empty;

    /// <summary>
    /// Maximum seconds to wait before giving up and continuing the sequence.
    /// Use -1 to wait indefinitely.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 60;

    /// <summary>
    /// How often to check whether the process is running, in milliseconds.
    /// </summary>
    public int PollingIntervalMs { get; set; } = 2_000;
}
