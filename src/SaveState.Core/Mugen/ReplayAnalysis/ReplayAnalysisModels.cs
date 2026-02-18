using SaveState.Core.Common.Base;

namespace SaveState.Core.Mugen.ReplayAnalysis;

/// <summary>
/// Represents a complete analysis of a fighting game replay.
/// </summary>
public class ReplayAnalysis : EntityBase
{
    /// <summary>
    /// Path to the source replay file.
    /// </summary>
    public string ReplayFilePath { get; set; } = string.Empty;
    
    /// <summary>
    /// Display name for the replay analysis.
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Optional description or notes.
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Game/Platform the replay is from (MUGEN, IKEMEN, etc.)
    /// </summary>
    public string Platform { get; set; } = "MUGEN";
    
    /// <summary>
    /// When the replay was originally recorded.
    /// </summary>
    public DateTime ReplayDate { get; set; }
    
    /// <summary>
    /// When the analysis was performed.
    /// </summary>
    public DateTime AnalyzedAt { get; set; }
    
    /// <summary>
    /// Duration of the replay/match.
    /// </summary>
    public TimeSpan Duration { get; set; }
    
    /// <summary>
    /// Player 1 character name.
    /// </summary>
    public string Player1Character { get; set; } = string.Empty;
    
    /// <summary>
    /// Player 2 character name.
    /// </summary>
    public string Player2Character { get; set; } = string.Empty;
    
    /// <summary>
    /// Player 1 name/identifier.
    /// </summary>
    public string? Player1Name { get; set; }
    
    /// <summary>
    /// Player 2 name/identifier.
    /// </summary>
    public string? Player2Name { get; set; }
    
    /// <summary>
    /// Winner of the match (1, 2, or 0 for draw).
    /// </summary>
    public int Winner { get; set; }
    
    /// <summary>
    /// Number of rounds played.
    /// </summary>
    public int RoundsPlayed { get; set; }
    
    /// <summary>
    /// Total frames in the replay.
    /// </summary>
    public int TotalFrames { get; set; }
    
    /// <summary>
    /// Frame rate of the replay (usually 60 FPS).
    /// </summary>
    public int FrameRate { get; set; } = 60;
    
    /// <summary>
    /// Player 1 combat statistics.
    /// </summary>
    public PlayerCombatStats Player1Stats { get; set; } = new();
    
    /// <summary>
    /// Player 2 combat statistics.
    /// </summary>
    public PlayerCombatStats Player2Stats { get; set; } = new();
    
    /// <summary>
    /// Detected combos in the replay.
    /// </summary>
    public List<DetectedCombo> Combos { get; set; } = new();
    
    /// <summary>
    /// Key moments/highlight candidates.
    /// </summary>
    public List<HighlightMoment> Highlights { get; set; } = new();
    
    /// <summary>
    /// Detected comebacks.
    /// </summary>
    public List<ComebackMoment> Comebacks { get; set; } = new();
    
    /// <summary>
    /// Frame-by-frame data snapshots (optional, for deep analysis).
    /// </summary>
    public List<FrameSnapshot>? FrameData { get; set; }
    
    /// <summary>
    /// Analysis metadata and tags.
    /// </summary>
    public List<string> Tags { get; set; } = new();
    
    /// <summary>
    /// Analysis version for compatibility.
    /// </summary>
    public string AnalysisVersion { get; set; } = "1.0";
    
    /// <summary>
    /// File hash for integrity verification.
    /// </summary>
    public string? FileHash { get; set; }
    
    /// <summary>
    /// Gets the longest combo from this replay.
    /// </summary>
    public DetectedCombo? LongestCombo => Combos.OrderByDescending(c => c.HitCount).FirstOrDefault();
    
    /// <summary>
    /// Gets the highest damage combo from this replay.
    /// </summary>
    public DetectedCombo? HighestDamageCombo => Combos.OrderByDescending(c => c.TotalDamage).FirstOrDefault();
    
