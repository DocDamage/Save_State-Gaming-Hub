using Microsoft.Extensions.Logging;
using SaveState.Core.GameLibrary.Services;
using SaveState.Core.GameLibrary.DTOs;

namespace SaveState.Infrastructure.External;

public class EpicProvider : IGameProvider
{
    private readonly IEpicApiClient _apiClient;
    private readonly ILogger<EpicProvider> _logger;

    public string Name => "Epic Games";
    public ProviderCapabilities Capabilities => ProviderCapabilities.All;

    public EpicProvider(IEpicApiClient apiClient, ILogger<EpicProvider> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    public async Task<IReadOnlyList<GameInfo>> GetInstalledGamesAsync(CancellationToken ct = default)
    {
        try
        {
            var epicGamesResult = await _apiClient.GetOwnedGamesAsync(ct).ConfigureAwait(false);
            if (epicGamesResult.IsFailure || epicGamesResult.Value is null)
            {
                _logger.LogWarning("Failed to get Epic owned games: {Error}", epicGamesResult.Error);
                return Array.Empty<GameInfo>();
            }

            var epicGames = epicGamesResult.Value;
            return epicGames.Select(g => new GameInfo
            {
                Source = "Epic",
                SourceId = g.Id,
                Title = g.Title,
                InstallPath = g.InstallPath,
                LastPlayed = g.LastPlayedDate,
                PlayTimeMinutes = g.PlayTimeMinutes,
                Platform = "PC"
            }).ToList();
        }
        catch (EpicApiException ex)
        {
            _logger.LogWarning(ex, "Failed to get Epic games");
            return Array.Empty<GameInfo>();
        }
    }

    public async Task<GameMetadata> GetGameMetadataAsync(string gameId, CancellationToken ct = default)
    {
        var metadataResult = await _apiClient.GetGameDetailsAsync(gameId, ct).ConfigureAwait(false);
        if (metadataResult.IsFailure || metadataResult.Value is null)
        {
            _logger.LogWarning("Failed to get Epic metadata for {GameId}: {Error}", gameId, metadataResult.Error);
            return GameMetadata.Empty;
        }

        return metadataResult.Value;
    }

    public Task<bool> LaunchGameAsync(string gameId, CancellationToken ct = default)
        => _apiClient.LaunchGameAsync(gameId, ct);
}
