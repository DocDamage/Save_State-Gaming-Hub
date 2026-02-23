using SaveState.Core.Common;

namespace SaveState.IntegrationTests;

/// <summary>
/// Fake implementation of ICloudCatalogService for integration tests.
/// Matches the interface defined in CloudGamingIntegrationTests.cs.
/// </summary>
public class FakeCloudCatalogService : ICloudCatalogService
{
    private readonly ICloudGamingManager? _cloudGamingManager;
    private readonly Dictionary<string, CloudGame> _games = new();
    private readonly Dictionary<string, CloudGameDetails> _gameDetails = new();
    private readonly Dictionary<string, HashSet<string>> _favorites = new(); // providerId -> set of gameIds
    private readonly Dictionary<string, DateTime> _recentlyPlayed = new(); // gameId -> last played

    public FakeCloudCatalogService(ICloudGamingManager? cloudGamingManager = null)
    {
        _cloudGamingManager = cloudGamingManager;
        InitializeFakeData();
    }

    private void InitializeFakeData()
    {
        // Initialize fake games
        var games = new[]
        {
            new CloudGame
            {
                Id = "game_1",
                ProviderId = "geforce_now",
                Title = "Cyberpunk 2077",
                CoverImageUrl = "https://example.com/cyberpunk.jpg",
                Genres = new List<string> { "RPG", "Action", "Open World" },
                LastPlayedAt = DateTime.UtcNow.AddDays(-1)
            },
            new CloudGame
            {
                Id = "game_2",
                ProviderId = "geforce_now",
                Title = "The Witcher 3: Wild Hunt",
                CoverImageUrl = "https://example.com/witcher3.jpg",
                Genres = new List<string> { "RPG", "Action", "Open World" },
                LastPlayedAt = DateTime.UtcNow.AddDays(-3)
            },
            new CloudGame
            {
                Id = "game_3",
                ProviderId = "xbox_cloud",
                Title = "Forza Horizon 5",
                CoverImageUrl = "https://example.com/forza5.jpg",
                Genres = new List<string> { "Racing", "Open World" },
                LastPlayedAt = DateTime.UtcNow.AddDays(-2)
            },
            new CloudGame
            {
                Id = "game_4",
                ProviderId = "xbox_cloud",
                Title = "Halo Infinite",
                CoverImageUrl = "https://example.com/halo.jpg",
                Genres = new List<string> { "FPS", "Action" },
                LastPlayedAt = null
            },
            new CloudGame
            {
                Id = "game_5",
                ProviderId = "amazon_luna",
                Title = "Assassin's Creed Valhalla",
                CoverImageUrl = "https://example.com/acvalhalla.jpg",
                Genres = new List<string> { "Action", "Adventure", "Open World" },
                LastPlayedAt = DateTime.UtcNow.AddDays(-7)
            }
        };

        foreach (var game in games)
        {
            _games[game.Id] = game;
            _gameDetails[game.Id] = new CloudGameDetails
            {
                Id = game.Id,
                ProviderId = game.ProviderId,
                Title = game.Title,
                CoverImageUrl = game.CoverImageUrl,
                Genres = game.Genres,
                LastPlayedAt = game.LastPlayedAt,
                Description = $"{game.Title} is an amazing game with rich gameplay and stunning visuals.",
                Developer = "Game Studio Inc.",
                Publisher = "Big Publisher",
                ReleaseDate = DateTime.UtcNow.AddYears(-2),
                MetacriticScore = 85,
                Screenshots = new List<string> { "https://example.com/ss1.jpg", "https://example.com/ss2.jpg" },
                AvailableQualities = new List<StreamQuality> { StreamQuality.Low, StreamQuality.Medium, StreamQuality.High, StreamQuality.Ultra }
            };
        }
    }

    private async Task<bool> IsConnectedAsync(string providerId)
    {
        if (_cloudGamingManager == null)
            return false;

        var result = await _cloudGamingManager.IsProviderConnectedAsync(providerId);
        return result.IsSuccess && result.Value;
    }

    public async Task<Result<SyncResult>> SyncGameLibraryAsync(string providerId)
    {
        if (!await IsConnectedAsync(providerId))
        {
            return Result.Failure<SyncResult>("Not connected to provider", ErrorType.Unauthorized);
        }

        var gamesForProvider = _games.Values.Where(g => g.ProviderId == providerId).ToList();
        var result = new SyncResult
        {
            GamesAdded = gamesForProvider.Count,
            GamesUpdated = 0,
            GamesRemoved = 0,
            SyncedAt = DateTime.UtcNow
        };

        return Result.Success(result);
    }

    public Task<Result<List<CloudGame>>> GetCloudGamesAsync()
    {
        // Return games regardless of connection status (for test compatibility)
        return Task.FromResult(Result.Success(_games.Values.ToList()));
    }

    public Task<Result<List<CloudGame>>> GetCloudGamesByProviderAsync(string providerId)
    {
        var games = _games.Values.Where(g => g.ProviderId == providerId).ToList();
        return Task.FromResult(Result.Success(games));
    }

    public Task<Result<List<CloudGame>>> SearchCloudGamesAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Task.FromResult(Result.Success(_games.Values.ToList()));
        }

        var results = _games.Values
            .Where(g => g.Title.Contains(query, StringComparison.OrdinalIgnoreCase))
            .ToList();

        return Task.FromResult(Result.Success(results));
    }

    public Task<Result<CloudGameDetails>> GetGameDetailsAsync(string gameId)
    {
        if (_gameDetails.TryGetValue(gameId, out var details))
        {
            return Task.FromResult(Result.Success(details));
        }

        return Task.FromResult(Result.Failure<CloudGameDetails>("Game not found", ErrorType.NotFound));
    }

    public Task<Result<List<CloudGame>>> GetRecentlyPlayedAsync(string providerId, int count)
    {
        var games = _games.Values
            .Where(g => g.ProviderId == providerId && g.LastPlayedAt.HasValue)
            .OrderByDescending(g => g.LastPlayedAt)
            .Take(count)
            .ToList();

        return Task.FromResult(Result.Success(games));
    }

    public Task<Result<List<CloudGame>>> GetFavoritesAsync(string providerId)
    {
        if (_favorites.TryGetValue(providerId, out var favoriteIds))
        {
            var games = _games.Values
                .Where(g => favoriteIds.Contains(g.Id))
                .ToList();

            return Task.FromResult(Result.Success(games));
        }

        return Task.FromResult(Result.Success(new List<CloudGame>()));
    }

    public Task<Result<bool>> AddToFavoritesAsync(string providerId, string gameId)
    {
        if (!_games.ContainsKey(gameId))
        {
            return Task.FromResult(Result.Failure<bool>("Game not found", ErrorType.NotFound));
        }

        if (!_favorites.TryGetValue(providerId, out var favorites))
        {
            favorites = new HashSet<string>();
            _favorites[providerId] = favorites;
        }

        favorites.Add(gameId);
        return Task.FromResult(Result.Success(true));
    }

    public Task<Result<bool>> RemoveFromFavoritesAsync(string providerId, string gameId)
    {
        if (_favorites.TryGetValue(providerId, out var favorites))
        {
            favorites.Remove(gameId);
        }

        return Task.FromResult(Result.Success(true));
    }
}
