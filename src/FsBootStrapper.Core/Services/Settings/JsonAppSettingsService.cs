using System.Text.Json;
using FsBootStrapper.Core.Abstractions;
using FsBootStrapper.Core.Models;
using Microsoft.Extensions.Logging;

namespace FsBootStrapper.Core.Services.Settings;

public sealed class JsonAppSettingsService : IAppSettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    private readonly string _filePath;
    private readonly IFileSystem _fs;
    private readonly ILogger<JsonAppSettingsService> _logger;

    public AppSettings Current { get; private set; } = new();

    public JsonAppSettingsService(
        IFileSystem fs,
        ILogger<JsonAppSettingsService> logger,
        string? settingsPath = null)
    {
        _fs = fs;
        _logger = logger;
        _filePath = settingsPath
            ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "FsBootStrapper", "settings.json");
        _fs.EnsureDirectoryExists(Path.GetDirectoryName(_filePath)!);
    }

    public Task LoadAsync(CancellationToken ct = default)
    {
        if (!_fs.FileExists(_filePath))
        {
            _logger.LogInformation("Settings file not found; using defaults");
            Current = new AppSettings();
            return Task.CompletedTask;
        }

        try
        {
            var json = _fs.ReadAllText(_filePath);
            Current = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
            _logger.LogInformation("Settings loaded from {Path}", _filePath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load settings from {Path}; using defaults", _filePath);
            Current = new AppSettings();
        }

        return Task.CompletedTask;
    }

    public Task SaveAsync(CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(Current, JsonOptions);
        _fs.WriteAllText(_filePath, json);
        _logger.LogInformation("Settings saved to {Path}", _filePath);
        return Task.CompletedTask;
    }
}
