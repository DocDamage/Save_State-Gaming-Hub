namespace SaveState.Application.Mugen.Models.NetworkFeatures;

/// <summary>
/// Active matchmaking session.
/// </summary>
public class MatchmakingSession
{
    public string SessionId { get; set; } = string.Empty;
    public string PlayerId { get; set; } = string.Empty;
    public string CharacterName { get; set; } = string.Empty;
    public MatchmakingMode Mode { get; set; }
    public required MatchmakingPreferences Preferences { get; set; }
    public required PlayerMatchmakingStats PlayerStats { get; set; }
    public DateTime StartTime { get; set; }
    public TimeSpan Timeout { get; set; }
    public bool MatchFound { get; set; }
    public string? MatchId { get; set; }
    public string? OpponentId { get; set; }
    public string? OpponentName { get; set; }
}

/// <summary>
/// Player currently in matchmaking queue.
/// </summary>
public class QueuedPlayer
{
    public string PlayerId { get; set; } = string.Empty;
    public string PlayerName { get; set; } = string.Empty;
    public string CharacterName { get; set; } = string.Empty;
    public MatchmakingMode Mode { get; set; }
    public required MatchmakingPreferences Preferences { get; set; }
    public required PlayerMatchmakingStats PlayerStats { get; set; }
    public DateTime QueuedAt { get; set; }
}

/// <summary>
/// Character matchup data for matchmaking optimization.
/// </summary>
public class CharacterMatchupData
{
    public string Character1 { get; set; } = string.Empty;
    public string Character2 { get; set; } = string.Empty;
    public float WinRate { get; set; }
    public int TotalMatches { get; set; }
    public TimeSpan AverageGameLength { get; set; }
}

/// <summary>
/// Match quality calculation result.
/// </summary>
public class MatchQualityResult
{
    public required string Player1Id { get; set; }
    public required string Player2Id { get; set; }
    public float QualityScore { get; set; }
    public float RatingCompatibility { get; set; }
    public float WinRateCompatibility { get; set; }
    public float CharacterMatchupScore { get; set; }
    public float RegionCompatibility { get; set; }
    public float QueueTimeFairness { get; set; }
}
