namespace SaveState.Application.Mugen.Services;

/// <summary>
/// Live match data.
/// </summary>
public class MobileCompanionServiceLiveMatchData
{
    public string MatchId { get; set; } = default!;
    public DateTime Timestamp { get; set; } = default!;
    public MobileCompanionServicePlayerMatchData PlayerData { get; set; } = default!;
    public MobileCompanionServicePlayerMatchData OpponentData { get; set; } = default!;
    public IReadOnlyList<MobileCompanionServiceMatchEvent> MatchEvents { get; set; } = default!;
}

/// <summary>
/// Player match data.
/// </summary>
public class MobileCompanionServicePlayerMatchData
{
    public int Health { get; set; } = default!;
    public int Meter { get; set; } = default!;
    public int Combo { get; set; } = default!;
    public MobileCompanionServiceMobileVector2 Position { get; set; } = default!;
    public string CurrentMove { get; set; } = default!;
}

/// <summary>
/// Match event.
/// </summary>
public class MobileCompanionServiceMatchEvent
{
    public MobileCompanionServiceMatchEventType EventType { get; set; } = default!;
    public string Description { get; set; } = default!;
    public DateTime Timestamp { get; set; } = default!;
    public int? Damage { get; set; } = default!;
}

/// <summary>
/// Live game stats.
/// </summary>
public class MobileCompanionServiceLiveGameStats
{
    public MobileCompanionServiceMatchStats? CurrentMatch { get; set; } = default!;
    public MobileCompanionServiceSessionStats MobileCompanionServiceSessionStats { get; set; } = default!;
}

/// <summary>
/// Match stats.
/// </summary>
public class MobileCompanionServiceMatchStats
{
    public string MatchId { get; set; } = default!;
    public int PlayerHealth { get; set; } = default!;
    public int OpponentHealth { get; set; } = default!;
    public TimeSpan MatchTime { get; set; } = default!;
    public int ComboCount { get; set; } = default!;
    public int MeterLevel { get; set; } = default!;
}

/// <summary>
/// Session stats.
/// </summary>
public class MobileCompanionServiceSessionStats
{
    public int MatchesPlayed { get; set; } = default!;
    public int Wins { get; set; } = default!;
    public int Losses { get; set; } = default!;
    public double WinRate { get; set; } = default!;
    public TimeSpan AverageMatchTime { get; set; } = default!;
    public int BestCombo { get; set; } = default!;
    public int TotalDamageDealt { get; set; } = default!;
}

/// <summary>
/// Vector2 for positions.
/// </summary>
public class MobileCompanionServiceMobileVector2
{
    public float X { get; set; } = default!;
    public float Y { get; set; } = default!;

    public MobileCompanionServiceMobileVector2() { }

    public MobileCompanionServiceMobileVector2(float x, float y)
    {
        X = x;
        Y = y;
    }
}

/// <summary>
/// Stream configuration.
/// </summary>
public class MobileCompanionServiceStreamConfig
{
    public int UpdateFrequencyMs { get; set; } = 1000;
    public bool IncludePositionData { get; set; } = true;
    public bool IncludeMoveData { get; set; } = true;
    public bool IncludeHealthData { get; set; } = true;
    public bool IncludeMeterData { get; set; } = true;
    public QualityLevel Quality { get; set; } = QualityLevel.High;

    public enum QualityLevel
    {
        Low,
        Medium,
        High,
        Ultra
    }
}