    /// <summary>
    /// Gets whether this was a perfect round (no damage taken).
    /// </summary>
    public bool HasPerfectRound => Player1Stats.PerfectRounds > 0 || Player2Stats.PerfectRounds > 0;
}

/// <summary>
/// Combat statistics for a single player in a replay.
/// </summary>
public class PlayerCombatStats
{
    public int TotalAttacks { get; set; }
    public int SuccessfulHits { get; set; }
    public int BlockedAttacks { get; set; }
    public int WhiffedAttacks { get; set; }
    public decimal HitRate => TotalAttacks > 0 ? (decimal)SuccessfulHits / TotalAttacks * 100 : 0;
    
    public int TotalDamageDealt { get; set; }
    public int TotalDamageTaken { get; set; }
    public int MaxComboDamage { get; set; }
    public int AverageComboDamage { get; set; }
    
    public int CombosPerformed { get; set; }
    public int TotalComboHits { get; set; }
    public int MaxComboHits { get; set; }
    public decimal AverageComboLength => CombosPerformed > 0 ? (decimal)TotalComboHits / CombosPerformed : 0;
    
    public int ThrowsAttempted { get; set; }
    public int ThrowsSuccessful { get; set; }
    public int ThrowEscapes { get; set; }
    
    public int SpecialMovesUsed { get; set; }
    public int SuperMovesUsed { get; set; }
    public int MeterBurned { get; set; }
    public int MeterGained { get; set; }
    
    public int BlocksPerformed { get; set; }
    public int PerfectBlocks { get; set; }
    public int Punishes { get; set; }
    public int PunishDamage { get; set; }
    
    public int JumpIns { get; set; }
    public int AntiAirs { get; set; }
    public int Crossups { get; set; }
    
    public int RoundsWon { get; set; }
    public int RoundsLost { get; set; }
    public int PerfectRounds { get; set; }
    
    public TimeSpan TimeInNeutral { get; set; }
    public TimeSpan TimeInPressure { get; set; }
    public TimeSpan TimeDefending { get; set; }
    
    /// <summary>
    /// Life lead/deficit at end of match.
    /// </summary>
    public int FinalLife { get; set; }
}

/// <summary>
/// A detected combo in a replay.
/// </summary>
public class DetectedCombo
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    /// <summary>
    /// Player who performed the combo (1 or 2).
    /// </summary>
    public int Player { get; set; }
    
    /// <summary>
    /// Character performing the combo.
    /// </summary>
    public string Character { get; set; } = string.Empty;
    
    /// <summary>
    /// Starting frame of the combo.
    /// </summary>
    public int StartFrame { get; set; }
    
    /// <summary>
    /// Ending frame of the combo.
    /// </summary>
    public int EndFrame { get; set; }
    
    /// <summary>
    /// Duration of the combo.
    /// </summary>
    public TimeSpan Duration => TimeSpan.FromSeconds((EndFrame - StartFrame) / 60.0);
    
    /// <summary>
    /// Number of hits in the combo.
    /// </summary>
    public int HitCount { get; set; }
    
    /// <summary>
    /// Total damage dealt by the combo.
    /// </summary>
    public int TotalDamage { get; set; }
    
    /// <summary>
    /// Damage scaled by combo scaling (if available).
    /// </summary>
    public decimal? ScalingPercentage { get; set; }
    
    /// <summary>
    /// Starting damage scaling (100% = no scaling).
    /// </summary>
    public decimal StartingScaling { get; set; } = 100m;
    
    /// <summary>
    /// Final damage scaling.
    /// </summary>
    public decimal FinalScaling { get; set; } = 100m;
    
    /// <summary>
    /// Moves used in the combo.
    /// </summary>
    public List<ComboMove> Moves { get; set; } = new();
    
    /// <summary>
    /// Resources used (meter, etc).
    /// </summary>
    public int ResourcesUsed { get; set; }
    
    /// <summary>
    /// Whether the combo started from a counter hit.
    /// </summary>
    public bool StartedFromCounterHit { get; set; }
    
    /// <summary>
    /// Whether the combo started from a punish.
    /// </summary>
    public bool StartedFromPunish { get; set; }
    
    /// <summary>
    /// Whether the combo is a Touch of Death (ToD).
    /// </summary>
    public bool IsTouchOfDeath { get; set; }
    
    /// <summary>
    /// Proration/damage scaling applied.
    /// </summary>
    public bool HasProration { get; set; }
    
    /// <summary>
    /// Difficulty assessment based on execution.
    /// </summary>
    public ComboDifficulty Difficulty { get; set; } = ComboDifficulty.Medium;
    
    /// <summary>
    /// Notable aspects of the combo.
    /// </summary>
    public List<string> Tags { get; set; } = new();
    
    /// <summary>
    /// Quality score (0-100) based on damage, length, and execution.
    /// </summary>
    public int QualityScore { get; set; }
}

