namespace SaveState.Core.Analytics.DTOs;

public sealed record GamingHeatmapData(
    IReadOnlyDictionary<DateOnly, DailyActivity> Activities,
    int TotalDays,
    int ActiveDays,
    int CurrentStreak,
    int LongestStreak,
    TimeSpan TotalPlaytime);

public sealed record DailyActivity(
    DateOnly Date,
    TimeSpan TotalPlaytime,
    int SessionCount,
    IReadOnlyList<string> GamesPlayed,
    ActivityLevel Level);

public enum ActivityLevel
{
    None = 0,
    Low = 1,      // < 30 min
    Medium = 2,   // 30 min - 2 hours
    High = 3,     // 2 - 4 hours
    VeryHigh = 4  // > 4 hours
}

public sealed record TopGame(Guid GameId, string Title, TimeSpan TotalPlaytime, int SessionCount);