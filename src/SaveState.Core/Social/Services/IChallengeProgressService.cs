using SaveState.Core.Common;
using SaveState.Core.Social.Entities;

namespace SaveState.Core.Social.Services;

/// <summary>
/// Service for tracking and updating challenge progress based on game events.
/// </summary>
public interface IChallengeProgressService
{
    /// <summary>
    /// Updates challenge progress when a game session ends.
    /// </summary>
    Task<Result> UpdateProgressOnGameSessionAsync(Guid userId, Guid gameId, TimeSpan sessionDuration, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates challenge progress when an achievement is unlocked.
    /// </summary>
    Task<Result> UpdateProgressOnAchievementAsync(Guid userId, Guid gameId, string achievementId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates challenge progress when game stats change.
    /// </summary>
    Task<Result> UpdateProgressOnStatsChangeAsync(Guid userId, Guid gameId, Dictionary<string, object> stats, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current progress for a user's active challenges.
    /// </summary>
    Task<Result<List<ChallengeProgress>>> GetUserChallengeProgressAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Manually recalculates progress for all active challenges for a user.
    /// </summary>
    Task<Result> RecalculateUserProgressAsync(Guid userId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents the current progress of a user in a challenge.
/// </summary>
public class ChallengeProgress
{
    public Guid ChallengeId { get; set; }
    public string ChallengeName { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public double CurrentProgress { get; set; }
    public double TargetProgress { get; set; }
    public double ProgressPercentage => TargetProgress > 0 ? (CurrentProgress / TargetProgress) * 100 : 0;
    public bool IsCompleted { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime LastUpdatedAt { get; set; }
}
