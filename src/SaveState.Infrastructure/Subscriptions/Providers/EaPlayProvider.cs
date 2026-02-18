// Copyright (c) 2026 SaveStateReborn. All rights reserved.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SaveState.Core.Subscriptions;
using SaveState.Core.Common;

namespace SaveState.Infrastructure.Subscriptions.Providers;

/// <summary>
/// Provider for EA Play subscription data.
/// </summary>
public sealed class EaPlayProvider : ISubscriptionProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<EaPlayProvider> _logger;
    private readonly EaPlayOptions _options;

    public SubscriptionServiceType ServiceType => SubscriptionServiceType.EAPlay;

    public EaPlayProvider(
        HttpClient httpClient,
        ILogger<EaPlayProvider> logger,
        IOptions<EaPlayOptions> options)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? new EaPlayOptions();
    }

    /// <inheritdoc />
    public Task<bool> IsSubscribedAsync(CancellationToken ct = default)
    {
        return Task.FromResult(false);
    }

    /// <inheritdoc />
    public async Task<SubscriptionServiceInfo> GetServiceInfoAsync(CancellationToken ct = default)
    {
        return new SubscriptionServiceInfo
        {
            Id = "ea-play",
            Type = SubscriptionServiceType.EAPlay,
            SubscriptionType = SubscriptionType.EaPlay,
            Name = "EA Play",
            Description = "Access to best EA games, trials, and 10% member discount",
            MonthlyPrice = 4.99m,
            AnnualPrice = 29.99m,
            GameCount = 90,
            SupportsCloudGaming = false,
            SupportsEaPlay = false,
            IsActive = true,
            Features = new List<SubscriptionFeature>
            {
                new() { Name = "The Vault", Description = "Access to 90+ games", IsIncluded = true },
                new() { Name = "10% Discount", Description = "On EA digital purchases", IsIncluded = true },
                new() { Name = "Early Trials", Description = "Play new games early", IsIncluded = true }
            }
        };
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<SubscriptionGame>>> GetGamesAsync(CancellationToken ct = default)
    {
        try
        {
            var games = new List<SubscriptionGame>();
            _logger.LogDebug("Fetching EA Play game catalog");
            return Result.Success<IReadOnlyList<SubscriptionGame>>(games);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get EA Play games");
            return Result.Failure<IReadOnlyList<SubscriptionGame>>("Failed to retrieve game catalog");
        }
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<SubscriptionGame>>> GetLeavingSoonAsync(CancellationToken ct = default)
    {
        try
        {
            var games = new List<SubscriptionGame>();
            _logger.LogDebug("Checking for EA Play leaving soon games");
            return Result.Success<IReadOnlyList<SubscriptionGame>>(games);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get EA Play leaving soon games");
            return Result.Failure<IReadOnlyList<SubscriptionGame>>("Failed to retrieve leaving soon games");
        }
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<SubscriptionGame>>> GetNewArrivalsAsync(CancellationToken ct = default)
    {
        try
        {
            var games = new List<SubscriptionGame>();
            _logger.LogDebug("Checking for EA Play new arrivals");
            return Result.Success<IReadOnlyList<SubscriptionGame>>(games);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get EA Play new arrivals");
            return Result.Failure<IReadOnlyList<SubscriptionGame>>("Failed to retrieve new arrivals");
        }
    }
}

/// <summary>
/// Configuration options for EA Play provider.
/// </summary>
public class EaPlayOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api.ea.com";
    public int CacheMinutes { get; set; } = 60;
}
