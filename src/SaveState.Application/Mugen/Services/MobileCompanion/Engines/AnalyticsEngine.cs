namespace SaveState.Application.Mugen.Services.MobileCompanion.Engines;

using Microsoft.Extensions.Logging;

/// <summary>
/// Engine for mobile analytics.
/// </summary>
public class AnalyticsEngine
{
    private readonly ILogger<AnalyticsEngine> _logger;

    public AnalyticsEngine(ILogger<AnalyticsEngine> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Generates mobile analytics for a user.
    /// </summary>
    public Task<MobileCompanionServiceMobileAnalytics> GenerateMobileAnalyticsAsync(
        string userId,
        TimeSpan period,
        CancellationToken ct = default)
    {
        var analytics = new MobileCompanionServiceMobileAnalytics
        {
            UserId = userId,
            Period = period,
            SessionCount = 10,
            AverageSessionLength = TimeSpan.FromMinutes(25),
            CommandsSent = 45,
            NotificationsReceived = 12,
            ContentDownloaded = 3,
            RemoteMatchesStarted = 8,
            PeakUsageHours = new List<int> { 18, 19, 20, 21 },
            DeviceBreakdown = new Dictionary<MobileCompanionServiceMobilePlatform, int>
            {
                { MobileCompanionServiceMobilePlatform.iOS, 5 },
                { MobileCompanionServiceMobilePlatform.Android, 3 }
            },
            FeatureUsage = new Dictionary<string, int>
            {
                { "RemoteControl", 15 },
                { "RealTimeStats", 20 },
                { "SocialFeatures", 8 }
            }
        };

        return Task.FromResult(analytics);
    }
}
