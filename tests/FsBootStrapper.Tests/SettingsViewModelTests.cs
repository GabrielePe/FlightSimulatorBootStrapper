using FluentAssertions;
using FsBootStrapper.Core.Models;
using FsBootStrapper.Core.Services.Detection;
using FsBootStrapper.Core.Services.Settings;
using FsBootStrapper.ViewModels;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace FsBootStrapper.Tests;

public sealed class SettingsViewModelTests
{
    private readonly IAppSettingsService _settingsService = Substitute.For<IAppSettingsService>();
    private readonly IMsfsDetectionService _detection = Substitute.For<IMsfsDetectionService>();
    private readonly ILogger<SettingsViewModel> _logger = Substitute.For<ILogger<SettingsViewModel>>();

    public SettingsViewModelTests()
    {
        _settingsService.Current.Returns(new AppSettings
        {
            MinimizeToTray = true,
            NotifyOnAddonCrash = false,
            NotifyOnMsfsExit = true,
            AutoCloseFsbsAfterMsfsExit = true
        });
        _detection.DetectInstallationsAsync().Returns(Task.FromResult<IReadOnlyList<MsfsInstallationInfo>>(Array.Empty<MsfsInstallationInfo>()));
    }

    [Fact]
    public async Task InitializeAsync_LoadsAutoCloseSetting()
    {
        var sut = new SettingsViewModel(_settingsService, _detection, _logger);

        await sut.InitializeAsync();

        sut.AutoCloseFsbsAfterMsfsExit.Should().BeTrue();
    }

    [Fact]
    public async Task SaveAsync_PersistsAutoCloseSetting()
    {
        var sut = new SettingsViewModel(_settingsService, _detection, _logger);
        await sut.InitializeAsync();

        sut.AutoCloseFsbsAfterMsfsExit = false;
        var saveMethod = typeof(SettingsViewModel).GetMethod(
            "SaveAsync",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("SaveAsync method not found");

        await (Task)saveMethod.Invoke(sut, null)!;

        _settingsService.Current.AutoCloseFsbsAfterMsfsExit.Should().BeFalse();
    }
}