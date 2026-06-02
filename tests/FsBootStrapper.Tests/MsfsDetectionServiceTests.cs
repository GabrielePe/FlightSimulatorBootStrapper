using FsBootStrapper.Core.Abstractions;
using FsBootStrapper.Core.Models;
using FsBootStrapper.Core.Services.Detection;
using FsBootStrapper.Core.Services.Settings;
using Microsoft.Extensions.Logging;
using NSubstitute;
using FluentAssertions;

namespace FsBootStrapper.Tests;

public sealed class MsfsDetectionServiceTests
{
    private readonly IRegistryReader _registry = Substitute.For<IRegistryReader>();
    private readonly IFileSystem _fs = Substitute.For<IFileSystem>();
    private readonly IAppSettingsService _settings = Substitute.For<IAppSettingsService>();
    private readonly ILogger<MsfsDetectionService> _logger =
        Substitute.For<ILogger<MsfsDetectionService>>();

    public MsfsDetectionServiceTests()
    {
        _settings.Current.Returns(new AppSettings());
    }

    private MsfsDetectionService CreateSut() =>
        new(_registry, _fs, _settings, _logger);

    [Fact]
    public async Task DetectInstallationsAsync_WhenSteamNotInstalled_DoesNotReturnSteam()
    {
        _registry.GetLocalMachineValue(
            Arg.Any<string>(), Arg.Any<string>()).Returns((string?)null);

        var sut = CreateSut();
        var result = await sut.DetectInstallationsAsync();

        result.Should().NotContain(i => i.Type == MsfsInstallationType.Steam);
    }

    [Fact]
    public async Task DetectInstallationsAsync_WhenSteamRegistryExistsButExeAbsent_DoesNotReturnSteam()
    {
        _registry.GetLocalMachineValue(
            @"SOFTWARE\WOW6432Node\Valve\Steam", "InstallPath")
            .Returns(@"C:\Steam");

        _fs.FileExists(Arg.Any<string>()).Returns(false);

        var sut = CreateSut();
        var result = await sut.DetectInstallationsAsync();

        result.Should().NotContain(i => i.Type == MsfsInstallationType.Steam);
    }

    [Fact]
    public async Task DetectInstallationsAsync_WhenSteamExePresent_ReturnsSteamInstallation()
    {
        _registry.GetLocalMachineValue(
            @"SOFTWARE\WOW6432Node\Valve\Steam", "InstallPath")
            .Returns(@"C:\Steam");

        var expectedExe = @"C:\Steam\steamapps\common\Microsoft Flight Simulator 2024\FlightSimulator.exe";
        _fs.FileExists(expectedExe).Returns(true);

        var sut = CreateSut();
        var result = await sut.DetectInstallationsAsync();

        var steam = result.FirstOrDefault(i => i.Type == MsfsInstallationType.Steam);
        steam.Should().NotBeNull();
        steam!.ExecutablePath.Should().Be(expectedExe);
        steam.LaunchUri.Should().Contain("2537750");
    }
}
