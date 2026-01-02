using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;

namespace SaveState.Presentation.Services.Dashboard.Widgets;

/// <summary>
/// Widget showing recent gaming activity.
/// </summary>
public partial class ActivityFeedWidget : WidgetBase
{
    public ActivityFeedWidget(ILogger<ActivityFeedWidget> logger)
        : base(logger)
    {
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
        Activities.Clear();

        // TODO: Get real activity data from services
        // For now, simulate some activities
        Activities.Add(new ActivityItem(
            "🎮 You played Elden Ring for 2 hours",
            DateTime.Now.AddMinutes(-30),
            ActivityType.GameSession));

        Activities.Add(new ActivityItem(
            "🏆 Achievement Unlocked: Dragon Slayer",
            DateTime.Now.AddHours(-2),
            ActivityType.Achievement));

        Activities.Add(new ActivityItem(
            "👥 Friend @GamerX started playing Cyberpunk 2077",
            DateTime.Now.AddHours(-3),
            ActivityType.Social));

        Activities.Add(new ActivityItem(
            "💰 Sale Alert: Hollow Knight -75% ($3.74)",
            DateTime.Now.AddHours(-5),
            ActivityType.Deal));

        await Task.CompletedTask; // Simulate async operation
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
        var timeSpan = DateTime.Now - timestamp;

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