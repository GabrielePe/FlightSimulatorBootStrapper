using FsBootStrapper.Core.Models;

namespace FsBootStrapper.Core.Services.Detection;

public interface IMsfsDetectionService
{
    /// <summary>
    /// Scans for Xbox/MS Store and Steam installations of MSFS 2024.
    /// Returns all found installations (may be empty).
    /// </summary>
    Task<IReadOnlyList<MsfsInstallationInfo>> DetectInstallationsAsync(CancellationToken ct = default);
}
