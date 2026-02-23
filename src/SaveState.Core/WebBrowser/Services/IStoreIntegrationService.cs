using SaveState.Core.Common;

namespace SaveState.Core.WebBrowser.Services;

/// <summary>
/// Service for integrating with game stores (Steam, Epic, etc.) to enhance browsing experience.
/// </summary>
public interface IStoreIntegrationService
{
    /// <summary>
    /// Checks if a game is owned by the user.
    /// </summary>
    /// <param name="store">The store type (steam, epic, etc.).</param>
    /// <param name="gameId">The game identifier in the store.</param>
    /// <returns>True if owned, false otherwise.</returns>
    Task<bool> IsGameOwnedAsync(string store, string gameId, CancellationToken ct = default);

    /// <summary>
    /// Gets SaveState statistics for a game.
    /// </summary>
    /// <param name="store">The store type.</param>
    /// <param name="gameId">The game identifier.</param>
    /// <returns>Store game statistics or null if not found.</returns>
    Task<StoreGameStats?> GetGameStatsAsync(string store, string gameId, CancellationToken ct = default);

    /// <summary>
    /// Initiates quick installation of a game.
    /// </summary>
    /// <param name="store">The store type.</param>
    /// <param name="gameId">The game identifier.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> QuickInstallAsync(string store, string gameId, CancellationToken ct = default);

    /// <summary>
    /// Gets price comparison across different stores.
    /// </summary>
    /// <param name="gameName">The game name to search for.</param>
    /// <returns>List of price information from different stores.</returns>
    Task<List<StorePriceInfo>> GetPriceComparisonAsync(string gameName, CancellationToken ct = default);

    /// <summary>
    /// Syncs wishlist from a store.
    /// </summary>
    /// <param name="store">The store type.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> SyncWishlistAsync(string store, CancellationToken ct = default);

    /// <summary>
    /// Adds a game to the SaveState library from a store page.
    /// </summary>
    /// <param name="store">The store type.</param>
    /// <param name="gameId">The game identifier.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<Result> AddToLibraryAsync(string store, string gameId, CancellationToken ct = default);

    /// <summary>
    /// Gets store-specific enhanced data for a game page.
    /// </summary>
    /// <param name="store">The store type.</param>
    /// <param name="gameId">The game identifier.</param>
    /// <returns>Enhanced store data.</returns>
    Task<StoreEnhancedData> GetEnhancedStoreDataAsync(string store, string gameId, CancellationToken ct = default);

    /// <summary>
    /// Event raised when a store page is detected and enhanced.
    /// </summary>
    event EventHandler<StorePageDetectedEventArgs>? OnStorePageDetected;
}

/// <summary>
/// Statistics for a game from the SaveState perspective.
/// </summary>
public class StoreGameStats
{
    /// <summary>
    /// Total hours played.
    /// </summary>
    public double TotalHoursPlayed { get; set; }

    /// <summary>
    /// Number of save states.
    /// </summary>
    public int SaveStateCount { get; set; }

    /// <summary>
    /// Last played date.
    /// </summary>
    public DateTime? LastPlayed { get; set; }

    /// <summary>
    /// Completion percentage.
    /// </summary>
    public double CompletionPercentage { get; set; }

    /// <summary>
    /// Number of achievements unlocked.
    /// </summary>
    public int AchievementsUnlocked { get; set; }

    /// <summary>
    /// Total number of achievements.
    /// </summary>
    public int TotalAchievements { get; set; }

    /// <summary>
    /// Whether the game is currently installed.
    /// </summary>
    public bool IsInstalled { get; set; }

    /// <summary>
    /// Installation path if installed.
    /// </summary>
    public string? InstallPath { get; set; }
}

/// <summary>
/// Price information from a specific store.
/// </summary>
public class StorePriceInfo
{
    /// <summary>
    /// The store name.
    /// </summary>
    public string Store { get; set; } = string.Empty;

    /// <summary>
    /// The game ID in the store.
    /// </summary>
    public string GameId { get; set; } = string.Empty;

    /// <summary>
    /// The game name.
    /// </summary>
    public string GameName { get; set; } = string.Empty;

    /// <summary>
    /// Current price.
    /// </summary>
    public decimal CurrentPrice { get; set; }

    /// <summary>
    /// Original price before discount.
    /// </summary>
    public decimal OriginalPrice { get; set; }

    /// <summary>
    /// Discount percentage.
    /// </summary>
    public int DiscountPercent { get; set; }

    /// <summary>
    /// Currency code.
    /// </summary>
    public string Currency { get; set; } = "USD";

    /// <summary>
    /// URL to the store page.
    /// </summary>
    public string StoreUrl { get; set; } = string.Empty;

    /// <summary>
    /// Whether the game is owned.
    /// </summary>
    public bool IsOwned { get; set; }

    /// <summary>
    /// Whether the game is on the wishlist.
    /// </summary>
    public bool IsOnWishlist { get; set; }
}

/// <summary>
/// Enhanced data for store pages.
/// </summary>
public class StoreEnhancedData
{
    /// <summary>
    /// SaveState stats for the game.
    /// </summary>
    public StoreGameStats? Stats { get; set; }

    /// <summary>
    /// Whether the game can be launched directly.
    /// </summary>
    public bool CanLaunch { get; set; }

    /// <summary>
    /// Whether the game can be installed.
    /// </summary>
    public bool CanInstall { get; set; }

    /// <summary>
    /// Quick actions available for this game.
    /// </summary>
    public List<StoreQuickAction> QuickActions { get; set; } = new();

    /// <summary>
    /// Related save states.
    /// </summary>
    public List<string> SaveStateIds { get; set; } = new();

    /// <summary>
    /// User notes about the game.
    /// </summary>
    public string? UserNotes { get; set; }

    /// <summary>
    /// User rating.
    /// </summary>
    public int? UserRating { get; set; }
}

/// <summary>
/// Quick action for a store page.
/// </summary>
public class StoreQuickAction
{
    /// <summary>
    /// Action identifier.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Display label.
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Icon for the action.
    /// </summary>
    public string Icon { get; set; } = string.Empty;

    /// <summary>
    /// Action type.
    /// </summary>
    public StoreActionType Type { get; set; }
}

/// <summary>
/// Types of store quick actions.
/// </summary>
public enum StoreActionType
{
    Launch,
    Install,
    AddToLibrary,
    CreateSaveState,
    ViewAchievements,
    ViewScreenshots
}

/// <summary>
/// Event arguments for store page detection.
/// </summary>
public class StorePageDetectedEventArgs : EventArgs
{
    /// <summary>
    /// The store type.
    /// </summary>
    public required string Store { get; init; }

    /// <summary>
    /// The game ID.
    /// </summary>
    public required string GameId { get; init; }

    /// <summary>
    /// The game name.
    /// </summary>
    public string? GameName { get; init; }

    /// <summary>
    /// The page URL.
    /// </summary>
    public required string Url { get; init; }
}
