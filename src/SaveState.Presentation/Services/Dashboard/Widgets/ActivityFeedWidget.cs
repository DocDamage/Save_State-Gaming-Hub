using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common.Services;
using SaveState.Core.GameLibrary;
using System.Collections.ObjectModel;

namespace SaveState.Presentation.Services.Dashboard.Widgets;

/// <summary>
/// Widget showing recent gaming activity.
/// </summary>
public partial class ActivityFeedWidget : WidgetBase
{
    private readonly IGameSessionRepository _sessionRepository;
    private readonly IAchievementRepository _achievementRepository;
    private readonly IGameRepository _gameRepository;
    private readonly ITimeProvider _timeProvider;

    public ActivityFeedWidget(
        IGameSessionRepository sessionRepository,
        IAchievementRepository achievementRepository,
        IGameRepository gameRepository,
        ILogger<ActivityFeedWidget> logger,
        ITimeProvider timeProvider)
        : base(logger)
    {
        _sessionRepository = sessionRepository;
        _achievementRepository = achievementRepository;
        _gameRepository = gameRepository;
        _timeProvider = timeProvider;
        Activities = new ObservableCollection<ActivityItem>();
    }

    /// <inheritdoc />
    public override string Id => "activity-feed";

    /// <inheritdoc />
    public override string Title => "Activity Feed";

    /// <inheritdoc />
    public override string Icon => "📰";

    /// <inheritdoc />
    public override WidgetSize DefaultSize => WidgetSize.Full;

    /// <inheritdoc />
    public override WidgetSize[] SupportedSizes => new[] { WidgetSize.Full, WidgetSize.Large };

    /// <inheritdoc />
    public override int RefreshIntervalMs => 300000; // 5 minutes

    /// <summary>
    /// Gets the collection of recent activities.
    /// </summary>
    public ObservableCollection<ActivityItem> Activities { get; }

    /// <inheritdoc />
    protected override async Task LoadDataAsync()
    {
        try
        {
            Activities.Clear();

            // Get recent game sessions (last 7 days)
            var recentDate = _timeProvider.UtcNow.AddDays(-7);
            var sessions = await _sessionRepository.GetRecentSessionsAsync(10);

            foreach (var session in sessions.OrderByDescending(s => s.EndedAt ?? s.StartedAt).Take(5))
            {
                var game = await _gameRepository.GetByIdAsync(SaveState.Core.Common.ValueObjects.GameId.From(session.GameId));
                var gameName = game?.Title ?? "Unknown Game";
                var duration = session.GetDuration();

                var durationText = duration.TotalHours >= 1
                    ? $"{duration.TotalHours:F1} hours"
                    : $"{duration.TotalMinutes:F0} minutes";

                Activities.Add(new ActivityItem(
                    $"🎮 Played {gameName} for {durationText}",
                    session.EndedAt ?? session.StartedAt,
                    ActivityType.GameSession));
            }

            // Get recent achievements
            var achievements = await _achievementRepository.GetRecentUnlockedAsync(5);
            foreach (var achievement in achievements)
            {
                var achievementName = achievement.Achievement?.Name ?? "Unknown Achievement";
                Activities.Add(new ActivityItem(
                    $"🏆 Achievement Unlocked: {achievementName}",
                    achievement.UnlockedAt ?? _timeProvider.UtcNow,
                    ActivityType.Achievement));
            }

            // Sort all activities by timestamp
            var sortedActivities = Activities.OrderByDescending(a => a.Timestamp).ToList();
            Activities.Clear();
            foreach (var activity in sortedActivities.Take(10))
            {
                Activities.Add(activity);
            }

            Logger.LogInformation("Loaded {Count} activity items", Activities.Count);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load activity feed data");

            // Fallback to empty or minimal data
            Activities.Clear();
            Activities.Add(new ActivityItem(
                "📊 No recent activity found",
                _timeProvider.UtcNow,
                ActivityType.Update));
        }
    }
}

/// <summary>
/// Represents an activity item in the feed.
/// </summary>
public record ActivityItem(string Message, DateTime Timestamp, ActivityType Type)
{
    /// <summary>
    /// Gets the formatted time string.
    /// </summary>
    public string TimeAgo => GetTimeAgo(Timestamp);

    private static string GetTimeAgo(DateTime timestamp)
    {
        var timeSpan = DateTime.UtcNow - timestamp.ToUniversalTime();

        if (timeSpan.TotalMinutes < 1)
            return "Just now";
        if (timeSpan.TotalMinutes < 60)
            return $"{(int)timeSpan.TotalMinutes}m ago";
        if (timeSpan.TotalHours < 24)
            return $"{(int)timeSpan.TotalHours}h ago";
        if (timeSpan.TotalDays < 7)
            return $"{(int)timeSpan.TotalDays}d ago";

        return timestamp.ToString("MMM dd");
    }
}

/// <summary>
/// Activity type enumeration.
/// </summary>
public enum ActivityType
{
    GameSession,
    Achievement,
    Social,
    Deal,
    Update
}
