using FsBootStrapper.Core.Abstractions;
using FsBootStrapper.Core.Models;
using FsBootStrapper.Core.Services.Profiles;
using Microsoft.Extensions.Logging;
using NSubstitute;
using FluentAssertions;

namespace FsBootStrapper.Tests;

public sealed class JsonProfileRepositoryTests
{
    private readonly IFileSystem _fs = Substitute.For<IFileSystem>();
    private readonly ILogger<JsonProfileRepository> _logger =
        Substitute.For<ILogger<JsonProfileRepository>>();
    private const string ProfilesDir = @"C:\fake\profiles";

    private JsonProfileRepository CreateSut() =>
        new(_fs, _logger, ProfilesDir);

    [Fact]
    public async Task GetAllAsync_WhenNoFiles_ReturnsEmpty()
    {
        _fs.GetFiles(ProfilesDir, "*.json").Returns([]);

        var sut = CreateSut();
        var result = await sut.GetAllAsync();

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task SaveAsync_SerializesProfileToFile()
    {
        var profile = new AppProfile { Name = "TestProfile" };
        string? writtenPath = null;
        string? writtenContent = null;

        _fs.When(f => f.WriteAllText(Arg.Any<string>(), Arg.Any<string>()))
           .Do(call =>
           {
               writtenPath = call.ArgAt<string>(0);
               writtenContent = call.ArgAt<string>(1);
           });

        var sut = CreateSut();
        await sut.SaveAsync(profile);

        writtenPath.Should().EndWith($"{profile.Id}.json");
        writtenContent.Should().Contain("TestProfile");
    }

    [Fact]
    public async Task GetAllAsync_DeserializesExistingProfiles()
    {
        var profile = new AppProfile { Name = "Loaded" };
        var json = System.Text.Json.JsonSerializer.Serialize(profile,
            new System.Text.Json.JsonSerializerOptions
            {
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
                Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
            });

        _fs.GetFiles(ProfilesDir, "*.json").Returns([$@"{ProfilesDir}\{profile.Id}.json"]);
        _fs.ReadAllText(Arg.Any<string>()).Returns(json);

        var sut = CreateSut();
        var result = await sut.GetAllAsync();

        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Loaded");
    }

    [Fact]
    public async Task DeleteAsync_WhenFileExists_DeletesFile()
    {
        var id = Guid.NewGuid();
        var path = $@"{ProfilesDir}\{id}.json";
        _fs.FileExists(path).Returns(true);

        var sut = CreateSut();
        await sut.DeleteAsync(id);

        _fs.Received(1).DeleteFile(path);
    }

    [Fact]
    public async Task DeleteAsync_WhenFileNotFound_DoesNotThrow()
    {
        var id = Guid.NewGuid();
        _fs.FileExists(Arg.Any<string>()).Returns(false);

        var sut = CreateSut();
        var act = () => sut.DeleteAsync(id);

        await act.Should().NotThrowAsync();
    }
}
