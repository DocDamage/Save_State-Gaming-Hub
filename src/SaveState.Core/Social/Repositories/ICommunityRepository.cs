using SaveState.Core.Common;
using SaveState.Core.Social.Entities;

namespace SaveState.Core.Social.Repositories;

/// <summary>
/// Repository for community features like challenges and leaderboards.
/// </summary>
public interface ICommunityRepository
{
    // Challenges
    Task<Result<Challenge>> GetChallengeByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<IReadOnlyList<Challenge>>> GetActiveChallengesAsync(CancellationToken ct = default);
    Task<Result<Guid>> CreateChallengeAsync(Challenge challenge, CancellationToken ct = default);
    Task<Result> UpdateChallengeAsync(Challenge challenge, CancellationToken ct = default);
    Task<Result> JoinChallengeAsync(Guid challengeId, Guid userId, CancellationToken ct = default);
    Task<Result> UpdateChallengeProgressAsync(Guid challengeId, Guid userId, double progress, CancellationToken ct = default);

    // Leaderboards
    Task<Result<Leaderboard>> GetLeaderboardAsync(Guid id, CancellationToken ct = default);
    Task<Result<Leaderboard>> GetLeaderboardByCategoryAsync(LeaderboardCategory category, CancellationToken ct = default);
    Task<Result<Guid>> CreateLeaderboardAsync(Leaderboard leaderboard, CancellationToken ct = default);
    Task<Result> UpdateLeaderboardAsync(Leaderboard leaderboard, CancellationToken ct = default);
    Task<Result> UpdateLeaderboardEntryAsync(LeaderboardRanking entry, CancellationToken ct = default);
}
