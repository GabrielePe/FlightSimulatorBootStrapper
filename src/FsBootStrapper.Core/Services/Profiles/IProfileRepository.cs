using FsBootStrapper.Core.Models;

namespace FsBootStrapper.Core.Services.Profiles;

public interface IProfileRepository
{
    Task<IReadOnlyList<AppProfile>> GetAllAsync(CancellationToken ct = default);
    Task<AppProfile?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task SaveAsync(AppProfile profile, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
