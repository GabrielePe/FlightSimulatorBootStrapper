using FsBootStrapper.Core.Models;

namespace FsBootStrapper.Core.Services.Settings;

public interface IAppSettingsService
{
    AppSettings Current { get; }
    Task LoadAsync(CancellationToken ct = default);
    Task SaveAsync(CancellationToken ct = default);
}
