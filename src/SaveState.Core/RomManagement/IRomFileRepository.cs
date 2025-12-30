using SaveState.Core.Common;
using SaveState.Core.Common.ValueObjects;
using SaveState.Core.RomManagement.Entities;

namespace SaveState.Core.RomManagement;

public interface IRomFileRepository
{
    Task<Entities.RomFile?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Entities.RomFile>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Entities.RomFile>> GetByPlatformIdAsync(Guid platformId, CancellationToken ct = default);
    Task<IReadOnlyList<Entities.RomFile>> GetByFolderPathAsync(string folderPath, Guid platformId, CancellationToken ct = default);
    Task<PagedResult<Entities.RomFile>> GetRomFilesAsync(
        int pageNumber = 1,
        int pageSize = 100,
        Guid? platformId = null,
        string? folderPath = null,
        CancellationToken ct = default);
    Task<int> CountAsync(Guid? platformId = null, CancellationToken ct = default);
    Task AddAsync(Entities.RomFile romFile, CancellationToken ct = default);
    Task UpdateAsync(Entities.RomFile romFile, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
