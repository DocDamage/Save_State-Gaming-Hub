using SaveState.Core.Common;

namespace SaveState.Core.Mugen.AiBattleAnalysis.Services;

/// <summary>
/// Service for AI-powered battle analysis.
/// </summary>
public interface IAiBattleAnalysisService
{
    /// <summary>
    /// Analyzes a battle replay or match data.
    /// </summary>
    Task<Result<AiBattleAnalysis>> AnalyzeBattleAsync(
        BattleAnalysisRequest request, 
        CancellationToken ct = default);
    
    /// <summary>
    /// Gets all analyses for a character.
    /// </summary>
    Task<Result<List<AiBattleAnalysis>>> GetCharacterAnalysesAsync(
        string characterName, 
        string? opponentName = null,
        CancellationToken ct = default);
    
    /// <summary>
    /// Compares two battles to show improvement/regression.
    /// </summary>
    Task<Result<BattleComparison>> CompareBattlesAsync(
        Guid currentAnalysisId, 
        Guid previousAnalysisId,
        CancellationToken ct = default);
    
    /// <summary>
    /// Gets detected patterns for a character.
    /// </summary>
    Task<Result<List<DetectedPattern>>> GetCharacterPatternsAsync(
        string characterName,
        PatternType? type = null,
        CancellationToken ct = default);
    
    /// <summary>
    /// Gets weaknesses for a character with filtering.
    /// </summary>
    Task<Result<List<PlayerWeakness>>> GetCharacterWeaknessesAsync(
        string characterName,
        SeverityLevel? minSeverity = null,
        CancellationToken ct = default);
    
    /// <summary>
    /// Generates training recommendations based on analyses.
    /// </summary>
    Task<Result<List<TrainingRecommendation>>> GenerateTrainingPlanAsync(
        string characterName,
        int sessionMinutes = 30,
        CancellationToken ct = default);
    
    /// <summary>
    /// Analyzes matchup-specific advice.
    /// </summary>
    Task<Result<List<CounterStrategy>>> GetMatchupAdviceAsync(
        string characterName,
        string opponentName,
        CancellationToken ct = default);
    
    /// <summary>
    /// Gets performance trend over time.
    /// </summary>
    Task<Result<PerformanceTrend>> GetPerformanceTrendAsync(
        string characterName,
        DateTime? since = null,
        CancellationToken ct = default);
    
    /// <summary>
    /// Exports analysis to various formats.
    /// </summary>
    Task<Result<byte[]>> ExportAnalysisAsync(
        Guid analysisId,
        ExportFormat format,
        CancellationToken ct = default);
    
    /// <summary>
    /// Deletes an analysis.
    /// </summary>
    Task<Result> DeleteAnalysisAsync(Guid analysisId, CancellationToken ct = default);
    
    /// <summary>
    /// Real-time analysis during live matches.
    /// </summary>
    Task<Result<RealTimeAnalysis>> StartRealTimeAnalysisAsync(
        string characterName,
        string opponentName,
        CancellationToken ct = default);
    
    /// <summary>
    /// Feeds frame data to real-time analysis.
    /// </summary>
    Task<Result> FeedFrameDataAsync(
        Guid sessionId,
        FrameDataSnapshot snapshot,
        CancellationToken ct = default);
    
    /// <summary>
    /// Stops real-time analysis and returns results.
    /// </summary>
    Task<Result<AiBattleAnalysis>> StopRealTimeAnalysisAsync(
        Guid sessionId,
        CancellationToken ct = default);
}

/// <summary>
/// Performance trend data.
/// </summary>
public class PerformanceTrend
{
    public string CharacterName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int TotalBattles { get; set; }
    public int Wins { get; set; }
    public int Losses { get; set; }
    public decimal WinRate => TotalBattles > 0 ? (decimal)Wins / TotalBattles * 100 : 0;
    public List<TrendDataPoint> RatingOverTime { get; set; } = new();
    public List<TrendDataPoint> HitRateOverTime { get; set; } = new();
    public List<TrendDataPoint> DamageOverTime { get; set; } = new();
    public TrendDirection OverallTrend { get; set; }
    public string Analysis { get; set; } = string.Empty;
}

/// <summary>
/// A data point in a trend.
/// </summary>
public class TrendDataPoint
{
    public DateTime Date { get; set; }
    public decimal Value { get; set; }
    public string? Label { get; set; }
}

/// <summary>
/// Trend direction.
/// </summary>
public enum TrendDirection
{
    Improving,
    Stable,
    Declining,
    Inconsistent
}

/// <summary>
/// Export formats.
/// </summary>
public enum ExportFormat
{
    Json,
    Pdf,
    Markdown,
    Csv
}

/// <summary>
/// Real-time analysis session state.
/// </summary>
public class RealTimeAnalysis
{
    public Guid SessionId { get; set; }
    public string CharacterName { get; set; } = string.Empty;
    public string OpponentName { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public int FrameCount { get; set; }
    public CombatStats CurrentStats { get; set; } = new();
    public List<RealTimeInsight> Insights { get; set; } = new();
    public List<string> Suggestions { get; set; } = new();
}

/// <summary>
/// Real-time insight during analysis.
/// </summary>
public class RealTimeInsight
{
    public DateTime Timestamp { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public SeverityLevel Priority { get; set; }
}

/// <summary>
/// Snapshot of frame data for real-time analysis.
/// </summary>
public class FrameDataSnapshot
{
    public int FrameNumber { get; set; }
    public int PlayerHealth { get; set; }
    public int OpponentHealth { get; set; }
    public int PlayerMeter { get; set; }
    public int OpponentMeter { get; set; }
    public string PlayerState { get; set; } = string.Empty;
    public string OpponentState { get; set; } = string.Empty;
    public int DistanceBetween { get; set; }
    public string? CurrentAction { get; set; }
    public bool IsHit { get; set; }
    public bool IsBlocking { get; set; }
}
