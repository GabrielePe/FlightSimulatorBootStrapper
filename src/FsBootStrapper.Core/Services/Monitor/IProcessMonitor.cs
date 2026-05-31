using FsBootStrapper.Core.Models;

namespace FsBootStrapper.Core.Services.Monitor;

public sealed class ProcessStateChangedEventArgs : EventArgs
{
    public Guid ItemId { get; init; }
    public string ProcessName { get; init; } = string.Empty;
    public bool IsRunning { get; init; }
    public int? ExitCode { get; init; }
}

public interface IProcessMonitor : IDisposable
{
    event EventHandler<ProcessStateChangedEventArgs>? ProcessStateChanged;

    /// <summary>Start monitoring a process that was launched for a given item.</summary>
    void Track(Guid itemId, System.Diagnostics.Process process, LaunchItem item);

    /// <summary>Stop monitoring all tracked processes.</summary>
    void UntrackAll();

    IReadOnlyDictionary<Guid, bool> GetCurrentStates();
}
