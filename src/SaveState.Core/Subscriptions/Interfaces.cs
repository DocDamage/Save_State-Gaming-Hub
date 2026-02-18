using SaveState.Core.Common;

namespace SaveState.Core.Subscriptions;

/// <summary>
/// Service for managing gaming subscription integrations.
/// </summary>
public interface ISubscriptionService
{
    /// <summary>
    /// Gets all available subscription services.
    /// </summary>
    Task<Result<IReadOnlyList<SubscriptionServiceInfo>>> GetAvailableServicesAsync(CancellationToken ct = default);
    
    /// <summary>
    /// Gets user's subscription library across all active subscriptions.
    /// </summary>
    Task<Result<UserSubscriptionLibrary>> GetUserLibraryAsync(CancellationToken ct = default);
    
    /// <summary>
    /// Syncs subscription library with external services.
    /// </summary>
    Task<Result> SyncLibraryAsync(CancellationToken ct = default);
    
    /// <summary>
    /// Gets games that are leaving soon.
    /// </summary>
    Task<Result<IReadOnlyList<LeavingSoonAlert>>> GetLeavingSoonGamesAsync(CancellationToken ct = default);
    
    /// <summary>
    /// Gets new arrivals across subscriptions.
    /// </summary>
    Task<Result<IReadOnlyList<SubscriptionGame>>> GetNewArrivalsAsync(CancellationToken ct = default);
    
    /// <summary>
    /// Gets personalized recommendations from subscription catalog.
    /// </summary>
    Task<Result<IReadOnlyList<SubscriptionRecommendation>>> GetRecommendationsAsync(int count = 10, CancellationToken ct = default);
    
    /// <summary>
    /// Checks if a specific game is available on any subscription.
    /// </summary>
    Task<Result<bool>> IsGameAvailableOnSubscriptionAsync(string gameTitle, CancellationToken ct = default);
    
    /// <summary>
    /// Gets subscription comparison analysis.
    /// </summary>
    Task<Result<SubscriptionComparison>> CompareSubscriptionsAsync(CancellationToken ct = default);
    
    /// <summary>
    /// Searches for games across all subscriptions.
    /// </summary>
    Task<Result<IReadOnlyList<SubscriptionGame>>> SearchGamesAsync(string query, CancellationToken ct = default);

    /// <summary>
    /// Gets leaving soon alerts.
    /// </summary>
    Task<Result<IReadOnlyList<SubscriptionAlert>>> GetLeavingSoonAlertsAsync(CancellationToken ct = default);

    /// <summary>
    /// Tracks a game for subscription availability.
    /// </summary>
    Task<Result<bool>> TrackGameAsync(Guid userId, string gameTitle, CancellationToken ct = default);

    /// <summary>
    /// Gets user's subscriptions.
    /// </summary>
    Task<Result<IReadOnlyList<GameSubscription>>> GetUserSubscriptionsAsync(Guid userId, CancellationToken ct = default);
}

/// <summary>
/// Provider for a specific subscription service.
/// </summary>
public interface ISubscriptionProvider
{
    /// <summary>
    /// The subscription service type this provider handles.
    /// </summary>
    SubscriptionServiceType ServiceType { get; }
    
    /// <summary>
    /// Checks if the user has an active subscription.
    /// </summary>
    Task<bool> IsSubscribedAsync(CancellationToken ct = default);
    
    /// <summary>
    /// Gets all games available on this subscription.
    /// </summary>
    Task<Result<IReadOnlyList<SubscriptionGame>>> GetGamesAsync(CancellationToken ct = default);
    
    /// <summary>
    /// Gets games leaving soon from this subscription.
    /// </summary>
    Task<Result<IReadOnlyList<SubscriptionGame>>> GetLeavingSoonAsync(CancellationToken ct = default);
    
    /// <summary>
    /// Gets new arrivals on this subscription.
    /// </summary>
    Task<Result<IReadOnlyList<SubscriptionGame>>> GetNewArrivalsAsync(CancellationToken ct = default);
    
    /// <summary>
    /// Gets detailed service information.
    /// </summary>
    Task<SubscriptionServiceInfo> GetServiceInfoAsync(CancellationToken ct = default);
}

/// <summary>
/// Repository for subscription data.
/// </summary>
public interface ISubscriptionRepository
{
    /// <summary>
    /// Saves subscription games to database.
    /// </summary>
    Task SaveGamesAsync(IEnumerable<SubscriptionGame> games, CancellationToken ct = default);
    
    /// <summary>
    /// Gets cached games.
    /// </summary>
    Task<IReadOnlyList<SubscriptionGame>> GetCachedGamesAsync(CancellationToken ct = default);
    
    /// <summary>
    /// Gets games by service type.
    /// </summary>
    Task<IReadOnlyList<SubscriptionGame>> GetGamesByServiceAsync(SubscriptionServiceType serviceType, CancellationToken ct = default);
    
    /// <summary>
    /// Clears cached data.
    /// </summary>
    Task ClearCacheAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets all active subscriptions for a user.
    /// </summary>
    Task<IReadOnlyList<UserSubscriptionEntity>> GetUserSubscriptionsAsync(
        Guid userId, 
        CancellationToken ct = default);

    /// <summary>
    /// Gets a specific subscription by ID.
    /// </summary>
    Task<UserSubscriptionEntity?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Gets a user's subscription for a specific service.
    /// </summary>
    Task<UserSubscriptionEntity?> GetByServiceIdAsync(
        Guid userId, 
        string serviceId, 
        CancellationToken ct = default);

    /// <summary>
    /// Adds a new subscription.
    /// </summary>
    Task AddAsync(UserSubscriptionEntity subscription, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing subscription.
    /// </summary>
    Task UpdateAsync(UserSubscriptionEntity subscription, CancellationToken ct = default);

    /// <summary>
    /// Deletes a subscription.
    /// </summary>
    Task DeleteAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Gets all tracked games for a user.
    /// </summary>
    Task<IReadOnlyList<TrackedGameEntity>> GetTrackedGamesAsync(
        Guid userId, 
        CancellationToken ct = default);

    /// <summary>
    /// Adds a game to track for subscription availability.
    /// </summary>
    Task AddTrackedGameAsync(TrackedGameEntity game, CancellationToken ct = default);

    /// <summary>
    /// Removes a tracked game.
    /// </summary>
    Task RemoveTrackedGameAsync(Guid userId, string gameTitle, CancellationToken ct = default);
}
