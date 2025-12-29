using SaveState.Core.GameLibrary.Entities;

namespace SaveState.Core.GameLibrary.DomainServices;

public interface IGameImportService
{
    Task<Game> ImportGameFromSteamAsync(string steamAppId, CancellationToken ct = default);
    Task<Game> ImportGameFromGogAsync(string gogId, CancellationToken ct = default);
    Task<Game> ImportGameFromEpicAsync(string epicId, CancellationToken ct = default);
    Task<Game> ImportGameFromDirectoryAsync(string gamePath, CancellationToken ct = default);
    Task<Game> ImportGameManuallyAsync(string title, string? description = null, CancellationToken ct = default);
}
