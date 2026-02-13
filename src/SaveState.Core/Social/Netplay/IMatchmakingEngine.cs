using SaveState.Core.Common;

namespace SaveState.Core.Social.Netplay;

/// <summary>
/// Engine for matchmaking with ROM hash verification and skill-based matching.
/// </summary>
public interface IMatchmakingEngine
{
    /// <summary>
    /// Enqueues a player for matchmaking.
    /// </summary>
    Task<Result<MatchmakingTicket>> EnqueueAsync(MatchmakingRequest request, CancellationToken ct = default);

    /// <summary>
    /// Removes a player from the matchmaking queue.
    /// </summary>
    Task<Result> DequeueAsync(string ticketId, CancellationToken ct = default);

    /// <summary>
    /// Finds the best match for a player in the queue.
    /// </summary>
    Task<Result<MatchCandidate?>> FindMatchAsync(string ticketId, CancellationToken ct = default);

    /// <summary>
    /// Accepts a proposed match.
    /// </summary>
    Task<Result<MatchConfirmation>> AcceptMatchAsync(string ticketId, string matchId, CancellationToken ct = default);

    /// <summary>
    /// Declines a proposed match.
    /// </summary>
    Task<Result> DeclineMatchAsync(string ticketId, string matchId, CancellationToken ct = default);

    /// <summary>
    /// Gets the current queue statistics.
    /// </summary>
    Task<Result<QueueStatistics>> GetQueueStatisticsAsync(string region, CancellationToken ct = default);

    /// <summary>
    /// Validates ROM hash compatibility between players.
    /// </summary>
    Task<Result<RomCompatibility>> ValidateRomCompatibilityAsync(string romHash1, string romHash2, CancellationToken ct = default);

    /// <summary>
    /// Calculates skill rating difference between two players.
    /// </summary>
    Task<Result<SkillMatchResult>> CalculateSkillMatchAsync(int player1Rating, int player2Rating, int maxDifference);
}

/// <summary>
/// Matchmaking request from a player.
/// </summary>
public sealed record MatchmakingRequest(
    string PlayerId,
    string Username,
    string RomHash,
    string Region,
    int SkillRating,
    MatchmakingCriteria Criteria,
    DateTime RequestedAt);

/// <summary>
/// Criteria for matchmaking.
/// </summary>
public sealed record MatchmakingCriteria(
    int MaxSkillDifference,
    int MaxWaitTimeSeconds,
    bool AllowCrossRegion,
    string[] PreferredRegions,
    bool AllowSpectators);

/// <summary>
/// Match candidate found by the engine.
/// </summary>
public sealed record MatchCandidate(
    string MatchId,
    string Player1Id,
    string Player2Id,
    string RomHash,
    int SkillDifference,
    int EstimatedQuality,
    DateTime FoundAt,
    DateTime ExpiresAt);

/// <summary>
/// Match confirmation after both players accept.
/// </summary>
public sealed record MatchConfirmation(
    string MatchId,
    string SessionId,
    string HostAddress,
    int Port,
    bool IsHost,
    DateTime ConfirmedAt);

/// <summary>
/// Queue statistics for a region.
/// </summary>
public sealed record QueueStatistics(
    string Region,
    int PlayersInQueue,
    int ActiveMatches,
    double AverageWaitTimeSeconds,
    int PeakHourPlayers,
    DateTime CalculatedAt);

/// <summary>
/// ROM compatibility result.
/// </summary>
public sealed record RomCompatibility(
    string RomHash1,
    string RomHash2,
    bool IsCompatible,
    RomCompatibilityLevel CompatibilityLevel,
    string? WarningMessage = null);

/// <summary>
/// Skill match calculation result.
/// </summary>
public sealed record SkillMatchResult(
    int Player1Rating,
    int Player2Rating,
    int Difference,
    bool IsAcceptable,
    double QualityScore);

/// <summary>
/// ROM compatibility levels.
/// </summary>
public enum RomCompatibilityLevel
{
    Identical,
    CompatibleVersion,
    DifferentRegion,
    DifferentVersion,
    Incompatible
}
