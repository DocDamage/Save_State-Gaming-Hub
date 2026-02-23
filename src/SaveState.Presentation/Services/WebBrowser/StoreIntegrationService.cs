using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.GameLibrary;
using SaveState.Core.GameLibrary.Services;
using SaveState.Core.WebBrowser.Services;

namespace SaveState.Presentation.Services.WebBrowser;

/// <summary>
/// Implementation of store integration service for enhancing game store browsing.
/// </summary>
public class StoreIntegrationService : IStoreIntegrationService
{
    private readonly ILogger<StoreIntegrationService> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly IGameRepository _gameRepository;
    private readonly IGameSessionRepository _sessionRepository;
    private readonly ISaveStateRepository _saveStateRepository;
    private readonly HttpClient _httpClient;

    // URL patterns for different stores
    private static readonly Dictionary<string, Regex> StorePatterns = new()
    {
        ["steam"] = new Regex(@"store\.steampowered\.com/app/(\d+)", RegexOptions.Compiled),
        ["epic"] = new Regex(@"store\.epicgames\.com/.+/p/([\w-]+)", RegexOptions.Compiled),
        ["gog"] = new Regex(@"gog\.com/game/(\w+)", RegexOptions.Compiled),
        ["origin"] = new Regex(@"origin\.com/.+/store/([\w-]+)", RegexOptions.Compiled),
        ["xbox"] = new Regex(@"microsoft\.com/.+/games/([\w-]+)", RegexOptions.Compiled),
        ["playstation"] = new Regex(@"playstation\.com/.+/games/([\w-]+)", RegexOptions.Compiled)
    };

    public StoreIntegrationService(
        ILogger<StoreIntegrationService> logger,
        ITimeProvider timeProvider,
        IGameRepository gameRepository,
        IGameSessionRepository sessionRepository,
        ISaveStateRepository saveStateRepository,
        HttpClient httpClient)
    {
        _logger = logger;
        _timeProvider = timeProvider;
        _gameRepository = gameRepository;
        _sessionRepository = sessionRepository;
        _saveStateRepository = saveStateRepository;
        _httpClient = httpClient;
    }

    /// <inheritdoc />
    public event EventHandler<StorePageDetectedEventArgs>? OnStorePageDetected;

