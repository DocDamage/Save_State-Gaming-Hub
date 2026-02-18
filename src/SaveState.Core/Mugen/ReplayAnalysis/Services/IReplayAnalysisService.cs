using SaveState.Core.Common;

namespace SaveState.Core.Mugen.ReplayAnalysis.Services;

/// <summary>
/// Service for analyzing fighting game replays and generating highlights.
/// </summary>
public interface IReplayAnalysisService
{
    /// <summary>
    /// Analyzes a replay file and returns detailed analysis.
    /// </summary>
    Task<Result<ReplayAnalysis>> AnalyzeReplayAsync(
        ReplayAnalysisRequest request, 
        CancellationToken ct = default);
    
    /// <summary>
    /// Gets a replay analysis by ID.
    /// </summary>
    Task<Result<ReplayAnalysis>> GetAnalysisAsync(
        Guid analysisId, 
        CancellationToken ct = default);
    
    /// <summary>
    /// Gets all replay analyses with optional filtering.
    /// </summary>
    Task<Result<List<ReplayAnalysisSummary>>> GetAnalysesAsync(
        ReplayAnalysisFilter? filter = null, 
        CancellationToken ct = default);
    
    /// <summary>
    /// Deletes a replay analysis.
    /// </summary>
    Task<Result> DeleteAnalysisAsync(
        Guid analysisId, 
        CancellationToken ct = default);
    
    /// <summary>
    /// Gets all combos from a replay analysis.
    /// </summary>
    Task<Result<List<DetectedCombo>>> GetCombosAsync(
        Guid analysisId, 
        int? player = null,
        int? minHits = null,
        CancellationToken ct = default);
    
    /// <summary>
    /// Gets highlight moments from a replay.
    /// </summary>
    Task<Result<List<HighlightMoment>>> GetHighlightsAsync(
        Guid analysisId, 
        HighlightType? type = null,
        int? minIntensity = null,
        CancellationToken ct = default);
    
    /// <summary>
    /// Gets comeback moments from a replay.
    /// </summary>
    Task<Result<List<ComebackMoment>>> GetComebacksAsync(
        Guid analysisId, 
        ComebackSeverity? minSeverity = null,
        CancellationToken ct = default);
    
    /// <summary>
    /// Compares two replay analyses.
    /// </summary>
    Task<Result<ReplayComparison>> CompareReplaysAsync(
        Guid analysisId1, 
        Guid analysisId2,
        CancellationToken ct = default);
    
    /// <summary>
    /// Generates a highlight reel from selected moments.
    /// </summary>
    Task<Result<HighlightReel>> GenerateHighlightReelAsync(
        Guid analysisId,
        List<Guid> highlightIds,
        HighlightReelOptions options,
        CancellationToken ct = default);
    
    /// <summary>
    /// Auto-generates a highlight reel based on intensity scores.
    /// </summary>
    Task<Result<HighlightReel>> AutoGenerateHighlightReelAsync(
        Guid analysisId,
        int maxDurationSeconds = 60,
        CancellationToken ct = default);
    
    /// <summary>
    /// Exports a highlight reel to a video file.
    /// </summary>
    Task<Result<string>> ExportHighlightReelAsync(
        Guid reelId,
        string outputPath,
        ExportFormat format,
        CancellationToken ct = default);
    
    /// <summary>
    /// Gets combo statistics across multiple replays.
    /// </summary>
    Task<Result<ComboStatistics>> GetComboStatisticsAsync(
        string character,
        int? minReplays = null,
        CancellationToken ct = default);
    
    /// <summary>
    /// Tags a replay analysis with custom tags.
    /// </summary>
    Task<Result> TagAnalysisAsync(
        Guid analysisId, 
        List<string> tags,
        CancellationToken ct = default);
    
    /// <summary>
    /// Searches for similar replays based on character matchup and style.
    /// </summary>
    Task<Result<List<ReplayAnalysisSummary>>> FindSimilarReplaysAsync(
        Guid analysisId,
        int maxResults = 10,
        CancellationToken ct = default);
    
    /// <summary>
    /// Validates if a replay file is supported and readable.
    /// </summary>
    Task<Result<ReplayFileInfo>> ValidateReplayFileAsync(
        string filePath,
        CancellationToken ct = default);
    
