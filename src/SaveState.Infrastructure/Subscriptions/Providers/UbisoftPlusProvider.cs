// Copyright (c) 2026 SaveStateReborn. All rights reserved.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SaveState.Core.Subscriptions;
using SaveState.Core.Common;

namespace SaveState.Infrastructure.Subscriptions.Providers;

/// <summary>
/// Provider for Ubisoft+ subscription data.
/// </summary>
public sealed class UbisoftPlusProvider : ISubscriptionProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<UbisoftPlusProvider> _logger;
    private readonly UbisoftPlusOptions _options;

    public SubscriptionServiceType ServiceType => SubscriptionServiceType.UbisoftPlus;

    public UbisoftPlusProvider(
        HttpClient httpClient,
        ILogger<UbisoftPlusProvider> logger,
        IOptions<UbisoftPlusOptions> options)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? new UbisoftPlusOptions();
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
            Id = "ubisoft-plus",
            Type = SubscriptionServiceType.UbisoftPlus,
            SubscriptionType = SubscriptionType.UbisoftPlus,
            Name = "Ubisoft+",
            Description = "Access to Ubisoft's premium games, DLC, and premium editions",
            MonthlyPrice = 17.99m,
            AnnualPrice = 179.99m,
            GameCount = 100,
            SupportsCloudGaming = true,
            SupportsEaPlay = false,
            IsActive = true,
            Features = new List<SubscriptionFeature>
            {
                new() { Name = "Premium Editions", Description = "All DLC included", IsIncluded = true },
                new() { Name = "Day One Access", Description = "New releases immediately", IsIncluded = true },
                new() { Name = "Cloud Gaming", Description = "Via Amazon Luna", IsIncluded = true }
            }
        };
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<SubscriptionGame>>> GetGamesAsync(CancellationToken ct = default)
    {
        try
        {
            var games = new List<SubscriptionGame>();
            _logger.LogDebug("Fetching Ubisoft+ game catalog");
            return Result.Success<IReadOnlyList<SubscriptionGame>>(games);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get Ubisoft+ games");
            return Result.Failure<IReadOnlyList<SubscriptionGame>>("Failed to retrieve game catalog");
        }
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<SubscriptionGame>>> GetLeavingSoonAsync(CancellationToken ct = default)
    {
        try
        {
            var games = new List<SubscriptionGame>();
            _logger.LogDebug("Checking for Ubisoft+ leaving soon games");
            return Result.Success<IReadOnlyList<SubscriptionGame>>(games);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get Ubisoft+ leaving soon games");
            return Result.Failure<IReadOnlyList<SubscriptionGame>>("Failed to retrieve leaving soon games");
        }
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<SubscriptionGame>>> GetNewArrivalsAsync(CancellationToken ct = default)
    {
        try
        {
            var games = new List<SubscriptionGame>();
            _logger.LogDebug("Checking for Ubisoft+ new arrivals");
            return Result.Success<IReadOnlyList<SubscriptionGame>>(games);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get Ubisoft+ new arrivals");
            return Result.Failure<IReadOnlyList<SubscriptionGame>>("Failed to retrieve new arrivals");
        }
    }
}

/// <summary>
/// Configuration options for Ubisoft+ provider.
/// </summary>
public class UbisoftPlusOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://public-ubiservices.ubi.com";
    public int CacheMinutes { get; set; } = 60;
}
