using SaveState.Core.Common;
using SaveState.Core.Social.Entities;

namespace SaveState.Core.Social;

/// <summary>
/// Repository interface for friends and their activities.
/// </summary>
public interface IFriendRepository
{
    // Friend operations
    /// <summary>
    /// Gets a friend by their platform-specific ID.
    /// </summary>
    Task<Friend?> GetByPlatformIdAsync(SocialPlatform platform, string platformUserId, CancellationToken ct = default);

    /// <summary>
    /// Gets all friends with optional filtering.
    /// </summary>
    Task<PagedResult<Friend>> GetFriendsAsync(
        SocialPlatform? platform = null,
        bool? isOnline = null,
        CancellationToken ct = default);

    /// <summary>
    /// Adds or updates a friend record.
    /// </summary>
    Task<Friend> AddOrUpdateFriendAsync(Friend friend, CancellationToken ct = default);

    /// <summary>
    /// Updates a friend's status.
    /// </summary>
    Task UpdateFriendStatusAsync(Guid friendId, bool isOnline, string? currentGame, CancellationToken ct = default);

    /// <summary>
    /// Removes a friend.
    /// </summary>
    Task DeleteFriendAsync(Guid friendId, CancellationToken ct = default);

    // Activity operations
    /// <summary>
    /// Gets recent activities for all friends.
    /// </summary>
    Task<PagedResult<FriendActivity>> GetActivitiesAsync(
        int limit = 50,
        SocialPlatform? platform = null,
        ActivityType? activityType = null,
        CancellationToken ct = default);

    /// <summary>
    /// Gets activities for a specific friend.
    /// </summary>
    Task<IReadOnlyList<FriendActivity>> GetFriendActivitiesAsync(
        Guid friendId,
        int limit = 20,
        CancellationToken ct = default);

    /// <summary>
    /// Adds a new activity.
    /// </summary>
    Task AddActivityAsync(FriendActivity activity, CancellationToken ct = default);

    /// <summary>
    /// Cleans up old activities (older than specified days).
    /// </summary>
    Task<int> CleanupOldActivitiesAsync(int daysToKeep = 30, CancellationToken ct = default);

    /// <summary>
    /// Gets activity statistics.
    /// </summary>
    Task<FriendActivityStatistics> GetStatisticsAsync(CancellationToken ct = default);
}

/// <summary>
/// Statistics for friend activities.
/// </summary>
public sealed record FriendActivityStatistics(
    int TotalFriends,
    int OnlineFriends,
    int TotalActivities,
    Dictionary<ActivityType, int> ActivitiesByType,
    Dictionary<SocialPlatform, int> FriendsByPlatform);