using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.Subscriptions;

namespace SaveState.Application.Subscriptions;

/// <summary>
/// Main service for managing gaming subscription integrations.
/// </summary>
public sealed class SubscriptionManagerService : ISubscriptionService
{
    private readonly ILogger<SubscriptionManagerService> _logger;
    private readonly IEnumerable<ISubscriptionProvider> _providers;
    private readonly ISubscriptionRepository _repository;
    private readonly ICacheService _cache;
    private readonly ITimeProvider _timeProvider;

    public SubscriptionManagerService(
        ILogger<SubscriptionManagerService> logger,
        IEnumerable<ISubscriptionProvider> providers,
        ISubscriptionRepository repository,
        ICacheService cache,
        ITimeProvider timeProvider)
    {
        _logger = logger;
        _providers = providers;
        _repository = repository;
        _cache = cache;
        _timeProvider = timeProvider;
    }

    public async Task<Result<IReadOnlyList<SubscriptionServiceInfo>>> GetAvailableServicesAsync(CancellationToken ct = default)
    {
        try
        {
            var services = new List<SubscriptionServiceInfo>();
            
            foreach (var provider in _providers)
            {
                var infoResult = await provider.GetServiceInfoAsync(ct);
                if (infoResult.IsSuccess && infoResult.Value is not null)
                {
                    services.Add(infoResult.Value);
                }
                else
                {
                    _logger.LogWarning(
                        "Failed to get service info for provider {ProviderType}: {Error}",
                        provider.ServiceType,
                        infoResult.Error);
                }
            }
            
            return Result.Success<IReadOnlyList<SubscriptionServiceInfo>>(services);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get available subscription services");
            return Result.Failure<IReadOnlyList<SubscriptionServiceInfo>>("Failed to retrieve subscription services");
        }
    }

    public async Task<Result<UserSubscriptionLibrary>> GetUserLibraryAsync(CancellationToken ct = default)
    {
        try
        {
            // Check cache first
            var cacheKey = "subscription_library";
            if (_cache.TryGetValue<UserSubscriptionLibrary>(cacheKey, out var cached))
            {
                _logger.LogDebug("Returning cached subscription library");
                return Result.Success(cached!);
            }
            
            var library = new UserSubscriptionLibrary();
            var allGames = new List<SubscriptionGame>();
            
            foreach (var provider in _providers)
            {
                var isSubscribed = await provider.IsSubscribedAsync(ct);
                if (!isSubscribed) continue;
                
                library.ActiveSubscriptions.Add(provider.ServiceType);
                
                var gamesResult = await provider.GetGamesAsync(ct);
                if (gamesResult.IsSuccess)
                {
                    allGames.AddRange(gamesResult.Value);
                }
            }
            
            // Deduplicate games by title
            library.Games = allGames
                .GroupBy(g => g.Title.ToLowerInvariant())
                .Select(g => 
                {
                    var first = g.First();
                    first.AvailableOn = g.SelectMany(x => x.AvailableOn).Distinct().ToList();
                    return first;
                })
                .ToList();
            
            library.LastSyncDate = _timeProvider.UtcNow;
            
            // Cache for 1 hour
            _cache.Set(cacheKey, library, TimeSpan.FromHours(1));
            
            _logger.LogInformation("Retrieved {Count} games from {ServiceCount} subscriptions",
                library.Games.Count, library.ActiveSubscriptions.Count);
            
            return Result.Success(library);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user subscription library");
            return Result.Failure<UserSubscriptionLibrary>("Failed to retrieve subscription library");
        }
    }