/// <summary>
/// A single move within a combo.
/// </summary>
public class ComboMove
{
    /// <summary>
    /// Name of the move.
    /// </summary>
    public string MoveName { get; set; } = string.Empty;
    
    /// <summary>
    /// Input notation for the move.
    /// </summary>
    public string Input { get; set; } = string.Empty;
    
    /// <summary>
    /// Frame when the move connected.
    /// </summary>
    public int Frame { get; set; }
    
    /// <summary>
    /// Damage dealt by this hit.
    /// </summary>
    public int Damage { get; set; }
    
    /// <summary>
    /// Hitstun caused by this move.
    /// </summary>
    public int Hitstun { get; set; }
    
    /// <summary>
    /// Whether this move was a counter hit.
    /// </summary>
    public bool IsCounterHit { get; set; }
    
    /// <summary>
    /// Whether this move was part of a cancel.
    /// </summary>
    public bool IsCancel { get; set; }
    
    /// <summary>
    /// Cancel type if applicable.
    /// </summary>
    public CancelType CancelType { get; set; } = CancelType.None;
    
    /// <summary>
    /// Properties of the move (projectile, overhead, etc).
    /// </summary>
    public List<string> Properties { get; set; } = new();
}

/// <summary>
/// Combo difficulty levels.
/// </summary>
public enum ComboDifficulty
{
    Easy,
    Medium,
    Hard,
    VeryHard,
    TOD // Touch of Death
}

/// <summary>
/// A notable moment/highlight in a replay.
/// </summary>
public class HighlightMoment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    /// <summary>
    /// Type of highlight moment.
    /// </summary>
    public HighlightType Type { get; set; }
    
    /// <summary>
    /// Description of what happened.
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Start frame of the highlight.
    /// </summary>
    public int StartFrame { get; set; }
    
    /// <summary>
    /// End frame of the highlight.
    /// </summary>
    public int EndFrame { get; set; }
    
    /// <summary>
    /// Duration of the highlight.
    /// </summary>
    public TimeSpan Duration => TimeSpan.FromSeconds((EndFrame - StartFrame) / 60.0);
    
    /// <summary>
    /// Player primarily involved (1 or 2, 0 for both).
    /// </summary>
    public int PrimaryPlayer { get; set; }
    
    /// <summary>
    /// Character involved.
    /// </summary>
    public string Character { get; set; } = string.Empty;
    
    /// <summary>
    /// Intensity/importance score (0-100).
    /// </summary>
    public int IntensityScore { get; set; }
    
    /// <summary>
    /// Whether this has been exported to a highlight reel.
    /// </summary>
    public bool IsExported { get; set; }
    
    /// <summary>
    /// Path to exported highlight clip if available.
    /// </summary>
    public string? ExportPath { get; set; }
    
    /// <summary>
    /// Additional metadata specific to the highlight type.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Types of highlight moments.
