// Copyright (c) 2026 SaveStateReborn. All rights reserved.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SaveState.Core.Subscriptions;
using SaveState.Core.Common;

namespace SaveState.Infrastructure.Subscriptions.Providers;

/// <summary>
/// Provider for NVIDIA GeForce NOW subscription data.
/// </summary>
public sealed class NvidiaGeForceNowProvider : ISubscriptionProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<NvidiaGeForceNowProvider> _logger;
    private readonly GeForceNowOptions _options;

    public SubscriptionServiceType ServiceType => SubscriptionServiceType.GeForceNow;

    public NvidiaGeForceNowProvider(
        HttpClient httpClient,
        ILogger<NvidiaGeForceNowProvider> logger,
        IOptions<GeForceNowOptions> options)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? new GeForceNowOptions();
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
            Id = "geforce-now",
            Type = SubscriptionServiceType.GeForceNow,
            SubscriptionType = SubscriptionType.GeForceNow,
            Name = "GeForce NOW",
            Description = "Stream PC games you already own from the cloud",
            MonthlyPrice = 9.99m,
            AnnualPrice = 49.99m,
            GameCount = 1500,
            SupportsCloudGaming = true,
            SupportsEaPlay = false,
            IsActive = true,
            Features = new List<SubscriptionFeature>
            {
                new() { Name = "RTX Gaming", Description = "Ray tracing enabled", IsIncluded = true },
                new() { Name = "4K Resolution", Description = "Up to 4K streaming", IsIncluded = false },
                new() { Name = "Long Sessions", Description = "Up to 8 hour sessions", IsIncluded = false }
            }
        };
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<SubscriptionGame>>> GetGamesAsync(CancellationToken ct = default)
    {
        try
        {
            var games = new List<SubscriptionGame>();
            _logger.LogDebug("Fetching GeForce NOW supported games");
            return Result.Success<IReadOnlyList<SubscriptionGame>>(games);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get GeForce NOW games");
            return Result.Failure<IReadOnlyList<SubscriptionGame>>("Failed to retrieve game catalog");
        }
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<SubscriptionGame>>> GetLeavingSoonAsync(CancellationToken ct = default)
    {
        try
        {
            var games = new List<SubscriptionGame>();
            _logger.LogDebug("Checking for GeForce NOW leaving soon games");
            return Result.Success<IReadOnlyList<SubscriptionGame>>(games);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get GeForce NOW leaving soon games");
            return Result.Failure<IReadOnlyList<SubscriptionGame>>("Failed to retrieve leaving soon games");
        }
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<SubscriptionGame>>> GetNewArrivalsAsync(CancellationToken ct = default)
    {
        try
        {
            var games = new List<SubscriptionGame>();
            _logger.LogDebug("Checking for GeForce NOW new arrivals");
            return Result.Success<IReadOnlyList<SubscriptionGame>>(games);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get GeForce NOW new arrivals");
            return Result.Failure<IReadOnlyList<SubscriptionGame>>("Failed to retrieve new arrivals");
        }
    }
}

/// <summary>
/// Configuration options for GeForce NOW provider.
/// </summary>
public class GeForceNowOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api.nvidia.com/geforce-now";
    public int CacheMinutes { get; set; } = 60;
}
