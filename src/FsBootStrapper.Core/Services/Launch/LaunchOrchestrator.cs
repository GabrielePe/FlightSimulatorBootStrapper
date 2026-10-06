using System.Collections.Concurrent;
using System.Diagnostics;
using System.Security.Principal;
using FsBootStrapper.Core.Infrastructure;
using FsBootStrapper.Core.Models;
using FsBootStrapper.Core.Services.Monitor;
using Microsoft.Extensions.Logging;

namespace FsBootStrapper.Core.Services.Launch;

public sealed class LaunchOrchestrator : ILaunchOrchestrator
{
    private readonly IProcessMonitor _monitor;
    private readonly ILogger<LaunchOrchestrator> _logger;

    // Tracks running processes keyed by LaunchItem.Id for shutdown management
    private readonly ConcurrentDictionary<Guid, (Process Process, LaunchItem Item)> _running = new();
    private Process? _msfsProcess;
    private IProgress<LaunchProgressEvent>? _progress;
    private volatile bool _isLaunching;

    public bool IsSessionActive => !_running.IsEmpty;
    public bool IsLaunching => _isLaunching;

    public LaunchOrchestrator(IProcessMonitor monitor, ILogger<LaunchOrchestrator> logger)
    {
        _monitor = monitor;
        _logger = logger;
    }

    public async Task LaunchAsync(
        AppProfile profile,
        MsfsInstallationInfo msfsInstallation,
        AppSettings settings,
        IProgress<LaunchProgressEvent> progress,
        CancellationToken ct = default)
    {
        _isLaunching = true;
        _running.Clear();
        _msfsProcess = null;
        _progress = progress;

        progress.Report(new LaunchProgressEvent
        {
            EventType = LaunchEventType.SequenceStarted,
            Message = $"Starting profile '{profile.Name}'"
        });
        _logger.LogInformation("Launch sequence started for profile '{Profile}'", profile.Name);

        try
        {
            // Phase 1: Before MSFS
            foreach (var item in profile.GetItemsForPhase(LaunchPhase.BeforeMsfs))
            {
                ct.ThrowIfCancellationRequested();
                await LaunchItemAsync(item, progress, ct);
            }

            // Phase 2: Launch MSFS
            await LaunchMsfsAsync(msfsInstallation, profile, settings, progress, ct);

            // Phase 3: After MSFS
            foreach (var item in profile.GetItemsForPhase(LaunchPhase.AfterMsfs))
            {
                ct.ThrowIfCancellationRequested();
                await LaunchItemAsync(item, progress, ct);
            }

            // Phase 4: Monitor MSFS for exit → trigger auto-close
            _ = MonitorMsfsExitAsync(settings, _msfsProcess, progress, ct);

            progress.Report(new LaunchProgressEvent
            {
                EventType = LaunchEventType.SequenceCompleted,
                Message = "All items launched successfully"
            });
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Launch sequence cancelled");
            progress.Report(new LaunchProgressEvent
            {
                EventType = LaunchEventType.SequenceFailed,
                Message = "Launch cancelled by user"
            });
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Launch sequence failed");
            progress.Report(new LaunchProgressEvent
            {
                EventType = LaunchEventType.SequenceFailed,
                Message = ex.Message,
                Exception = ex
            });
            throw;
        }
        finally
        {
            _isLaunching = false;
        }
    }

    private async Task LaunchItemAsync(
        LaunchItem item,
        IProgress<LaunchProgressEvent> progress,
        CancellationToken ct)
    {
        // Apply pre-launch delay
        if (item.DelayBeforeLaunchMs > 0)
        {
            _logger.LogDebug("Waiting {Delay}ms before launching '{Item}'", item.DelayBeforeLaunchMs, item.DisplayName);
            await Task.Delay(item.DelayBeforeLaunchMs, ct);
        }

        // Wait for prerequisite process
        if (item.WaitForProcess is not null)
        {
            progress.Report(new LaunchProgressEvent
            {
                EventType = LaunchEventType.ItemWaiting,
                ItemId = item.Id,
                Message = $"Waiting for process '{item.WaitForProcess.ProcessName}'..."
            });

            bool found = await WaitForProcessAsync(item.WaitForProcess, ct);
            if (!found)
            {
                _logger.LogWarning("Timed out waiting for '{Process}' before launching '{Item}'",
                    item.WaitForProcess.ProcessName, item.DisplayName);
            }
        }

        progress.Report(new LaunchProgressEvent
        {
            EventType = LaunchEventType.ItemLaunching,
            ItemId = item.Id,
            Message = $"Launching '{item.DisplayName}'..."
        });

        try
        {
            var workingDir = Path.GetDirectoryName(item.ExecutablePath) ?? string.Empty;
            var arguments = item.Arguments ?? string.Empty;

            // If FSBS is elevated but this item does NOT require elevation,
            // de-elevate it to Medium IL so it can communicate with MSFS (Xbox/Store).
            bool isElevated = new WindowsPrincipal(WindowsIdentity.GetCurrent())
                .IsInRole(WindowsBuiltInRole.Administrator);

            Process process;
            if (isElevated && !item.RequiresElevation)
            {
                process = DeElevatedLauncher.Start(item.ExecutablePath, arguments, workingDir);
            }
            else
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = item.ExecutablePath,
                    Arguments = arguments,
                    UseShellExecute = true,
                    WorkingDirectory = workingDir
                };
                process = Process.Start(startInfo)
                    ?? throw new InvalidOperationException($"Process.Start returned null for '{item.ExecutablePath}'");
            }

