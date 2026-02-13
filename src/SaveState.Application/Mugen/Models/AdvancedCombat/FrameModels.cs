namespace SaveState.Application.Mugen.Models.AdvancedCombat;

/// <summary>
/// Frame data for a move.
/// </summary>
public class FrameData
{
    public string MoveName { get; set; } = default!;
    public int TotalFrames { get; set; } = default!;
    public int StartupFrames { get; set; } = default!;
    public int ActiveFrames { get; set; } = default!;
    public int RecoveryFrames { get; set; } = default!;
    public IReadOnlyDictionary<string, int> FrameBreakdown { get; set; } = default!;
    public int HitAdvantage { get; set; } = default!;
    public int BlockAdvantage { get; set; } = default!;
    public DateTime GeneratedAt { get; set; } = default!;
}

/// <summary>
/// Frame advantage calculation.
/// </summary>
public class FrameAdvantage
{
    public string MoveName { get; set; } = default!;
    public int OnHit { get; set; } = default!;
    public int OnBlock { get; set; } = default!;
    public int OnWhiff { get; set; } = default!;
    public bool IsPlus { get; set; } = default!;
    public bool IsPunishable { get; set; } = default!;
}

/// <summary>
/// Frame data display.
/// </summary>
public class FrameDataDisplay
{
    public string DisplayId { get; set; } = default!;
    public string SessionId { get; set; } = default!;
    public FrameData FrameData { get; set; } = default!;
    public DisplayMode DisplayMode { get; set; } = default!;
    public bool ShowAdvanced { get; set; } = default!;
    public DateTime CreatedAt { get; set; } = default!;
    public bool Active { get; set; } = default!;
}

/// <summary>
/// Frame data request.
/// </summary>
public class FrameDataRequest
{
    public string MoveName { get; set; } = default!;
    public DisplayMode DisplayMode { get; set; } = default!;
    public bool ShowAdvanced { get; set; } = default!;
}

/// <summary>
/// Move analysis data.
/// </summary>
public class MoveAnalysis
{
    public string MoveName { get; set; } = default!;
    public int FrameAdvantage { get; set; } = default!;
    public float RiskRewardRatio { get; set; } = default!;
    public IReadOnlyList<string> OptimalFollowups { get; set; } = default!;
    public IReadOnlyList<string> CounterMoves { get; set; } = default!;
    public DateTime AnalyzedAt { get; set; } = default!;
}

/// <summary>
/// Move analysis request.
/// </summary>
public class MoveAnalysisRequest
{
    public string MoveName { get; set; } = default!;
    public AnalysisDepth Depth { get; set; } = default!;
}

/// <summary>
/// Frame data insights.
/// </summary>
public class FrameDataInsights
{
    public int DisplaysAccessed { get; set; } = default!;
    public int MovesAnalyzed { get; set; } = default!;
    public float TrainingEfficiency { get; set; } = default!;
    public float TimingImprovement { get; set; } = default!;
    public float AnalysisDepth { get; set; } = default!;
}