/// </summary>
public enum HighlightType
{
    Combo,
    Comeback,
    PerfectRound,
    Clutch,
    Reversal,
    Punish,
    AntiAir,
    Reset,
    Mixup,
    SuperMove,
    InstantKill,
    DramaticFinish,
    DoubleKO,
    TimeOut,
    Trade,
    Meaty,
    Optimal,
    Improbable,
    Stylish
}

/// <summary>
/// A comeback moment detected in a replay.
/// </summary>
public class ComebackMoment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    /// <summary>
    /// Player who made the comeback (1 or 2).
    /// </summary>
    public int Player { get; set; }
    
    /// <summary>
    /// Character making the comeback.
    /// </summary>
    public string Character { get; set; } = string.Empty;
    
    /// <summary>
    /// Frame when the comeback started (life deficit was high).
    /// </summary>
    public int ComebackStartFrame { get; set; }
    
    /// <summary>
    /// Frame when the comeback was completed.
    /// </summary>
    public int ComebackEndFrame { get; set; }
    
    /// <summary>
    /// Life percentage at the lowest point (0-100).
    /// </summary>
    public decimal LowestLifePercentage { get; set; }
    
    /// <summary>
    /// Life percentage when comeback started.
    /// </summary>
    public decimal LifeAtStart { get; set; }
    
    /// <summary>
    /// Opponent's life percentage at comeback completion.
    /// </summary>
    public decimal OpponentLifeAtEnd { get; set; }
    
    /// <summary>
    /// How much life was recovered/defended.
    /// </summary>
    public decimal LifeRecovered { get; set; }
    
    /// <summary>
    /// Comeback severity rating.
    /// </summary>
    public ComebackSeverity Severity { get; set; }
    
    /// <summary>
    /// Key moments during the comeback.
    /// </summary>
    public List<string> KeyMoments { get; set; } = new();
    
    /// <summary>
    /// Overall comeback score (0-100).
    /// </summary>
    public int ComebackScore { get; set; }
}

/// <summary>
/// Severity levels for comebacks.
/// </summary>
public enum ComebackSeverity
{
    Minor,      // Small life deficit overcome
    Moderate,   // Significant deficit
    Major,      // Large deficit
    Impossible, // Extremely unlikely comeback
    Legendary   // Near-zero health to win
}

/// <summary>
/// Frame-by-frame snapshot for deep analysis.
/// </summary>
public class FrameSnapshot
{
    /// <summary>
    /// Frame number.
    /// </summary>
    public int FrameNumber { get; set; }
    
    /// <summary>
    /// Timestamp from replay start.
    /// </summary>
    public TimeSpan Timestamp => TimeSpan.FromSeconds(FrameNumber / 60.0);
    
    // Player 1 state
    public int P1Health { get; set; }
    public int P1Meter { get; set; }
    public int P1X { get; set; }
    public int P1Y { get; set; }
    public string P1State { get; set; } = string.Empty;
    public string? P1CurrentMove { get; set; }
    public bool P1IsAttacking { get; set; }
    public bool P1IsBlocking { get; set; }
    public bool P1IsHit { get; set; }
    public bool P1IsKnockedDown { get; set; }
    
    // Player 2 state
    public int P2Health { get; set; }
    public int P2Meter { get; set; }
    public int P2X { get; set; }
    public int P2Y { get; set; }
    public string P2State { get; set; } = string.Empty;
    public string? P2CurrentMove { get; set; }
    public bool P2IsAttacking { get; set; }
    public bool P2IsBlocking { get; set; }
    public bool P2IsHit { get; set; }
    public bool P2IsKnockedDown { get; set; }
    
    /// <summary>
    /// Distance between players.
    /// </summary>
    public int Distance => Math.Abs(P1X - P2X);
    
    /// <summary>
    /// Current round.
    /// </summary>
    public int Round { get; set; }
    
    /// <summary>
    /// Match timer value.
    /// </summary>
    public int? Timer { get; set; }
}

/// <summary>
/// Cancel types for combo moves.
/// </summary>
public enum CancelType
{
    None,
    Self,
    Special,
    Super,
    Any
}

