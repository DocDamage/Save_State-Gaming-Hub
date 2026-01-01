namespace SaveState.Core.GameLibrary.DTOs;

/// <summary>
/// Statistics about a game's play history.
/// </summary>
public sealed record PlaytimeStatistics(
    Guid GameId,
    TimeSpan TotalPlaytime,
    int TotalSessions,
    DateTime? FirstPlayedAt,
    DateTime? LastPlayedAt,
    TimeSpan AverageSessionDuration,
    TimeSpan LongestSessionDuration,
    int SessionsThisWeek,
    int SessionsThisMonth);
