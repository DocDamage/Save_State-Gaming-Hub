using Microsoft.Extensions.Logging;
using SaveState.Core.GameLibrary.Services;
using SaveState.Core.GameLibrary.DTOs;

namespace SaveState.Infrastructure.External;

public class GogProvider : IGameProvider
{
    private readonly IGogApiClient _apiClient;
    private readonly ILogger<GogProvider> _logger;

    public string Name => "GOG";
    public ProviderCapabilities Capabilities => ProviderCapabilities.All;

    public GogProvider(IGogApiClient apiClient, ILogger<GogProvider> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    public async Task<IReadOnlyList<GameInfo>> GetInstalledGamesAsync(CancellationToken ct = default)
    {
        try
        {
            var gogGamesResult = await _apiClient.GetOwnedGamesAsync(ct).ConfigureAwait(false);
            if (gogGamesResult.IsFailure || gogGamesResult.Value is null)
            {
                _logger.LogWarning("Failed to get GOG owned games: {Error}", gogGamesResult.Error);
                return Array.Empty<GameInfo>();
            }

            var gogGames = gogGamesResult.Value;
            return gogGames.Select(g => new GameInfo
            {
                Source = "GOG",
                SourceId = g.Id.ToString(),
                Title = g.Title,
                InstallPath = g.InstallPath,
                LastPlayed = g.LastPlayedDate,
                PlayTimeMinutes = g.PlayTimeMinutes,
                Platform = "PC"
            }).ToList();
        }
        catch (GogApiException ex)
        {
            _logger.LogWarning(ex, "Failed to get GOG games");
            return Array.Empty<GameInfo>();
        }
    }

    public async Task<GameMetadata> GetGameMetadataAsync(string gameId, CancellationToken ct = default)
    {
        var metadataResult = await _apiClient.GetGameDetailsAsync(gameId, ct).ConfigureAwait(false);
        if (metadataResult.IsFailure || metadataResult.Value is null)
        {
            _logger.LogWarning("Failed to get GOG metadata for {GameId}: {Error}", gameId, metadataResult.Error);
            return GameMetadata.Empty;
        }

        return metadataResult.Value;
    }

    public Task<bool> LaunchGameAsync(string gameId, CancellationToken ct = default)
        => _apiClient.LaunchGameAsync(gameId, ct);
}
