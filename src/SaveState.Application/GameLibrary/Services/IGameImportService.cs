using SaveState.Application.GameLibrary.DTOs;

namespace SaveState.Application.GameLibrary.Services;

public interface IGameImportService
{
    Task<ImportResult> ImportAllLibrariesAsync(
        ImportOptions options,
        IProgress<ImportProgress>? progress = null,
        CancellationToken ct = default);
}
