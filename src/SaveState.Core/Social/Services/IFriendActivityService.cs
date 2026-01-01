using SaveState.Core.Common;
using SaveState.Core.Social.Entities;

namespace SaveState.Core.Social.Services;

/// <summary>
/// Service for managing friend activity and social features.
/// </summary>
public interface IFriendActivityService
{
    /// <summary>
    /// Gets the activity feed for friends.
    /// </summary>
    Task<Result<IReadOnlyList<FriendActivity>>> GetActivityFeedAsync(
        int limit = 50,
        CancellationToken ct = default);

    /// <summary>
    /// Gets all friends with their current status.
    /// </summary>
    Task<Result<IReadOnlyList<Friend>>> GetFriendsAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets only online friends.
    /// </summary>
    Task<Result<IReadOnlyList<Friend>>> GetOnlineFriendsAsync(CancellationToken ct = default);

    /// <summary>
    /// Syncs friends from Discord.
    /// </summary>
    Task<Result> SyncDiscordFriendsAsync(CancellationToken ct = default);

    /// <summary>
    /// Syncs friends from Steam (placeholder for future implementation).
    /// </summary>
    Task<Result> SyncSteamFriendsAsync(CancellationToken ct = default);

    /// <summary>
    /// Records an activity for a friend.
    /// </summary>
    Task<Result> RecordActivityAsync(
        Guid friendId,
        ActivityType type,
        string gameTitle,
        string? details = null,
        CancellationToken ct = default);

    /// <summary>
    /// Updates friend statuses from connected platforms.
    /// </summary>
    Task<Result> UpdateFriendStatusesAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets activity statistics.
    /// </summary>
    Task<Result<FriendActivityStatistics>> GetStatisticsAsync(CancellationToken ct = default);
}