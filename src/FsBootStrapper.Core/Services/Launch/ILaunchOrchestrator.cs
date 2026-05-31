using FsBootStrapper.Core.Models;

namespace FsBootStrapper.Core.Services.Launch;

public interface ILaunchOrchestrator
{
    /// <summary>
    /// Executes the full launch sequence for the given profile, reporting progress
    /// through <paramref name="progress"/>.
    /// </summary>
    Task LaunchAsync(
        AppProfile profile,
        MsfsInstallationInfo msfsInstallation,
        AppSettings settings,
        IProgress<LaunchProgressEvent> progress,
        CancellationToken ct = default);

    /// <summary>
    /// Requests a graceful stop of all tracked processes.
    /// </summary>
    Task StopAllAsync(CancellationToken ct = default);
}
