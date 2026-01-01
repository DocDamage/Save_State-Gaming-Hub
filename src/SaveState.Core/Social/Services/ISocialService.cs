using SaveState.Core.Common;
using SaveState.Core.Social.Entities;

namespace SaveState.Core.Social.Services;

public interface ISocialService
{
    // Friend Management
    Task<Result<IReadOnlyList<Entities.Friend>>> GetFriendsAsync(CancellationToken ct = default);
    Task<Result> SendFriendRequestAsync(Guid userId, CancellationToken ct = default);
    Task<Result> RemoveFriendAsync(Guid friendId, CancellationToken ct = default);

    // Activity Sharing
    Task<Result> ShareAchievementAsync(string achievementName, string description, string rarity, CancellationToken ct = default);

    // Leaderboards
    Task<Result<IReadOnlyList<LeaderboardEntry>>> GetLeaderboardAsync(LeaderboardType type, string gameId = null, int limit = 50, CancellationToken ct = default);
}

public sealed record LeaderboardEntry(
    int Rank,
    Guid UserId,
    string Username,
    long Score,
    string ScoreType,
    IReadOnlyDictionary<string, object> Metadata,
    DateTime LastUpdated);

public enum LeaderboardType { GlobalScore, GameSpecific, Weekly, Monthly, FriendsOnly }