/// <summary>
/// Request to analyze a replay file.
/// </summary>
public class ReplayAnalysisRequest
{
    /// <summary>
    /// Path to the replay file.
    /// </summary>
    public string ReplayFilePath { get; set; } = string.Empty;
    
    /// <summary>
    /// Optional display name.
    /// </summary>
    public string? Name { get; set; }
    
    /// <summary>
    /// Optional description.
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary> /// <summary>
    /// Analyze options for the replay.
    /// </summary>
    public ReplayAnalysisOptions Options { get; set; } = new();
}

/// <summary>
/// Options for replay analysis.
/// </summary>
public class ReplayAnalysisOptions
{
    /// <summary>
    /// Whether to detect and analyze combos.
    /// </summary>
    public bool DetectCombos { get; set; } = true;
    
    /// <summary>
    /// Whether to detect comeback moments.
    /// </summary>
    public bool DetectComebacks { get; set; } = true;
    
    /// <summary>
    /// Whether to generate highlight moments.
    /// </summary>
    public bool GenerateHighlights { get; set; } = true;
    
    /// <summary>
    /// Whether to capture frame-by-frame data (memory intensive).
    /// </summary>
    public bool CaptureFrameData { get; set; } = false;
    
    /// <summary>
    /// Minimum combo hits to consider.
    /// </summary>
    public int MinComboHits { get; set; } = 3;
    
    /// <summary>
    /// Minimum damage for a combo to be considered significant.
    /// </summary>
    public int MinComboDamage { get; set; } = 100;
    
    /// <summary>
    /// Minimum intensity score for a highlight.
    /// </summary>
    public int MinHighlightIntensity { get; set; } = 50;
    
    /// <summary>
    /// Specific player to focus analysis on (null for both).
    /// </summary>
    public int? FocusPlayer { get; set; }
    
    /// <summary>
    /// Character-specific analysis mode.
    /// </summary>
    public string? CharacterFocus { get; set; }
}

/// <summary>
/// Summary statistics for a replay analysis.
/// </summary>
public class ReplayAnalysisSummary
{
    public Guid AnalysisId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Player1Character { get; set; } = string.Empty;
    public string Player2Character { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
    public int TotalCombos { get; set; }
    public int LongestComboHits { get; set; }
    public int HighestComboDamage { get; set; }
    public int HighlightCount { get; set; }
    public int ComebackCount { get; set; }
    public DateTime AnalyzedAt { get; set; }
    public List<string> Tags { get; set; } = new();
}

/// <summary>
/// Filter criteria for searching replay analyses.
/// </summary>
public class ReplayAnalysisFilter
{
    public string? Character { get; set; }
    public string? PlayerName { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int? MinComboHits { get; set; }
    public int? MinComboDamage { get; set; }
    public List<string>? Tags { get; set; }
    public int? Winner { get; set; }
    public bool? HasComebacks { get; set; }
    public bool? HasPerfectRounds { get; set; }
}

/// <summary>
/// Comparison result between two replays.
/// </summary>
public class ReplayComparison
{
    public ReplayAnalysis Replay1 { get; set; } = null!;
    public ReplayAnalysis Replay2 { get; set; } = null!;
    
    public int ComboCountDifference => Replay1.Combos.Count - Replay2.Combos.Count;
    public int LongestComboDifference => (Replay1.LongestCombo?.HitCount ?? 0) - (Replay2.LongestCombo?.HitCount ?? 0);
    public int DamageDifference => Replay1.Player1Stats.TotalDamageDealt - Replay2.Player1Stats.TotalDamageDealt;
    public decimal HitRateDifference => Replay1.Player1Stats.HitRate - Replay2.Player1Stats.HitRate;
    
    public List<string> Improvements { get; set; } = new();
    public List<string> Regressions { get; set; } = new();
    public string Analysis { get; set; } = string.Empty;
}
