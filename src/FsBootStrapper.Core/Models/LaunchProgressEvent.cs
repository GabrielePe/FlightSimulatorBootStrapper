namespace FsBootStrapper.Core.Models;

/// <summary>Type of progress event emitted by the launch orchestrator.</summary>
public enum LaunchEventType
{
    SequenceStarted,
    ItemWaiting,       // Waiting for a prerequisite process
    ItemLaunching,
    ItemLaunched,
    ItemFailed,
    ItemSkipped,
    MsfsLaunching,
    MsfsRunning,
    MsfsExited,
    AutoCloseRequested,
    ItemCrashed,
    ItemClosed,
    SequenceCompleted,
    SequenceFailed
}

/// <summary>Progress event emitted by <see cref="Services.Launch.ILaunchOrchestrator"/>.</summary>
public sealed class LaunchProgressEvent
{
    public LaunchEventType EventType { get; init; }
    public Guid? ItemId { get; init; }
    public string Message { get; init; } = string.Empty;
    public Exception? Exception { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
