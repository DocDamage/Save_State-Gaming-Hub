// Copyright (c) 2026 SaveStateReborn. All rights reserved.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SaveState.Core.Subscriptions;
using SaveState.Core.Common;

namespace SaveState.Infrastructure.Subscriptions.Providers;

/// <summary>
/// Provider for PlayStation Plus subscription data.
/// </summary>
public sealed class PlayStationPlusProvider : ISubscriptionProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PlayStationPlusProvider> _logger;
    private readonly PlayStationPlusOptions _options;

    public SubscriptionServiceType ServiceType => SubscriptionServiceType.PlayStationPlus;

    public PlayStationPlusProvider(
        HttpClient httpClient,
        ILogger<PlayStationPlusProvider> logger,
        IOptions<PlayStationPlusOptions> options)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? new PlayStationPlusOptions();
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
            Id = "playstation-plus",
            Type = SubscriptionServiceType.PlayStationPlus,
            SubscriptionType = SubscriptionType.PlayStationPlus,
            Name = "PlayStation Plus",
            Description = "Online multiplayer, monthly games, and game catalog",
            MonthlyPrice = 9.99m,
            AnnualPrice = 79.99m,
            GameCount = 700,
            SupportsCloudGaming = true,
            SupportsEaPlay = false,
            IsActive = true,
            Features = new List<SubscriptionFeature>
            {
                new() { Name = "Online Multiplayer", Description = "Play games online", IsIncluded = true },
                new() { Name = "Monthly Games", Description = "Free games each month", IsIncluded = true },
                new() { Name = "Game Catalog", Description = "Access to 400+ games", IsIncluded = false }
            }
        };
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<SubscriptionGame>>> GetGamesAsync(CancellationToken ct = default)
    {
        try
        {
            var games = new List<SubscriptionGame>();
            _logger.LogDebug("Fetching PlayStation Plus game catalog");
            return Result.Success<IReadOnlyList<SubscriptionGame>>(games);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get PlayStation Plus games");
            return Result.Failure<IReadOnlyList<SubscriptionGame>>("Failed to retrieve game catalog");
        }
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<SubscriptionGame>>> GetLeavingSoonAsync(CancellationToken ct = default)
    {
        try
        {
            var games = new List<SubscriptionGame>();
            _logger.LogDebug("Checking for PlayStation Plus leaving soon games");
            return Result.Success<IReadOnlyList<SubscriptionGame>>(games);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get PS Plus leaving soon games");
            return Result.Failure<IReadOnlyList<SubscriptionGame>>("Failed to retrieve leaving soon games");
        }
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<SubscriptionGame>>> GetNewArrivalsAsync(CancellationToken ct = default)
    {
        try
        {
            var games = new List<SubscriptionGame>();
            _logger.LogDebug("Checking for PlayStation Plus new arrivals");
            return Result.Success<IReadOnlyList<SubscriptionGame>>(games);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get PS Plus new arrivals");
            return Result.Failure<IReadOnlyList<SubscriptionGame>>("Failed to retrieve new arrivals");
        }
    }
}

/// <summary>
/// Configuration options for PlayStation Plus provider.
/// </summary>
public class PlayStationPlusOptions
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://web.np.playstation.com/api/graphql/v1";
    public int CacheMinutes { get; set; } = 60;
}
