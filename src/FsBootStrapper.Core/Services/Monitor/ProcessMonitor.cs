using System.Collections.Concurrent;
using System.Diagnostics;
using FsBootStrapper.Core.Models;
using Microsoft.Extensions.Logging;

namespace FsBootStrapper.Core.Services.Monitor;

public sealed class ProcessMonitor : IProcessMonitor
{
    private sealed record TrackedEntry(Guid ItemId, Process Process, LaunchItem Item);

    private readonly ConcurrentDictionary<Guid, TrackedEntry> _tracked = new();
    private readonly ILogger<ProcessMonitor> _logger;
    private bool _disposed;

    public event EventHandler<ProcessStateChangedEventArgs>? ProcessStateChanged;

    public ProcessMonitor(ILogger<ProcessMonitor> logger)
    {
        _logger = logger;
    }

    public void Track(Guid itemId, Process process, LaunchItem item)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        process.EnableRaisingEvents = true;
        process.Exited += (_, _) => OnProcessExited(itemId, process, item);

        _tracked[itemId] = new TrackedEntry(itemId, process, item);
        _logger.LogDebug("Tracking process {Name} (PID {Pid}) for item {ItemId}",
            item.DisplayName, process.Id, itemId);
    }

    private void OnProcessExited(Guid itemId, Process process, LaunchItem item)
    {
        int? exitCode = null;
        try { exitCode = process.ExitCode; } catch { /* process may already be gone */ }

        _tracked.TryRemove(itemId, out _);
        _logger.LogInformation(
            "Process '{Name}' exited with code {ExitCode}", item.DisplayName, exitCode);

        ProcessStateChanged?.Invoke(this, new ProcessStateChangedEventArgs
        {
            ItemId = itemId,
            ProcessName = item.DisplayName,
            IsRunning = false,
            ExitCode = exitCode
        });
    }

    public void UntrackAll()
    {
        _tracked.Clear();
    }

    public IReadOnlyDictionary<Guid, bool> GetCurrentStates()
    {
        var result = new Dictionary<Guid, bool>();
        foreach (var (id, entry) in _tracked)
        {
            bool running;
            try { running = !entry.Process.HasExited; }
            catch { running = false; }
            result[id] = running;
        }
        return result;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _tracked.Clear();
    }
}
