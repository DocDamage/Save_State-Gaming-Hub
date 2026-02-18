// Copyright (c) 2026 SaveStateReborn. All rights reserved.

using Microsoft.Extensions.Logging;

namespace SaveState.Core.Subscriptions;

/// <summary>
/// Price comparison data for a game across subscription and purchase options.
/// </summary>
public class GamePriceComparison
{
    /// <summary>
    /// The game being compared.
    /// </summary>
    public SubscriptionGame Game { get; set; } = null!;

    /// <summary>
    /// Subscription options for this game.
    /// </summary>
    public List<SubscriptionPriceOption> SubscriptionOptions { get; set; } = new();

    /// <summary>
    /// Purchase options from various stores.
    /// </summary>
    public List<PurchasePriceOption> PurchaseOptions { get; set; } = new();

    /// <summary>
    /// Historical low price.
    /// </summary>
    public decimal? HistoricalLowPrice { get; set; }

    /// <summary>
    /// Best current option (subscription or purchase).
    /// </summary>
    public PriceOptionBase? BestOption { get; set; }

    /// <summary>
    /// Recommendation text for the user.
    /// </summary>
    public string Recommendation { get; set; } = string.Empty;
}

/// <summary>
/// Base class for price options.
/// </summary>
public abstract class PriceOptionBase
{
    /// <summary>
    /// The price.
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// Any discount percentage.
    /// </summary>
    public decimal? DiscountPercent { get; set; }

    /// <summary>
    /// URL to the store/service.
    /// </summary>
    public string? StoreUrl { get; set; }

    /// <summary>
    /// When this price was last updated.
    /// </summary>
    public DateTime LastUpdated { get; set; }

    /// <summary>
    /// Whether this option requires an existing subscription.
    /// </summary>
    public abstract bool RequiresSubscription { get; }

    /// <summary>
    /// Formatted price string.
    /// </summary>
    public string FormattedPrice => $"${Price:F2}";

    /// <summary>
    /// Formatted discount string.
    /// </summary>
    public string? FormattedDiscount => DiscountPercent.HasValue ? $"-{DiscountPercent:F0}%" : null;
}

/// <summary>
/// Subscription-based price option.
/// </summary>
public class SubscriptionPriceOption : PriceOptionBase
{
    /// <summary>
    /// The subscription service.
    /// </summary>
    public SubscriptionServiceType ServiceType { get; set; }

    /// <summary>
    /// Name of the subscription service.
    /// </summary>
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>
    /// Monthly cost of the subscription.
    /// </summary>
    public decimal MonthlyCost { get; set; }

    /// <summary>
    /// Whether the user is already subscribed.
    /// </summary>
    public bool IsAlreadySubscribed { get; set; }

    /// <summary>
    /// Effective cost (0 if already subscribed, otherwise monthly cost).
    /// </summary>
    public decimal EffectiveCost => IsAlreadySubscribed ? 0 : MonthlyCost;

    public override bool RequiresSubscription => !IsAlreadySubscribed;

    /// <summary>
    /// How long the game typically stays on the service.
    /// </summary>
    public TimeSpan? TypicalAvailabilityDuration { get; set; }
}

/// <summary>
/// Purchase price option from a store.
/// </summary>
public class PurchasePriceOption : PriceOptionBase
{
    /// <summary>
    /// The store name (Steam, GOG, Epic, etc.).
    /// </summary>
    public string StoreName { get; set; } = string.Empty;

    /// <summary>
    /// Whether this is a DRM-free version.
    /// </summary>
    public bool IsDrmFree { get; set; }

    /// <summary>
    /// Whether this includes all DLC.
    /// </summary>
    public bool IncludesDLC { get; set; }

    public override bool RequiresSubscription => false;

    /// <summary>
    /// Platform availability (Steam, Epic, etc.).
    /// </summary>
    public List<string> Platforms { get; set; } = new();
}

/// <summary>
/// Service for comparing game prices across subscriptions and stores.
/// </summary>
public interface IPriceComparisonService
{
    /// <summary>
    /// Gets price comparison for a specific game.
    /// </summary>
    Task<GamePriceComparison> ComparePricesAsync(string gameTitle, CancellationToken ct = default);

    /// <summary>
    /// Gets the best option for a game (subscription or purchase).
    /// </summary>
    Task<PriceOptionBase?> GetBestOptionAsync(string gameTitle, CancellationToken ct = default);

