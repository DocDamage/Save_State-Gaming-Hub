using SaveState.Core.GameLibrary.DTOs;

namespace SaveState.Core.GameLibrary.Services;

public interface IGameProvider
{
    string Name { get; }
    ProviderCapabilities Capabilities { get; }

    Task<IReadOnlyList<GameInfo>> GetInstalledGamesAsync(CancellationToken ct = default);
    Task<GameMetadata> GetGameMetadataAsync(string gameId, CancellationToken ct = default);
    Task<bool> LaunchGameAsync(string gameId, CancellationToken ct = default);
}

[Flags]
public enum ProviderCapabilities
{
    None = 0,
    Discovery = 1,
    Metadata = 2,
    Launch = 4,
    All = Discovery | Metadata | Launch
}