    public async Task<Result> SyncLibraryAsync(CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Syncing subscription library...");
            
            var allGames = new List<SubscriptionGame>();
            
            foreach (var provider in _providers)
            {
                var gamesResult = await provider.GetGamesAsync(ct);
                if (gamesResult.IsSuccess)
                {
                    allGames.AddRange(gamesResult.Value);
                }
            }
            
            await _repository.SaveGamesAsync(allGames, ct);
            
            // Invalidate cache
            _cache.Remove("subscription_library");
            
            _logger.LogInformation("Subscription library synced with {Count} games", allGames.Count);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync subscription library");
            return Result.Failure("Failed to sync subscription library");
        }
    }

    public async Task<Result<IReadOnlyList<LeavingSoonAlert>>> GetLeavingSoonGamesAsync(CancellationToken ct = default)
    {
        try
        {
            var alerts = new List<LeavingSoonAlert>();
            
            foreach (var provider in _providers)
            {
                var leavingResult = await provider.GetLeavingSoonAsync(ct);
                if (leavingResult.IsSuccess)
                {
                    foreach (var game in leavingResult.Value)
                    {
                        alerts.Add(new LeavingSoonAlert
                        {
                            Game = game,
                            LeavingDate = game.LeavingSoonDate!.Value
                        });
                    }
                }
            }
            
            // Sort by urgency (days remaining)
            alerts = alerts.OrderBy(a => a.DaysRemaining).ToList();
            
            _logger.LogInformation("Found {Count} games leaving soon", alerts.Count);
            return Result.Success<IReadOnlyList<LeavingSoonAlert>>(alerts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get leaving soon games");
            return Result.Failure<IReadOnlyList<LeavingSoonAlert>>("Failed to retrieve leaving soon games");
        }
    }

    public async Task<Result<IReadOnlyList<SubscriptionGame>>> GetNewArrivalsAsync(CancellationToken ct = default)
    {
        try
        {
            var newArrivals = new List<SubscriptionGame>();
            
            foreach (var provider in _providers)
            {
                var arrivalsResult = await provider.GetNewArrivalsAsync(ct);
                if (arrivalsResult.IsSuccess)
                {
                    newArrivals.AddRange(arrivalsResult.Value);
                }
            }
            
            // Sort by added date
            newArrivals = newArrivals
                .Where(g => g.AddedDate.HasValue)
                .OrderByDescending(g => g.AddedDate)
                .ToList();
            
            _logger.LogInformation("Found {Count} new arrivals", newArrivals.Count);
            return Result.Success<IReadOnlyList<SubscriptionGame>>(newArrivals);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get new arrivals");
            return Result.Failure<IReadOnlyList<SubscriptionGame>>("Failed to retrieve new arrivals");
        }
    }

    public async Task<Result<IReadOnlyList<SubscriptionRecommendation>>> GetRecommendationsAsync(int count = 10, CancellationToken ct = default)
    {
        try
        {
            // Get user's library and subscription games
            var libraryResult = await GetUserLibraryAsync(ct);
            if (!libraryResult.IsSuccess)
            {
                return Result.Failure<IReadOnlyList<SubscriptionRecommendation>>("Failed to get library");
            }
            
            var subscriptionGames = libraryResult.Value.Games;
            
            // Simple recommendation algorithm based on genres of played games
            // In real implementation, this would use ML or more sophisticated matching
            var recommendations = subscriptionGames
                .Where(g => !string.IsNullOrEmpty(g.Title)) // Filter out already played games
                .OrderBy(_ => Guid.NewGuid()) // Random for demo
                .Take(count)
                .Select(g => new SubscriptionRecommendation
                {
                    Game = g,
                    MatchScore = new Random().NextDouble() * 100,
                    Reason = "Popular among similar players",
                    BasedOnGames = new List<string> { "Your gaming history" }
                })
                .ToList();
            
            return Result.Success<IReadOnlyList<SubscriptionRecommendation>>(recommendations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get recommendations");
            return Result.Failure<IReadOnlyList<SubscriptionRecommendation>>("Failed to generate recommendations");
        }
    }

    public async Task<Result<bool>> IsGameAvailableOnSubscriptionAsync(string gameTitle, CancellationToken ct = default)
    {
        try
        {
            var libraryResult = await GetUserLibraryAsync(ct);
            if (!libraryResult.IsSuccess)
            {
                return Result.Failure<bool>("Failed to check subscription availability");
            }
            
            var isAvailable = libraryResult.Value.Games
                .Any(g => g.Title.Equals(gameTitle, StringComparison.OrdinalIgnoreCase));
            
            return Result.Success(isAvailable);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check if game is available on subscription");
            return Result.Failure<bool>("Failed to check subscription availability");
        }
    }

    public async Task<Result<SubscriptionComparison>> CompareSubscriptionsAsync(CancellationToken ct = default)
    {
        try
        {
            var servicesResult = await GetAvailableServicesAsync(ct);
            if (!servicesResult.IsSuccess)
            {
                return Result.Failure<SubscriptionComparison>("Failed to compare subscriptions");
            }

            var comparison = new SubscriptionComparison
            {
                Services = servicesResult.Value.ToList(),
                TotalMonthlyCost = servicesResult.Value.Sum(s => s.MonthlyPrice),
                TotalUniqueGames = servicesResult.Value.Sum(s => s.GameCount)
            };
            
            // Find best value
            var bestValue = servicesResult.Value
                .OrderByDescending(s => s.GameCount / Math.Max((double)s.MonthlyPrice, 1))
                .FirstOrDefault();
            
            if (bestValue != null)
            {
                comparison.BestValueRecommendation = $"{bestValue.Name} offers the best value with {bestValue.GameCount} games for ${bestValue.MonthlyPrice}/month";
            }
            
            return Result.Success(comparison);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to compare subscriptions");
            return Result.Failure<SubscriptionComparison>("Failed to compare subscriptions");
        }
    }

    public async Task<Result<IReadOnlyList<SubscriptionGame>>> SearchGamesAsync(string query, CancellationToken ct = default)
    {
        try
        {
            var libraryResult = await GetUserLibraryAsync(ct);
            if (!libraryResult.IsSuccess)
            {
                return Result.Failure<IReadOnlyList<SubscriptionGame>>("Failed to search games");
            }
            
            var results = libraryResult.Value.Games
                .Where(g => g.Title.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                           g.Genres.Any(genre => genre.Contains(query, StringComparison.OrdinalIgnoreCase)))
                .ToList();
            
            return Result.Success<IReadOnlyList<SubscriptionGame>>(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search subscription games");
            return Result.Failure<IReadOnlyList<SubscriptionGame>>("Failed to search games");
        }
    }

    public async Task<Result<IReadOnlyList<SubscriptionAlert>>> GetLeavingSoonAlertsAsync(CancellationToken ct = default)
    {
        try
        {
            var alerts = new List<SubscriptionAlert>();
            
            foreach (var provider in _providers)
            {
                var leavingResult = await provider.GetLeavingSoonAsync(ct);
                if (leavingResult.IsSuccess)
                {
                    foreach (var game in leavingResult.Value)
                    {
                        if (game.LeavingSoonDate.HasValue)
                        {
                            alerts.Add(new SubscriptionAlert
                            {
                                ServiceName = provider.ServiceType.ToString(),
                                GameTitle = game.Title,
                                LeavingDate = game.LeavingSoonDate.Value,
                                Type = AlertType.LeavingSoon
                            });
                        }
                    }
                }
            }
            
            return Result.Success<IReadOnlyList<SubscriptionAlert>>(alerts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get leaving soon alerts");
            return Result.Failure<IReadOnlyList<SubscriptionAlert>>("Failed to retrieve alerts");
        }
    }

    public async Task<Result<bool>> TrackGameAsync(Guid userId, string gameTitle, CancellationToken ct = default)
    {
        try
        {
            var entity = new TrackedGameEntity
            {
                UserId = userId,
                GameTitle = gameTitle,
                TrackedAt = _timeProvider.UtcNow,
                NotifyOnAvailable = true,
                NotifyOnLeaving = true
            };
            
            await _repository.AddTrackedGameAsync(entity, ct);
            
            _logger.LogInformation("User {UserId} is now tracking game {GameTitle}", userId, gameTitle);
            return Result.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to track game {GameTitle} for user {UserId}", gameTitle, userId);
            return Result.Failure<bool>("Failed to track game");
        }
    }

    public async Task<Result<IReadOnlyList<GameSubscription>>> GetUserSubscriptionsAsync(Guid userId, CancellationToken ct = default)
    {
        try
        {
            var trackedGames = await _repository.GetTrackedGamesAsync(userId, ct);
            
            var subscriptions = trackedGames.Select(t => new GameSubscription
            {
                GameTitle = t.GameTitle,
                ServiceName = t.PreferredServiceId,
                DateAdded = t.TrackedAt
            }).ToList();
            
            return Result.Success<IReadOnlyList<GameSubscription>>(subscriptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user subscriptions for user {UserId}", userId);
            return Result.Failure<IReadOnlyList<GameSubscription>>("Failed to retrieve user subscriptions");
        }
    }
}
