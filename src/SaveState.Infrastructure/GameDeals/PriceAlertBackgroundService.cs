// Copyright (c) 2026 SaveStateReborn. All rights reserved.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common.Services;
using SaveState.Core.GameDeals;
using SaveState.Infrastructure.Subscriptions;

namespace SaveState.Infrastructure.GameDeals;

/// <summary>
/// Background service that checks price alerts and sends notifications.
/// </summary>
public sealed class PriceAlertBackgroundService : BackgroundService, IPriceAlertChecker
{
    private readonly ILogger<PriceAlertBackgroundService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly ITimeProvider _timeProvider;
    private readonly TimeSpan _checkInterval = TimeSpan.FromHours(1); // Check every hour

    public PriceAlertBackgroundService(
        ILogger<PriceAlertBackgroundService> logger,
        IServiceProvider serviceProvider,
        ITimeProvider timeProvider)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Price alert background service started");

        // Initial delay
        await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckAlertsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking price alerts");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }
    }

    /// <inheritdoc />
    public async Task CheckAlertsAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Checking price alerts at {Time}", _timeProvider.Now);

        using var scope = _serviceProvider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IGameDealsRepository>();
        var dealsService = scope.ServiceProvider.GetRequiredService<IGameDealsService>();
        var notificationService = scope.ServiceProvider.GetService<INotificationService>();

        var alerts = await repository.GetActivePriceAlertsAsync(ct);

        foreach (var alert in alerts)
        {
            try
            {
                if (!alert.CanTrigger)
                {
                    _logger.LogDebug("Skipping alert {AlertId} - too soon since last trigger", alert.Id);
                    continue;
                }

                // Get current deals for this game
                var dealsResult = await dealsService.GetDealsForGameAsync(alert.GameTitle, ct);
                if (!dealsResult.IsSuccess || !dealsResult.Value.Any())
                {
                    continue;
                }

                var deals = dealsResult.Value;
                var triggered = false;
                var notifications = new List<string>();

                // Check target price
                if (alert.TargetPrice.HasValue)
                {
                    var matchingDeals = deals.Where(d => d.CurrentPrice <= alert.TargetPrice.Value).ToList();
                    if (matchingDeals.Any())
                    {
                        var bestDeal = matchingDeals.OrderBy(d => d.CurrentPrice).First();
                        notifications.Add($"🎯 Target price reached! {alert.GameTitle} is now {bestDeal.FormattedPrice} at {bestDeal.Store.Name}");
                        triggered = true;
                    }
                }

                // Check target discount
                if (alert.TargetDiscountPercent.HasValue)
                {
                    var matchingDeals = deals.Where(d => d.DiscountPercent >= alert.TargetDiscountPercent.Value).ToList();
                    if (matchingDeals.Any())
                    {
                        var bestDeal = matchingDeals.OrderByDescending(d => d.DiscountPercent).First();
                        notifications.Add($"🔥 {alert.GameTitle} is {bestDeal.FormattedDiscount} off at {bestDeal.Store.Name}! Now {bestDeal.FormattedPrice}");
                        triggered = true;
                    }
                }

                // Check historical low
                if (alert.AlertOnHistoricalLow)
                {
                    var historicalLowDeals = deals.Where(d => d.IsHistoricalLow).ToList();
                    if (historicalLowDeals.Any())
                    {
                        var bestDeal = historicalLowDeals.OrderBy(d => d.CurrentPrice).First();
                        notifications.Add($"🔥 Historical low! {alert.GameTitle} is at its lowest price ever: {bestDeal.FormattedPrice} at {bestDeal.Store.Name}");
                        triggered = true;
                    }
                }

                if (triggered)
                {
                    // Send notification
                    foreach (var message in notifications)
                    {
                        _logger.LogInformation("Alert triggered: {Message}", message);

                        notificationService?.SendNotification(new Notification
                        {
                            Title = "Price Alert!",
                            Message = message,
                            Type = NotificationType.Info,
                            Category = NotificationCategory.Deal
                        });
                    }

                    // Update last triggered time
                    await repository.UpdateAlertLastTriggeredAsync(alert.Id, ct);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing alert {AlertId}", alert.Id);
            }
        }

        _logger.LogInformation("Finished checking {Count} price alerts", alerts.Count);
    }
}

/// <summary>
/// Background service that periodically refreshes deals from all sources.
/// </summary>
public sealed class DealRefreshBackgroundService : BackgroundService
{
    private readonly ILogger<DealRefreshBackgroundService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly ITimeProvider _timeProvider;
    private readonly TimeSpan _refreshInterval = TimeSpan.FromHours(6);

    public DealRefreshBackgroundService(
        ILogger<DealRefreshBackgroundService> logger,
        IServiceProvider serviceProvider,
        ITimeProvider timeProvider)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Deal refresh background service started");

        // Initial refresh
        await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        await RefreshDealsAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(_refreshInterval, stoppingToken);

            try
            {
                await RefreshDealsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing deals");
            }
        }
    }

    private async Task RefreshDealsAsync(CancellationToken ct)
    {
        _logger.LogInformation("Refreshing deals at {Time}", _timeProvider.Now);

        using var scope = _serviceProvider.CreateScope();
        var dealsService = scope.ServiceProvider.GetRequiredService<IGameDealsService>();

        var result = await dealsService.RefreshDealsAsync(ct);

        if (result.IsSuccess)
        {
            _logger.LogInformation("Successfully refreshed deals");
        }
        else
        {
            _logger.LogWarning("Failed to refresh deals: {Error}", result.Error);
        }
    }
}