            _running[item.Id] = (process, item);
            _monitor.Track(item.Id, process, item);

            _logger.LogInformation("Launched '{Name}' (PID {Pid})", item.DisplayName, process.Id);
            progress.Report(new LaunchProgressEvent
            {
                EventType = LaunchEventType.ItemLaunched,
                ItemId = item.Id,
                Message = $"'{item.DisplayName}' is running (PID {process.Id})"
            });

            // Apply post-launch delay before the next item starts
            if (item.DelayAfterLaunchMs > 0)
            {
                _logger.LogDebug("Waiting {Delay}ms after launching '{Item}'", item.DelayAfterLaunchMs, item.DisplayName);
                await Task.Delay(item.DelayAfterLaunchMs, ct);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to launch '{Name}'", item.DisplayName);
            progress.Report(new LaunchProgressEvent
            {
                EventType = LaunchEventType.ItemFailed,
                ItemId = item.Id,
                Message = $"Failed to launch '{item.DisplayName}': {ex.Message}",
                Exception = ex
            });
            throw;
        }
    }

    private async Task LaunchMsfsAsync(
        MsfsInstallationInfo msfs,
        AppProfile profile,
        AppSettings settings,
        IProgress<LaunchProgressEvent> progress,
        CancellationToken ct)
    {
        progress.Report(new LaunchProgressEvent
        {
            EventType = LaunchEventType.MsfsLaunching,
            Message = $"Launching MSFS 2024 ({msfs.Type})..."
        });
        _logger.LogInformation("Launching MSFS via {Type}, URI/path: {Launch}", msfs.Type, msfs.LaunchUri);

        var startInfo = msfs.Type switch
        {
            MsfsInstallationType.Xbox => new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = msfs.LaunchUri,
                UseShellExecute = true
            },
            MsfsInstallationType.Steam => new ProcessStartInfo
            {
                FileName = msfs.LaunchUri,
                UseShellExecute = true
            },
            MsfsInstallationType.Custom => new ProcessStartInfo
            {
                FileName = msfs.ExecutablePath,
                UseShellExecute = false
            },
            _ => throw new InvalidOperationException($"Unknown MSFS installation type: {msfs.Type}")
        };

        Process.Start(startInfo);

        // Wait for the MSFS process to appear
        _logger.LogInformation("Waiting for MSFS process '{Name}' to appear...", settings.MsfsProcessName);
        var deadline = DateTime.UtcNow.AddSeconds(settings.MsfsProcessWaitTimeoutSeconds);

        while (DateTime.UtcNow < deadline)
        {
            ct.ThrowIfCancellationRequested();
            var procs = Process.GetProcessesByName(settings.MsfsProcessName);
            if (procs.Length > 0)
            {
                _msfsProcess = procs[0];
                _msfsProcess.EnableRaisingEvents = true;
                // Dispose extra instances if duplicated
                foreach (var p in procs.Skip(1)) p.Dispose();

                _logger.LogInformation("MSFS process found (PID {Pid})", _msfsProcess.Id);
                progress.Report(new LaunchProgressEvent
                {
                    EventType = LaunchEventType.MsfsRunning,
                    Message = $"MSFS 2024 is running (PID {_msfsProcess.Id})"
                });
                return;
            }
            await Task.Delay(settings.MsfsProcessPollingIntervalMs, ct);
        }

        _logger.LogWarning("MSFS process did not appear within timeout; continuing anyway");
    }

    private async Task MonitorMsfsExitAsync(
        AppSettings settings,
        Process? msfsProcess,
        IProgress<LaunchProgressEvent> progress,
        CancellationToken ct)
    {
        try
        {
            if (msfsProcess is null)
            {
                _logger.LogInformation("Waiting for MSFS process '{Name}' to become available for exit monitoring...", settings.MsfsProcessName);

                while (!ct.IsCancellationRequested)
                {
                    var procs = Process.GetProcessesByName(settings.MsfsProcessName);
                    if (procs.Length > 0)
                    {
                        msfsProcess = procs[0];
                        msfsProcess.EnableRaisingEvents = true;

                        foreach (var p in procs.Skip(1))
                        {
                            p.Dispose();
                        }

                        break;
                    }

                    await Task.Delay(settings.MsfsProcessPollingIntervalMs, ct);
                }
            }

            if (msfsProcess is null)
            {
                return;
            }

            await msfsProcess.WaitForExitAsync(ct);
        }
        catch (OperationCanceledException) { return; }

        _logger.LogInformation("MSFS exited. Starting auto-close sequence");
        progress.Report(new LaunchProgressEvent
        {
            EventType = LaunchEventType.MsfsExited,
            Message = "MSFS 2024 has exited. Closing add-ons..."
        });

        var allClosedCleanly = await StopAllAsync(ct);
        if (allClosedCleanly && settings.AutoCloseFsbsAfterMsfsExit)
        {
            progress.Report(new LaunchProgressEvent
            {
                EventType = LaunchEventType.AutoCloseRequested,
                Message = "All add-ons closed cleanly. Closing FsBootStrapper..."
            });
        }
        else if (settings.AutoCloseFsbsAfterMsfsExit)
        {
            _logger.LogWarning("Auto-close skipped because one or more add-ons did not close cleanly");
        }
    }

    public async Task<bool> StopAllAsync(CancellationToken ct = default)
    {
        var allClosedCleanly = true;

        foreach (var (id, (process, item)) in _running)
        {
            if (item.AutoCloseSupported) continue;

            try
            {
                if (process.HasExited) continue;

                if (item.CloseMethod == CloseMethod.GracefulClose)
                {
                    var timeoutSec = item.GracefulCloseTimeoutMs / 1_000.0;

                    // Signal every top-level window owned by the process (not just the single
                    // "main" one .NET's CloseMainWindow() guesses at), so tray-only add-ons with
                    // no visible taskbar window still receive a close request.
                    int windowsSignaled = NativeWindowCloser.CloseAllWindows(process.Id);

                    if (windowsSignaled == 0)
                    {
                        // Nothing to close gracefully (no window at all) — waiting out the
                        // timeout would just delay the kill for no benefit.
                        _logger.LogWarning(
                            "'{Name}' exposes no window to close gracefully; killing immediately", item.DisplayName);
                        _progress?.Report(new LaunchProgressEvent
                        {
                            EventType = LaunchEventType.ItemClosed,
                            ItemId = item.Id,
                            Message = $"'{item.DisplayName}' has no window to close — forcing kill"
                        });
                        process.Kill();
                    }
                    else
                    {
                        _progress?.Report(new LaunchProgressEvent
                        {
                            EventType = LaunchEventType.ItemClosed,
                            ItemId = item.Id,
                            Message = $"Requesting graceful close of '{item.DisplayName}' (timeout: {timeoutSec:0.#}s)..."
                        });

                        bool exited = await Task.Run(
                            () => process.WaitForExit(item.GracefulCloseTimeoutMs), ct);

                        if (!exited)
                        {
                            _logger.LogWarning(
                                "'{Name}' did not close gracefully; killing", item.DisplayName);
                            _progress?.Report(new LaunchProgressEvent
                            {
                                EventType = LaunchEventType.ItemClosed,
                                ItemId = item.Id,
                                Message = $"'{item.DisplayName}' did not respond within {timeoutSec:0.#}s — forcing kill"
                            });
                            process.Kill();
                        }
                    }
                }
                else
                {
                    _progress?.Report(new LaunchProgressEvent
                    {
                        EventType = LaunchEventType.ItemClosed,
                        ItemId = item.Id,
                        Message = $"Killing '{item.DisplayName}'..."
                    });
                    process.Kill();
                }

                _logger.LogInformation("Closed '{Name}'", item.DisplayName);
                _progress?.Report(new LaunchProgressEvent
                {
                    EventType = LaunchEventType.ItemClosed,
                    ItemId = item.Id,
                    Message = $"'{item.DisplayName}' closed"
                });
            }
            catch (Exception ex)
            {
                allClosedCleanly = false;
                _logger.LogWarning(ex, "Error closing '{Name}'", item.DisplayName);
                _progress?.Report(new LaunchProgressEvent
                {
                    EventType = LaunchEventType.ItemFailed,
                    ItemId = item.Id,
                    Message = $"Error closing '{item.DisplayName}': {ex.Message}",
                    Exception = ex
                });
            }
        }

        _running.Clear();
        _monitor.UntrackAll();
        return allClosedCleanly;
    }

    private static async Task<bool> WaitForProcessAsync(WaitCondition condition, CancellationToken ct)
    {
        var deadline = condition.TimeoutSeconds < 0
            ? DateTime.MaxValue
            : DateTime.UtcNow.AddSeconds(condition.TimeoutSeconds);

        while (DateTime.UtcNow < deadline)
        {
            ct.ThrowIfCancellationRequested();
            if (Process.GetProcessesByName(condition.ProcessName).Length > 0)
                return true;
            await Task.Delay(condition.PollingIntervalMs, ct);
        }
        return false;
    }
}
