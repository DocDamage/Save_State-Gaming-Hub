namespace SaveState.Application.Mugen.Services.MatchAnalytics;

/// <summary>
/// Types of statistics that can be calculated for player performance analysis.
/// </summary>
public enum StatType
{
    WinRate,
    DamageDealt,
    DamageReceived,
    ComboEfficiency,
    SpecialMoveUsage,
    InputAccuracy,
    DefensiveEffectiveness,
    OffensivePressure,
    Consistency,
    Adaptability
}

/// <summary>
/// Types of patterns that can be detected in player behavior.
/// </summary>
public enum PatternType
{
    ComboHeavy,
    SpecialSpammer,
    DefensivePlayer,
    RushdownStyle,
    PokerStyle,
    InputHeavy,
    TimingBased,
    CharacterSpecialist,
    ComebackKing,
    MomentumPlayer
}

/// <summary>
/// Types of analytics reports that can be generated.
/// </summary>
public enum ReportType
{
    MatchSummary,
    PlayerPerformance,
    TrendAnalysis,
    PatternReport,
    ImprovementPlan,
    ComparativeAnalysis,
    CharacterMatchup,
    FullAnalytics
}

/// <summary>
/// Priority levels for recommendations and insights.
/// </summary>
public enum AnalyticsPriority
{
    Low,
    Medium,
    High,
    Critical
}

/// <summary>
/// Status of a match recording or analysis operation.
/// </summary>
public enum AnalyticsStatus
{
    Pending,
    InProgress,
    Completed,
    Failed,
    Cached
}
