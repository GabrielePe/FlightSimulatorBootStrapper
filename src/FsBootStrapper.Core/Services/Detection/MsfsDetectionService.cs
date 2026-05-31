using FsBootStrapper.Core.Abstractions;
using FsBootStrapper.Core.Models;
using Microsoft.Extensions.Logging;

namespace FsBootStrapper.Core.Services.Detection;

public sealed class MsfsDetectionService : IMsfsDetectionService
{
    // MSFS 2024 Steam App ID
    private const string SteamAppId = "2537750";
    private const string SteamRegistryKey = @"SOFTWARE\WOW6432Node\Valve\Steam";
    private const string SteamInstallPathValue = "InstallPath";
    private const string Msfs2024SteamRelativePath = @"steamapps\common\Microsoft Flight Simulator 2024\FlightSimulator.exe";

    // Xbox / MS Store package family name
    private const string XboxPackageFamilyName = "Microsoft.Limitless_8wekyb3d8bbwe";
    private const string XboxLaunchUri = $"shell:AppsFolder\\{XboxPackageFamilyName}!App";

    private readonly IRegistryReader _registry;
    private readonly IFileSystem _fs;
    private readonly ILogger<MsfsDetectionService> _logger;

    public MsfsDetectionService(
        IRegistryReader registry,
        IFileSystem fs,
        ILogger<MsfsDetectionService> logger)
    {
        _registry = registry;
        _fs = fs;
        _logger = logger;
    }

    public Task<IReadOnlyList<MsfsInstallationInfo>> DetectInstallationsAsync(CancellationToken ct = default)
    {
        var results = new List<MsfsInstallationInfo>();

        var steam = TryDetectSteam();
        if (steam is not null) results.Add(steam);

        var xbox = TryDetectXbox();
        if (xbox is not null) results.Add(xbox);

        _logger.LogInformation("MSFS detection complete. Found {Count} installation(s)", results.Count);
        return Task.FromResult<IReadOnlyList<MsfsInstallationInfo>>(results);
    }

    private MsfsInstallationInfo? TryDetectSteam()
    {
        try
        {
            var steamInstallPath = _registry.GetLocalMachineValue(SteamRegistryKey, SteamInstallPathValue);
            if (string.IsNullOrWhiteSpace(steamInstallPath))
            {
                _logger.LogDebug("Steam not found in registry");
                return null;
            }

            var exePath = Path.Combine(steamInstallPath, Msfs2024SteamRelativePath);
            if (!_fs.FileExists(exePath))
            {
                _logger.LogDebug("Steam MSFS 2024 executable not found at {Path}", exePath);
                return null;
            }

            _logger.LogInformation("Steam MSFS 2024 detected at {Path}", exePath);
            return new MsfsInstallationInfo
            {
                Type = MsfsInstallationType.Steam,
                ExecutablePath = exePath,
                LaunchUri = $"steam://rungameid/{SteamAppId}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error while detecting Steam installation");
            return null;
        }
    }

    private MsfsInstallationInfo? TryDetectXbox()
    {
        try
        {
            // Use Windows.Management.Deployment to find the package
            var manager = new Windows.Management.Deployment.PackageManager();
            var packages = manager.FindPackagesForUser(
                string.Empty,
                XboxPackageFamilyName);

            var package = packages?.FirstOrDefault();
            if (package is null)
            {
                _logger.LogDebug("Xbox/MS Store MSFS 2024 package not found");
                return null;
            }

            var installPath = package.InstalledLocation?.Path ?? string.Empty;
            var exePath = Path.Combine(installPath, "FlightSimulator.exe");

            _logger.LogInformation("Xbox MSFS 2024 detected. Package path: {Path}", installPath);
            return new MsfsInstallationInfo
            {
                Type = MsfsInstallationType.Xbox,
                ExecutablePath = exePath,
                LaunchUri = XboxLaunchUri,
                Version = package.Id.Version.Major + "." + package.Id.Version.Minor
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error while detecting Xbox/MS Store installation");
            return null;
        }
    }
}