    /// <inheritdoc />
    public async Task<bool> IsGameOwnedAsync(string store, string gameId, CancellationToken ct = default)
    {
        try
        {
            // Check if the game exists in our library with the matching store ID
            var games = await _gameRepository.GetAllAsync(ct);
            return games.Any(g =>
                g.StoreId == gameId &&
                g.Platform.Name.Equals(store, StringComparison.OrdinalIgnoreCase));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check ownership for {Store}:{GameId}", store, gameId);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<StoreGameStats?> GetGameStatsAsync(string store, string gameId, CancellationToken ct = default)
    {
        try
        {
            var games = await _gameRepository.GetAllAsync(ct);
            var game = games.FirstOrDefault(g =>
                g.StoreId == gameId &&
                g.Platform.Name.Equals(store, StringComparison.OrdinalIgnoreCase));

            if (game == null)
                return null;

            var sessions = await _sessionRepository.GetSessionsByGameAsync(game.Id, ct);
            var saveStates = await _saveStateRepository.GetByGameIdAsync(game.Id, ct);

            var totalHours = sessions.Sum(s => s.Duration?.TotalHours ?? 0);
            var lastPlayed = sessions
                .Where(s => s.EndTime.HasValue)
                .OrderByDescending(s => s.EndTime)
                .FirstOrDefault()?.EndTime;

            return new StoreGameStats
            {
                TotalHoursPlayed = totalHours,
                SaveStateCount = saveStates.Count,
                LastPlayed = lastPlayed,
                CompletionPercentage = game.CompletionPercentage,
                AchievementsUnlocked = game.Achievements?.Count(a => a.IsUnlocked) ?? 0,
                TotalAchievements = game.Achievements?.Count ?? 0,
                IsInstalled = !string.IsNullOrEmpty(game.ExecutablePath),
                InstallPath = game.ExecutablePath
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get game stats for {Store}:{GameId}", store, gameId);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<Result> QuickInstallAsync(string store, string gameId, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Quick install requested for {Store}:{GameId}", store, gameId);

            // This would integrate with store-specific APIs to initiate installation
            // For now, we just open the store's install URL
            var installUrl = store.ToLowerInvariant() switch
            {
                "steam" => $"steam://install/{gameId}",
                "epic" => $"com.epicgames.launcher://store/p/{gameId}",
                "xbox" => $"msxbox://game/?gameId={gameId}",
                _ => null
            };

            if (string.IsNullOrEmpty(installUrl))
            {
                return Result.Failure($"Quick install not supported for {store}", ErrorType.NotImplemented);
            }

            // Open the install URL
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(installUrl)
            {
                UseShellExecute = true
            });

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initiate quick install for {Store}:{GameId}", store, gameId);
            return Result.Failure($"Quick install failed: {ex.Message}", ErrorType.External);
        }
    }

    /// <inheritdoc />
    public async Task<List<StorePriceInfo>> GetPriceComparisonAsync(string gameName, CancellationToken ct = default)
    {
        var prices = new List<StorePriceInfo>();

        try
        {
            // This would integrate with price comparison APIs like IsThereAnyDeal
            // For now, return placeholder data structure

            var stores = new[] { "Steam", "Epic", "GOG", "Xbox", "PlayStation" };

            foreach (var store in stores)
            {
                prices.Add(new StorePriceInfo
                {
                    Store = store,
                    GameName = gameName,
                    CurrentPrice = 59.99m,
                    OriginalPrice = 59.99m,
                    DiscountPercent = 0,
                    Currency = "USD",
                    StoreUrl = $"https://{store.ToLowerInvariant()}.com/search?q={Uri.EscapeDataString(gameName)}",
                    IsOwned = await IsGameOwnedAsync(store.ToLowerInvariant(), "", ct),
                    IsOnWishlist = false
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get price comparison for {GameName}", gameName);
        }

        return prices;
    }

    /// <inheritdoc />
    public async Task<Result> SyncWishlistAsync(string store, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Syncing wishlist from {Store}", store);

            // This would integrate with store APIs to fetch wishlist
            // and potentially create wishlist items in SaveState

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync wishlist from {Store}", store);
            return Result.Failure($"Wishlist sync failed: {ex.Message}", ErrorType.External);
        }
    }

    /// <inheritdoc />
    public async Task<Result> AddToLibraryAsync(string store, string gameId, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Adding {Store}:{GameId} to library", store, gameId);

            // Check if already in library
            if (await IsGameOwnedAsync(store, gameId, ct))
            {
                return Result.Failure("Game already in library", ErrorType.Conflict);
            }

            // This would fetch game metadata from the store and add to library
            // For now, return success

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add game to library: {Store}:{GameId}", store, gameId);
            return Result.Failure($"Failed to add game: {ex.Message}", ErrorType.External);
        }
    }

    /// <inheritdoc />
    public async Task<StoreEnhancedData> GetEnhancedStoreDataAsync(string store, string gameId, CancellationToken ct = default)
    {
        var data = new StoreEnhancedData();

        try
        {
            var stats = await GetGameStatsAsync(store, gameId, ct);
            if (stats != null)
            {
                data.Stats = stats;
                data.CanLaunch = stats.IsInstalled;
                data.CanInstall = !stats.IsInstalled;

                data.QuickActions.Add(new StoreQuickAction
                {
                    Id = "launch",
                    Label = "Launch",
                    Icon = "▶",
                    Type = StoreActionType.Launch
                });

                if (stats.SaveStateCount > 0)
                {
                    data.QuickActions.Add(new StoreQuickAction
                    {
                        Id = "savestates",
                        Label = $"Save States ({stats.SaveStateCount})",
                        Icon = "💾",
                        Type = StoreActionType.CreateSaveState
                    });
                }

                data.QuickActions.Add(new StoreQuickAction
                {
                    Id = "achievements",
                    Label = $"Achievements ({stats.AchievementsUnlocked}/{stats.TotalAchievements})",
                    Icon = "🏆",
                    Type = StoreActionType.ViewAchievements
                });
            }
            else
            {
                // Game not in library
                data.CanInstall = true;
                data.QuickActions.Add(new StoreQuickAction
                {
                    Id = "add",
                    Label = "Add to Library",
                    Icon = "➕",
                    Type = StoreActionType.AddToLibrary
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get enhanced data for {Store}:{GameId}", store, gameId);
        }

        return data;
    }

    /// <summary>
    /// Detects if a URL is a store page and extracts store information.
    /// </summary>
    public StorePageDetectedEventArgs? DetectStorePage(string url)
    {
        foreach (var (store, pattern) in StorePatterns)
        {
            var match = pattern.Match(url);
            if (match.Success)
            {
                var gameId = match.Groups[1].Value;

                var args = new StorePageDetectedEventArgs
                {
                    Store = store,
                    GameId = gameId,
                    Url = url
                };

                OnStorePageDetected?.Invoke(this, args);
                return args;
            }
        }

        return null;
    }
}
