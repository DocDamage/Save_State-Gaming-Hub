using SaveState.Core.Common.Base;
using SaveState.Core.Common.Services;

namespace SaveState.Core.Social.Entities;

/// <summary>
/// Represents an activity performed by a friend.
/// </summary>
public class FriendActivity : EntityBase
{
    /// <summary>
    /// Gets the ID of the friend who performed this activity.
    /// </summary>
    public Guid FriendId { get; private set; }

    /// <summary>
    /// Gets the friend who performed this activity.
    /// </summary>
    public Friend? Friend { get; private set; }

    /// <summary>
    /// Gets the type of activity.
    /// </summary>
    public ActivityType Type { get; private set; }

    /// <summary>
    /// Gets the title of the game involved in this activity.
    /// </summary>
    public string GameTitle { get; private set; } = string.Empty;

    /// <summary>
    /// Gets additional details about the activity.
    /// </summary>
    public string? Details { get; private set; }

    /// <summary>
    /// Gets the date and time when this activity occurred.
    /// </summary>
    public DateTime Timestamp { get; private set; }

    /// <summary>
    /// Gets the social platform where this activity was observed.
    /// </summary>
    public SocialPlatform Platform { get; private set; }

    private FriendActivity() { }

    /// <summary>
    /// Creates a new friend activity record.
    /// </summary>
    public static FriendActivity Create(
        Guid friendId,
        ActivityType type,
        string gameTitle,
        SocialPlatform platform,
        ITimeProvider timeProvider,
        string? details = null)
    {
        Guard.Against.Null(timeProvider, nameof(timeProvider));
        return new FriendActivity
        {
            Id = Guid.NewGuid(),
            FriendId = friendId,
            Type = type,
            GameTitle = Guard.Against.NullOrWhiteSpace(gameTitle, nameof(gameTitle)),
            Platform = platform,
            Details = details,
            Timestamp = timeProvider.UtcNow
        };
    }

    [Obsolete("Use Create(Guid, ActivityType, string, SocialPlatform, ITimeProvider, string?) instead")]
    public static FriendActivity Create(
        Guid friendId,
        ActivityType type,
        string gameTitle,
        SocialPlatform platform,
        string? details = null)
    {
        return new FriendActivity
        {
            Id = Guid.NewGuid(),
            FriendId = friendId,
            Type = type,
            GameTitle = Guard.Against.NullOrWhiteSpace(gameTitle, nameof(gameTitle)),
            Platform = platform,
            Details = details,
            Timestamp = DateTime.UtcNow
        };
    }
}

/// <summary>
/// Types of activities that friends can perform.
/// </summary>
public enum ActivityType
{
    StartedPlaying,
    StoppedPlaying,
    UnlockedAchievement,
    CompletedGame,
    AddedToLibrary,
    WroteReview,
    JoinedMultiplayer,
    // Future: Custom activities from plugins
}