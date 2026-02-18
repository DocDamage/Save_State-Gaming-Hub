// Copyright (c) 2026 SaveStateReborn. All rights reserved.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SaveState.Core.Subscriptions;
using SaveState.Core.Common.Services;

namespace SaveState.Infrastructure.Subscriptions;

/// <summary>
/// Background service that periodically syncs subscription catalogs.
/// </summary>
public sealed class SubscriptionSyncBackgroundService : BackgroundService
{
    private readonly ILogger<SubscriptionSyncBackgroundService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly ITimeProvider _timeProvider;
    private readonly TimeSpan _syncInterval = TimeSpan.FromHours(6); // Sync every 6 hours

    public SubscriptionSyncBackgroundService(
        ILogger<SubscriptionSyncBackgroundService> logger,
        IServiceProvider serviceProvider,
        ITimeProvider timeProvider)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Subscription sync background service started");

        // Initial delay before first sync
        await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SyncSubscriptionsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during subscription sync");
            }

            _logger.LogInformation("Next subscription sync scheduled at {NextSyncTime}", 
                _timeProvider.Now.Add(_syncInterval));

            await Task.Delay(_syncInterval, stoppingToken);
        }
    }

    private async Task SyncSubscriptionsAsync(CancellationToken ct)
    {
        _logger.LogInformation("Starting subscription catalog sync at {SyncTime}", _timeProvider.Now);

        using var scope = _serviceProvider.CreateScope();
        var subscriptionService = scope.ServiceProvider.GetRequiredService<ISubscriptionService>();
        var providers = scope.ServiceProvider.GetServices<ISubscriptionProvider>().ToList();

        try
        {
            // Sync the library
            var syncResult = await subscriptionService.SyncLibraryAsync(ct);
            if (syncResult.IsSuccess)
            {
                _logger.LogInformation("Subscription library synced successfully");
            }
            else
            {
                _logger.LogWarning("Subscription library sync failed: {Error}", syncResult.Error);
            }

            // Get leaving soon games for notifications
            var leavingSoonResult = await subscriptionService.GetLeavingSoonGamesAsync(ct);
            if (leavingSoonResult.IsSuccess && leavingSoonResult.Value.Any())
            {
                _logger.LogInformation("Found {Count} games leaving soon", leavingSoonResult.Value.Count);
            }

            // Get new arrivals
            var newArrivalsResult = await subscriptionService.GetNewArrivalsAsync(ct);
            if (newArrivalsResult.IsSuccess && newArrivalsResult.Value.Any())
            {
                _logger.LogInformation("Found {Count} new arrivals", newArrivalsResult.Value.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync subscription catalogs");
        }
    }
}

/// <summary>
/// Service for checking and notifying about subscription alerts.
/// </summary>
public sealed class SubscriptionAlertService : BackgroundService
{
    private readonly ILogger<SubscriptionAlertService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly ITimeProvider _timeProvider;
    private readonly TimeSpan _checkInterval = TimeSpan.FromHours(1); // Check every hour

    public SubscriptionAlertService(
        ILogger<SubscriptionAlertService> logger,
        IServiceProvider serviceProvider,
        ITimeProvider timeProvider)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Subscription alert service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckAlertsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking subscription alerts");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }
    }

    private async Task CheckAlertsAsync(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var subscriptionService = scope.ServiceProvider.GetRequiredService<ISubscriptionService>();
        var notificationService = scope.ServiceProvider.GetService<INotificationService>();

        try
        {
            var alertsResult = await subscriptionService.GetLeavingSoonAlertsAsync(ct);
            if (!alertsResult.IsSuccess || !alertsResult.Value.Any())
                return;

            var urgentAlerts = alertsResult.Value
                .Where(a => (a.LeavingDate - _timeProvider.UtcNow).Days <= 3)
                .ToList();

            foreach (var alert in urgentAlerts)
            {
                _logger.LogInformation("Urgent alert: {GameTitle} leaving {ServiceName} on {LeavingDate:MMM dd}",
                    alert.GameTitle, alert.ServiceName, alert.LeavingDate);

                notificationService?.SendNotification(new Notification
                {
                    Title = "Game Leaving Soon!",
                    Message = $"{alert.GameTitle} is leaving {alert.ServiceName} on {alert.LeavingDate:MMM dd}",
                    Type = NotificationType.Warning,
                    Category = NotificationCategory.Subscription
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check subscription alerts");
        }
    }
}

/// <summary>
/// Notification service interface (if not already defined).
/// </summary>
public interface INotificationService
{
    void SendNotification(Notification notification);
}

/// <summary>
/// Represents a notification.
/// </summary>
public class Notification
{
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public NotificationCategory Category { get; set; }
}

/// <summary>
/// Types of notifications.
/// </summary>
public enum NotificationType
{
    Info,
    Success,
    Warning,
    Error
}

/// <summary>
/// Categories of notifications.
/// </summary>
public enum NotificationCategory
{
    General,
    Subscription,
    Achievement,
    Social,
    System,
    Deal
}
