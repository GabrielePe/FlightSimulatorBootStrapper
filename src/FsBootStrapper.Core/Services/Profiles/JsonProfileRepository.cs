using System.Text.Json;
using FsBootStrapper.Core.Abstractions;
using FsBootStrapper.Core.Models;
using Microsoft.Extensions.Logging;

namespace FsBootStrapper.Core.Services.Profiles;

public sealed class JsonProfileRepository : IProfileRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    private readonly string _profilesDir;
    private readonly IFileSystem _fs;
    private readonly ILogger<JsonProfileRepository> _logger;

    public JsonProfileRepository(
        IFileSystem fs,
        ILogger<JsonProfileRepository> logger,
        string? profilesDir = null)
    {
        _fs = fs;
        _logger = logger;
        _profilesDir = profilesDir
            ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "FsBootStrapper", "profiles");
        _fs.EnsureDirectoryExists(_profilesDir);
    }

    private string FilePath(Guid id) => Path.Combine(_profilesDir, $"{id}.json");

    public Task<IReadOnlyList<AppProfile>> GetAllAsync(CancellationToken ct = default)
    {
        var profiles = new List<AppProfile>();
        foreach (var file in _fs.GetFiles(_profilesDir, "*.json"))
        {
            try
            {
                var json = _fs.ReadAllText(file);
                var profile = JsonSerializer.Deserialize<AppProfile>(json, JsonOptions);
                if (profile is not null)
                    profiles.Add(profile);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize profile from {File}", file);
            }
        }
        return Task.FromResult<IReadOnlyList<AppProfile>>(profiles);
    }

    public async Task<AppProfile?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var path = FilePath(id);
        if (!_fs.FileExists(path))
            return null;
        var all = await GetAllAsync(ct);
        return all.FirstOrDefault(p => p.Id == id);
    }

    public Task SaveAsync(AppProfile profile, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(profile);
        profile.UpdatedAt = DateTime.UtcNow;
        var json = JsonSerializer.Serialize(profile, JsonOptions);
        _fs.WriteAllText(FilePath(profile.Id), json);
        _logger.LogInformation("Profile '{Name}' ({Id}) saved", profile.Name, profile.Id);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var path = FilePath(id);
        if (_fs.FileExists(path))
        {
            _fs.DeleteFile(path);
            _logger.LogInformation("Profile {Id} deleted", id);
        }
        return Task.CompletedTask;
    }
}
