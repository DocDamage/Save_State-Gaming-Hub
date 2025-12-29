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
            var gogGames = await _apiClient.GetOwnedGamesAsync(ct).ConfigureAwait(false);
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

    public Task<GameMetadata> GetGameMetadataAsync(string gameId, CancellationToken ct = default)
        => _apiClient.GetGameDetailsAsync(gameId, ct);

    public Task<bool> LaunchGameAsync(string gameId, CancellationToken ct = default)
        => _apiClient.LaunchGameAsync(gameId, ct);
}
