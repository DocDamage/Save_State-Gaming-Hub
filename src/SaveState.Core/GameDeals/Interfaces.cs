// Copyright (c) 2026 SaveStateReborn. All rights reserved.

using SaveState.Core.Common;

namespace SaveState.Core.GameDeals;

/// <summary>
/// Service for managing game deals and price tracking.
/// </summary>
public interface IGameDealsService
{
    /// <summary>
    /// Gets current deals with optional filtering.
    /// </summary>
    Task<Result<IReadOnlyList<GameDeal>>> GetDealsAsync(DealFilterOptions? filter = null, CancellationToken ct = default);

    /// <summary>
    /// Gets deals for a specific game.
    /// </summary>
    Task<Result<IReadOnlyList<GameDeal>>> GetDealsForGameAsync(string gameTitle, CancellationToken ct = default);

    /// <summary>
    /// Gets the best current deal for a game.
    /// </summary>
    Task<Result<GameDeal?>> GetBestDealAsync(string gameTitle, CancellationToken ct = default);

    /// <summary>
    /// Gets price history for a game.
    /// </summary>
    Task<Result<IReadOnlyList<PriceHistoryEntry>>> GetPriceHistoryAsync(string gameTitle, string storeId, CancellationToken ct = default);

    /// <summary>
    /// Gets deal statistics for a game.
    /// </summary>
    Task<Result<DealStatistics>> GetDealStatisticsAsync(string gameTitle, CancellationToken ct = default);

    /// <summary>
    /// Creates a price alert for a game.
    /// </summary>
    Task<Result<PriceAlert>> CreatePriceAlertAsync(Guid userId, string gameTitle, decimal? targetPrice, decimal? targetDiscountPercent, CancellationToken ct = default);

    /// <summary>
    /// Gets user's price alerts.
    /// </summary>
    Task<Result<IReadOnlyList<PriceAlert>>> GetUserPriceAlertsAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Deletes a price alert.
    /// </summary>
    Task<Result> DeletePriceAlertAsync(Guid alertId, CancellationToken ct = default);

    /// <summary>
    /// Refreshes deals from all sources.
    /// </summary>
    Task<Result> RefreshDealsAsync(CancellationToken ct = default);

    /// <summary>
    /// Searches for games by title.
    /// </summary>
    Task<Result<IReadOnlyList<string>>> SearchGamesAsync(string query, CancellationToken ct = default);
}

/// <summary>
/// Client for fetching deals from a specific source.
/// </summary>
public interface IDealSourceClient
{
    /// <summary>
    /// The source name.
    /// </summary>
    string SourceName { get; }

    /// <summary>
    /// Gets deals from this source.
    /// </summary>
    Task<Result<IReadOnlyList<GameDeal>>> GetDealsAsync(DealFilterOptions? filter = null, CancellationToken ct = default);

    /// <summary>
    /// Gets deals for a specific game.
    /// </summary>
    Task<Result<IReadOnlyList<GameDeal>>> GetDealsForGameAsync(string gameTitle, CancellationToken ct = default);

    /// <summary>
    /// Gets price history for a game.
    /// </summary>
    Task<Result<IReadOnlyList<PriceHistoryEntry>>> GetPriceHistoryAsync(string gameTitle, CancellationToken ct = default);
}

/// <summary>
/// Repository for deal data persistence.
/// </summary>
public interface IGameDealsRepository
{
    /// <summary>
    /// Saves deals to database.
    /// </summary>
    Task SaveDealsAsync(IEnumerable<GameDeal> deals, CancellationToken ct = default);

    /// <summary>
    /// Gets deals with filtering.
    /// </summary>
    Task<IReadOnlyList<GameDeal>> GetDealsAsync(DealFilterOptions? filter = null, CancellationToken ct = default);

    /// <summary>
    /// Gets deals for a specific game.
    /// </summary>
    Task<IReadOnlyList<GameDeal>> GetDealsForGameAsync(string gameTitle, CancellationToken ct = default);

    /// <summary>
    /// Gets the best deal for a game.
    /// </summary>
    Task<GameDeal?> GetBestDealAsync(string gameTitle, CancellationToken ct = default);

    /// <summary>
    /// Saves price history entries.
    /// </summary>
    Task SavePriceHistoryAsync(IEnumerable<PriceHistoryEntry> entries, CancellationToken ct = default);

    /// <summary>
    /// Gets price history for a game.
    /// </summary>
    Task<IReadOnlyList<PriceHistoryEntry>> GetPriceHistoryAsync(string gameTitle, string storeId, CancellationToken ct = default);

    /// <summary>
    /// Creates a price alert.
    /// </summary>
    Task CreatePriceAlertAsync(PriceAlert alert, CancellationToken ct = default);

    /// <summary>
    /// Gets price alerts for a user.
    /// </summary>
    Task<IReadOnlyList<PriceAlert>> GetPriceAlertsForUserAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Gets all active price alerts.
    /// </summary>
    Task<IReadOnlyList<PriceAlert>> GetActivePriceAlertsAsync(CancellationToken ct = default);

    /// <summary>
    /// Deletes a price alert.
    /// </summary>
    Task DeletePriceAlertAsync(Guid alertId, CancellationToken ct = default);

    /// <summary>
    /// Updates last triggered time for an alert.
    /// </summary>
    Task UpdateAlertLastTriggeredAsync(Guid alertId, CancellationToken ct = default);

    /// <summary>
    /// Clears old deal data.
    /// </summary>
    Task ClearOldDealsAsync(TimeSpan maxAge, CancellationToken ct = default);
}

/// <summary>
/// Service for checking price alerts and sending notifications.
/// </summary>
public interface IPriceAlertChecker
{
    /// <summary>
    /// Checks all active alerts and triggers notifications.
    /// </summary>
    Task CheckAlertsAsync(CancellationToken ct = default);
}