    /// <summary>
    /// Calculates break-even point for subscription vs purchase.
    /// </summary>
    /// <param name="subscriptionPrice">Monthly subscription price</param>
    /// <param name="purchasePrice">Game purchase price</param>
    /// <returns>Number of months until subscription equals purchase price</returns>
    int CalculateBreakEvenMonths(decimal subscriptionPrice, decimal purchasePrice);
}

/// <summary>
/// Price comparison service implementation.
/// </summary>
public class PriceComparisonService : IPriceComparisonService
{
    private readonly ISubscriptionService _subscriptionService;
    private readonly ILogger<PriceComparisonService> _logger;

    public PriceComparisonService(
        ISubscriptionService subscriptionService,
        ILogger<PriceComparisonService> logger)
    {
        _subscriptionService = subscriptionService;
        _logger = logger;
    }

    public async Task<GamePriceComparison> ComparePricesAsync(string gameTitle, CancellationToken ct = default)
    {
        _logger.LogInformation("Comparing prices for {GameTitle}", gameTitle);

        var comparison = new GamePriceComparison
        {
            Game = new SubscriptionGame { Title = gameTitle }
        };

        // Get subscription availability
        var subResult = await _subscriptionService.IsGameAvailableOnSubscriptionAsync(gameTitle, ct);
        if (subResult.IsSuccess && subResult.Value)
        {
            var servicesResult = await _subscriptionService.GetAvailableServicesAsync(ct);
            if (servicesResult.IsSuccess)
            {
                foreach (var service in servicesResult.Value)
                {
                    comparison.SubscriptionOptions.Add(new SubscriptionPriceOption
                    {
                        ServiceType = service.Type,
                        ServiceName = service.Name,
                        MonthlyCost = service.MonthlyPrice,
                        Price = service.MonthlyPrice,
                        IsAlreadySubscribed = false // Would check user subscriptions
                    });
                }
            }
        }

        // Mock purchase options - in real implementation, would call price APIs
        comparison.PurchaseOptions = new List<PurchasePriceOption>
        {
            new() { StoreName = "Steam", Price = 59.99m, Platforms = new List<string> { "PC" } },
            new() { StoreName = "GOG", Price = 59.99m, IsDrmFree = true, Platforms = new List<string> { "PC" } },
            new() { StoreName = "Epic", Price = 59.99m, Platforms = new List<string> { "PC" } }
        };

        // Determine best option
        comparison.BestOption = DetermineBestOption(comparison);
        comparison.Recommendation = GenerateRecommendation(comparison);

        return comparison;
    }

    public async Task<PriceOptionBase?> GetBestOptionAsync(string gameTitle, CancellationToken ct = default)
    {
        var comparison = await ComparePricesAsync(gameTitle, ct);
        return comparison.BestOption;
    }

    public int CalculateBreakEvenMonths(decimal subscriptionPrice, decimal purchasePrice)
    {
        if (subscriptionPrice <= 0) return 0;
        return (int)Math.Ceiling(purchasePrice / subscriptionPrice);
    }

    private PriceOptionBase? DetermineBestOption(GamePriceComparison comparison)
    {
        // If already subscribed to a service with the game, that's the best option
        var activeSub = comparison.SubscriptionOptions.FirstOrDefault(s => s.IsAlreadySubscribed);
        if (activeSub != null) return activeSub;

        // If subscription cost is lower than purchase for short-term play
        var cheapestSub = comparison.SubscriptionOptions.OrderBy(s => s.MonthlyCost).FirstOrDefault();
        var cheapestPurchase = comparison.PurchaseOptions.OrderBy(p => p.Price).FirstOrDefault();

        if (cheapestSub != null && cheapestPurchase != null)
        {
            // If game stays on subscription for 6+ months on average, subscription might be better
            if (cheapestSub.TypicalAvailabilityDuration?.TotalDays > 180)
            {
                if (cheapestSub.MonthlyCost * 6 < cheapestPurchase.Price)
                {
                    return cheapestSub;
                }
            }
        }

        return cheapestPurchase;
    }

    private string GenerateRecommendation(GamePriceComparison comparison)
    {
        if (comparison.BestOption is SubscriptionPriceOption sub)
        {
            if (sub.IsAlreadySubscribed)
                return $"✓ You already have access through {sub.ServiceName}!";

            var breakEven = CalculateBreakEvenMonths(sub.MonthlyCost, 
                comparison.PurchaseOptions.Min(p => p.Price));
            return $"💡 Subscribe to {sub.ServiceName} if you plan to play for less than {breakEven} months";
        }

        if (comparison.BestOption is PurchasePriceOption purchase)
        {
            return $"💡 Buy from {purchase.StoreName} for long-term ownership";
        }

        return "Compare options based on your play style";
    }
}
