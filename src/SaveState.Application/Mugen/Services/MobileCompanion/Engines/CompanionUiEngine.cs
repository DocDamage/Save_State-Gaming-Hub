namespace SaveState.Application.Mugen.Services.MobileCompanion.Engines;

using Microsoft.Extensions.Logging;
using SaveState.Core.Common.Services;

/// <summary>
/// Engine for generating companion UI elements.
/// </summary>
public class CompanionUiEngine
{
    private readonly ILogger<CompanionUiEngine> _logger;
    private readonly ITimeProvider _timeProvider;

    public CompanionUiEngine(ILogger<CompanionUiEngine> logger, ITimeProvider timeProvider)
    {
        _logger = logger;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Gets quick actions for the dashboard.
    /// </summary>
    public Task<List<MobileCompanionServiceQuickAction>> GetQuickActionsAsync(
        MobileCompanionServiceMobileSession session,
        CancellationToken ct = default)
    {
        var actions = new List<MobileCompanionServiceQuickAction>
        {
            new()
            {
                ActionId = "quick_match",
                Title = "Quick Match",
                Description = "Find a match quickly",
                Icon = "gamepad",
                ActionType = MobileCompanionServiceQuickActionType.StartMatch,
                Parameters = new Dictionary<string, object>()
            },
            new()
            {
                ActionId = "training",
                Title = "Training Mode",
                Description = "Practice your skills",
                Icon = "dumbbell",
                ActionType = MobileCompanionServiceQuickActionType.OpenTraining,
                Parameters = new Dictionary<string, object>()
            },
            new()
            {
                ActionId = "character_select",
                Title = "Characters",
                Description = "Browse characters",
                Icon = "users",
                ActionType = MobileCompanionServiceQuickActionType.OpenCharacterSelect,
                Parameters = new Dictionary<string, object>()
            }
        };

        return Task.FromResult(actions);
    }

    /// <summary>
    /// Gets recent activity for a user.
    /// </summary>
    public Task<List<MobileCompanionServiceActivityItem>> GetRecentActivityAsync(string userId, CancellationToken ct = default)
    {
        var activities = new List<MobileCompanionServiceActivityItem>
        {
            new()
            {
                ActivityId = "1",
                Type = MobileCompanionServiceActivityType.MatchCompleted,
                Description = "Won vs Player2",
                Timestamp = _timeProvider.UtcNow.AddHours(-1),
                Metadata = new Dictionary<string, object>()
            },
            new()
            {
                ActivityId = "2",
                Type = MobileCompanionServiceActivityType.AchievementUnlocked,
                Description = "Unlocked First Win",
                Timestamp = _timeProvider.UtcNow.AddHours(-2),
                Metadata = new Dictionary<string, object>()
            }
        };

        return Task.FromResult(activities);
    }

    /// <summary>
    /// Gets social feed for a user.
    /// </summary>
    public Task<List<MobileCompanionServiceSocialActivity>> GetSocialFeedAsync(string userId, CancellationToken ct = default)
    {
        var activities = new List<MobileCompanionServiceSocialActivity>
        {
            new()
            {
                ActivityId = "social1",
                UserId = "friend1",
                UserName = "FriendOne",
                ActivityType = MobileCompanionServiceSocialActivityType.MatchResult,
                Description = "Won 5 matches in a row!",
                Timestamp = _timeProvider.UtcNow.AddMinutes(-30),
                Likes = 10,
                Comments = 2
            }
        };

        return Task.FromResult(activities);
    }

    /// <summary>
    /// Gets content queue for a user.
    /// </summary>
    public Task<List<MobileCompanionServiceContentItem>> GetContentQueueAsync(string userId, CancellationToken ct = default)
    {
        var items = new List<MobileCompanionServiceContentItem>
        {
            new()
            {
                ItemId = "content1",
                Title = "New Character Available",
                Description = "Check out the new fighter!",
                Type = MobileCompanionServiceContentType.Character,
                Priority = 1,
                ContentId = "content1",
                Name = "New Character Available",
                Status = MobileCompanionServiceDownloadStatus.Pending,
                Progress = 0,
                Size = 0
            }
        };

        return Task.FromResult(items);
    }
}

/// <summary>
/// Recent activity item.
/// </summary>
public class RecentActivityItem
{
    public string ActivityId { get; set; } = default!;
    public string Type { get; set; } = default!;
    public string Description { get; set; } = default!;
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Converts to MobileCompanionServiceActivityItem.
    /// </summary>
    public MobileCompanionServiceActivityItem ToServiceActivityItem()
    {
        return new MobileCompanionServiceActivityItem
        {
            ActivityId = ActivityId,
            Type = Enum.TryParse<MobileCompanionServiceActivityType>(Type, out var type) ? type : MobileCompanionServiceActivityType.MatchCompleted,
            Description = Description,
            Timestamp = Timestamp,
            Metadata = new Dictionary<string, object>()
        };
    }
}
