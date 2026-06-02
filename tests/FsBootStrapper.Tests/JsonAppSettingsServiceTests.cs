using FsBootStrapper.Core.Abstractions;
using FsBootStrapper.Core.Models;
using FsBootStrapper.Core.Services.Settings;
using Microsoft.Extensions.Logging;
using NSubstitute;
using FluentAssertions;

namespace FsBootStrapper.Tests;

public sealed class JsonAppSettingsServiceTests
{
    private readonly IFileSystem _fs = Substitute.For<IFileSystem>();
    private readonly ILogger<JsonAppSettingsService> _logger =
        Substitute.For<ILogger<JsonAppSettingsService>>();
    private const string SettingsPath = @"C:\fake\settings.json";

    private JsonAppSettingsService CreateSut() =>
        new(_fs, _logger, SettingsPath);

    [Fact]
    public async Task LoadAsync_WhenFileMissing_UsesDefaults()
    {
        _fs.FileExists(SettingsPath).Returns(false);

        var sut = CreateSut();
        await sut.LoadAsync();

        sut.Current.Should().NotBeNull();
        sut.Current.MsfsProcessName.Should().Be("FlightSimulator2024");
        sut.Current.AutoCloseFsbsAfterMsfsExit.Should().BeFalse();
    }

    [Fact]
    public async Task LoadAsync_WhenFileExists_DeserializesSettings()
    {
        var settings = new AppSettings { MsfsProcessName = "CustomProcess" };
        var json = System.Text.Json.JsonSerializer.Serialize(settings,
            new System.Text.Json.JsonSerializerOptions
            {
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
                Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
            });

        _fs.FileExists(SettingsPath).Returns(true);
        _fs.ReadAllText(SettingsPath).Returns(json);

        var sut = CreateSut();
        await sut.LoadAsync();

        sut.Current.MsfsProcessName.Should().Be("CustomProcess");
    }

    [Fact]
    public async Task SaveAsync_WritesSerializedSettings()
    {
        string? writtenContent = null;
        _fs.When(f => f.WriteAllText(SettingsPath, Arg.Any<string>()))
           .Do(call => writtenContent = call.ArgAt<string>(1));

        var sut = CreateSut();
        sut.Current.MsfsProcessName = "AnotherProcess";
        sut.Current.AutoCloseFsbsAfterMsfsExit = true;
        await sut.SaveAsync();

        writtenContent.Should().Contain("AnotherProcess");
        writtenContent.Should().Contain("autoCloseFsbsAfterMsfsExit");
    }

    [Fact]
    public async Task LoadAsync_WhenJsonCorrupt_FallsBackToDefaults()
    {
        _fs.FileExists(SettingsPath).Returns(true);
        _fs.ReadAllText(SettingsPath).Returns("not valid json {{{{");

        var sut = CreateSut();
        await sut.LoadAsync();

        sut.Current.Should().NotBeNull();
        sut.Current.MsfsProcessName.Should().Be("FlightSimulator2024");
    }
}