    /// <summary>
    /// Gets frame data for a specific time range.
    /// </summary>
    Task<Result<List<FrameSnapshot>>> GetFrameRangeAsync(
        Guid analysisId,
        int startFrame,
        int endFrame,
        CancellationToken ct = default);
}

/// <summary>
/// Highlight reel containing multiple moments.
/// </summary>
public class HighlightReel
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SourceAnalysisId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public TimeSpan TotalDuration { get; set; }
    public List<HighlightMoment> Moments { get; set; } = new();
    public List<TransitionEffect> Transitions { get; set; } = new();
    public string? BackgroundMusic { get; set; }
    public bool IsExported { get; set; }
    public string? ExportPath { get; set; }
    public int QualityScore { get; set; }
}

/// <summary>
/// Transition effect between highlights.
/// </summary>
public class TransitionEffect
{
    public int FromIndex { get; set; }
    public int ToIndex { get; set; }
    public TransitionType Type { get; set; }
    public TimeSpan Duration { get; set; }
}

/// <summary>
/// Types of transitions between highlights.
/// </summary>
public enum TransitionType
{
    Cut,
    Fade,
    Dissolve,
    Wipe,
    Zoom,
    SlowMotion,
    Flash
}

/// <summary>
/// Options for generating highlight reels.
/// </summary>
public class HighlightReelOptions
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TimeSpan? MaxDuration { get; set; }
    public bool AddTransitions { get; set; } = true;
    public TransitionType DefaultTransition { get; set; } = TransitionType.Fade;
    public bool AddBackgroundMusic { get; set; } = false;
    public string? BackgroundMusicPath { get; set; }
    public bool IncludeSlowMotion { get; set; } = true;
    public bool ExportWithHud { get; set; } = true;
    public VideoQuality Quality { get; set; } = VideoQuality.High;
}

/// <summary>
/// Video quality settings.
/// </summary>
public enum VideoQuality
{
    Low,      // 720p
    Medium,   // 1080p
    High,     // 1440p
    Ultra     // 4K
}

/// <summary>
/// Export formats for highlight reels.
/// </summary>
public enum ExportFormat
{
    Mp4,
    WebM,
    Gif,
    Avi,
    Mov
}

/// <summary>
/// Combo statistics across multiple replays.
/// </summary>
public class ComboStatistics
{
    public string Character { get; set; } = string.Empty;
    public int TotalReplaysAnalyzed { get; set; }
    public int TotalCombosFound { get; set; }
    public int LongestComboHits { get; set; }
    public decimal AverageComboHits { get; set; }
    public int HighestComboDamage { get; set; }
    public decimal AverageComboDamage { get; set; }
    public List<ComboRoute> MostCommonRoutes { get; set; } = new();
    public List<ComboMoveStatistics> MoveUsage { get; set; } = new();
    public Dictionary<ComboDifficulty, int> DifficultyDistribution { get; set; } = new();
    public List<DetectedCombo> TopCombos { get; set; } = new();
}

/// <summary>
/// Common combo route/starter.
/// </summary>
public class ComboRoute
{
    public List<string> MoveSequence { get; set; } = new();
    public int OccurrenceCount { get; set; }
    public decimal AverageDamage { get; set; }
    public decimal SuccessRate { get; set; }
}

/// <summary>
/// Statistics for individual moves in combos.
/// </summary>
public class ComboMoveStatistics
{
    public string MoveName { get; set; } = string.Empty;
    public int TimesUsed { get; set; }
    public decimal UsagePercentage { get; set; }
    public decimal AverageDamage { get; set; }
    public int MaxHitsInCombo { get; set; }
}

/// <summary>
/// Information about a replay file.
/// </summary>
public class ReplayFileInfo
{
    public string FilePath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string FileHash { get; set; } = string.Empty;
    public DateTime FileDate { get; set; }
    public bool IsValid { get; set; }
    public string? ValidationError { get; set; }
    public string Platform { get; set; } = string.Empty;
    public string? Version { get; set; }
    public TimeSpan? Duration { get; set; }
    public int? TotalFrames { get; set; }
    public int FrameRate { get; set; } = 60;
    public string? Player1Character { get; set; }
    public string? Player2Character { get; set; }
    public List<string> SupportedFormats { get; set; } = new();
}
