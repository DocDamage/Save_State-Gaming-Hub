using System.IO;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Core.GameLibrary.Services;
using SaveState.Core.GameLibrary.DTOs;

namespace SaveState.Core.GameLibrary.DomainServices;

public class GameImportService : IGameImportService
{
    private readonly IEnumerable<IGameProvider> _providers;

    public GameImportService(IEnumerable<IGameProvider> providers)
    {
        _providers = providers;
    }

    public async Task<Game> ImportGameFromSteamAsync(string steamAppId, CancellationToken ct = default)
    {
        var provider = _providers.FirstOrDefault(p => p.Name == "Steam");
        if (provider == null) throw new InvalidOperationException("Steam provider not available");

        var metadata = await provider.GetGameMetadataAsync(steamAppId, ct).ConfigureAwait(false);
        var game = Game.Create(metadata.Title, null, metadata.Description, metadata.CoverImageUrl, "Steam", steamAppId);

        return game;
    }

    public async Task<Game> ImportGameFromGogAsync(string gogId, CancellationToken ct = default)
    {
        var provider = _providers.FirstOrDefault(p => p.Name == "GOG");
        if (provider == null) throw new InvalidOperationException("GOG provider not available");

        var metadata = await provider.GetGameMetadataAsync(gogId, ct).ConfigureAwait(false);
        var game = Game.Create(metadata.Title, null, metadata.Description, metadata.CoverImageUrl, "GOG", gogId);

        return game;
    }

    public async Task<Game> ImportGameFromEpicAsync(string epicId, CancellationToken ct = default)
    {
        var provider = _providers.FirstOrDefault(p => p.Name == "Epic");
        if (provider == null) throw new InvalidOperationException("Epic provider not available");

        var metadata = await provider.GetGameMetadataAsync(epicId, ct).ConfigureAwait(false);
        var game = Game.Create(metadata.Title, null, metadata.Description, metadata.CoverImageUrl, "Epic", epicId);

        return game;
    }

    public async Task<Game> ImportGameFromDirectoryAsync(string gamePath, CancellationToken ct = default)
    {
        var gameTitle = Path.GetFileName(gamePath.TrimEnd(Path.DirectorySeparatorChar));
        var game = Game.Create(gameTitle);
        game.SetInstallPath(gamePath);

        return await Task.FromResult(game).ConfigureAwait(false);
    }

    public async Task<Game> ImportGameManuallyAsync(string title, string? description = null, CancellationToken ct = default)
    {
        var game = Game.Create(title, null, description);
        return await Task.FromResult(game).ConfigureAwait(false);
    }
}
