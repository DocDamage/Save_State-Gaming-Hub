using System.Text.Json;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.Subscriptions;

namespace SaveState.Infrastructure.Subscriptions;

/// <summary>
/// Provider for Xbox Game Pass subscription.
/// </summary>
public sealed class XboxGamePassProvider : ISubscriptionProvider
{
    private readonly ILogger<XboxGamePassProvider> _logger;
    private readonly HttpClient _httpClient;
    private readonly ITimeProvider _timeProvider;
    private const string GamePassApiBaseUrl = "https://catalog.gamepass.com/sigls/v2";
    private const string XboxCatalogUrl = "https://displaycatalog.mp.microsoft.com/v7.0/products";

    public SubscriptionServiceType ServiceType => SubscriptionServiceType.XboxGamePass;

    public XboxGamePassProvider(ILogger<XboxGamePassProvider> logger, HttpClient httpClient, ITimeProvider timeProvider)
    {
        _logger = logger;
        _httpClient = httpClient;
        _timeProvider = timeProvider;
    }

    public Task<bool> IsSubscribedAsync(CancellationToken ct = default)
    {
        // Would check Xbox Live API for subscription status
        // Simplified for demo
        return Task.FromResult(true);
    }

    public async Task<Result<IReadOnlyList<SubscriptionGame>>> GetGamesAsync(CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Fetching Xbox Game Pass catalog...");
            
            // Xbox Game Pass PC catalog ID
            var pcCatalogId = "fdd9e2a7-0fee-49f6-ad69-4354098401ff";
            var url = $"{GamePassApiBaseUrl}?id={pcCatalogId}&language=en-us&market=US";
            
            var response = await _httpClient.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync(ct);
            var games = ParseGamePassCatalog(json);
            
            _logger.LogInformation("Fetched {Count} games from Xbox Game Pass", games.Count);
            return Result.Success<IReadOnlyList<SubscriptionGame>>(games);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch Xbox Game Pass catalog");
            return Result.Failure<IReadOnlyList<SubscriptionGame>>("Failed to fetch Game Pass catalog");
        }
    }

    public async Task<Result<IReadOnlyList<SubscriptionGame>>> GetLeavingSoonAsync(CancellationToken ct = default)
    {
        try
        {
            // Xbox Game Pass leaving soon uses a different catalog
            var leavingSoonId = "a2d7a76e-50c4-4533-8753-265832a1e786";
            var url = $"{GamePassApiBaseUrl}?id={leavingSoonId}&language=en-us&market=US";
            
            var response = await _httpClient.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync(ct);
            var games = ParseGamePassCatalog(json);
            
            // Mark as leaving soon (typically 14 days)
            foreach (var game in games)
            {
                game.LeavingSoonDate = _timeProvider.UtcNow.AddDays(14);
            }
            
            return Result.Success<IReadOnlyList<SubscriptionGame>>(games);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch leaving soon games");
            return Result.Failure<IReadOnlyList<SubscriptionGame>>("Failed to fetch leaving soon games");
        }
    }

    public async Task<Result<IReadOnlyList<SubscriptionGame>>> GetNewArrivalsAsync(CancellationToken ct = default)
    {
        try
        {
            // New arrivals/recently added catalog
            var recentId = "9b9b4b07-3368-4898-8bb1-019cfd5e3d5e";
            var url = $"{GamePassApiBaseUrl}?id={recentId}&language=en-us&market=US";
            
            var response = await _httpClient.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadAsStringAsync(ct);
            var games = ParseGamePassCatalog(json);
            
            // Mark as new arrivals
            foreach (var game in games)
            {
                game.AddedDate = _timeProvider.UtcNow.AddDays(-new Random().Next(1, 30));
            }
            
            return Result.Success<IReadOnlyList<SubscriptionGame>>(games);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch new arrivals");
            return Result.Failure<IReadOnlyList<SubscriptionGame>>("Failed to fetch new arrivals");
        }
    }

    public Task<Result<SubscriptionServiceInfo>> GetServiceInfoAsync(CancellationToken ct = default)
    {
        var serviceInfo = new SubscriptionServiceInfo
        {
            Type = SubscriptionServiceType.XboxGamePass,
            Name = "Xbox Game Pass",
            Description = "Access to over 100 high-quality PC games",
            MonthlyPrice = 9.99m,
            AnnualPrice = 99.99m,
            WebsiteUrl = "https://www.xbox.com/xbox-game-pass",
            IsActive = true,
            Features = new List<SubscriptionFeature>
            {
                new() { Name = "100+ PC Games", Description = "Full library of PC games", IsIncluded = true },
                new() { Name = "Day One Releases", Description = "Microsoft first-party games on release day", IsIncluded = true },
                new() { Name = "Member Discounts", Description = "Up to 20% off game purchases", IsIncluded = true },
                new() { Name = "EA Play", Description = "EA Play included at no extra cost", IsIncluded = true }
            }
        };
        return Task.FromResult(Result.Success(serviceInfo));
    }

    private List<SubscriptionGame> ParseGamePassCatalog(string json)
    {
        var games = new List<SubscriptionGame>();
        
        try
        {
            using var doc = JsonDocument.Parse(json);
            var products = doc.RootElement.GetProperty("Products");
            
            foreach (var product in products.EnumerateArray())
            {
                var productId = product.GetProperty("ProductId").GetString();
                var title = product.GetProperty("LocalizedProperties")[0].GetProperty("ProductTitle").GetString();
                
                var game = new SubscriptionGame
                {
                    GameId = productId ?? Guid.NewGuid().ToString(),
                    Title = title ?? "Unknown Game",
                    AvailableOn = new List<SubscriptionServiceType> { SubscriptionServiceType.XboxGamePass },
                    CoverImageUrl = $"https://store-images.s-microsoft.com/image/apps.12345.12345-0000-0000-000000000000.12345-0000-0000-0000-000000000000?w=400&h=600"
                };
                
                games.Add(game);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error parsing Game Pass catalog");
        }
        
        return games;
    }
}
