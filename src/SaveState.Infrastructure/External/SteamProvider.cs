using Microsoft.Extensions.Logging;
using SaveState.Core.GameLibrary.Services;
using SaveState.Core.GameLibrary.DTOs;

namespace SaveState.Infrastructure.External;

public class SteamProvider : IGameProvider
{
    private readonly ISteamApiClient _apiClient;
    private readonly ILogger<SteamProvider> _logger;

    public string Name => "Steam";
    public ProviderCapabilities Capabilities => ProviderCapabilities.All;

    public SteamProvider(ISteamApiClient apiClient, ILogger<SteamProvider> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    public async Task<IReadOnlyList<GameInfo>> GetInstalledGamesAsync(CancellationToken ct = default)
    {
        try
        {
            var steamGames = await _apiClient.GetOwnedGamesAsync(ct).ConfigureAwait(false);

            return steamGames.Select(g => new GameInfo
            {
                Source = "Steam",
                SourceId = g.AppId.ToString(),
                Title = g.Name,
                InstallPath = g.InstallPath,
                LastPlayed = g.LastPlayedDate,
                PlayTimeMinutes = g.PlayTimeMinutes,
                Platform = "PC"
            }).ToList();
        }
        catch (SteamApiException ex)
        {
            _logger.LogWarning(ex, "Failed to get Steam games");
            return Array.Empty<GameInfo>();
        }
    }

    public Task<GameMetadata> GetGameMetadataAsync(string gameId, CancellationToken ct = default)
        => _apiClient.GetGameDetailsAsync(gameId, ct);

    public Task<bool> LaunchGameAsync(string gameId, CancellationToken ct = default)
        => _apiClient.LaunchGameAsync(gameId, ct);
}
