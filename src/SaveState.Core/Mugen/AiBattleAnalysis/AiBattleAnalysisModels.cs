using SaveState.Core.Common.Base;

namespace SaveState.Core.Mugen.AiBattleAnalysis;

/// <summary>
/// Represents an analyzed battle/replay between two characters.
/// </summary>
public class AiBattleAnalysis : EntityBase
{
    /// <summary>
    /// Character being analyzed (the "player" character).
    /// </summary>
    public string CharacterName { get; set; } = string.Empty;
    
    /// <summary>
    /// Opponent character name.
    /// </summary>
    public string OpponentName { get; set; } = string.Empty;
    
    /// <summary>
    /// Source of the battle (replay file, live match, etc.).
    /// </summary>
    public string Source { get; set; } = string.Empty;
    
    /// <summary>
    /// When the battle occurred.
    /// </summary>
    public DateTime BattleDate { get; set; }
    
    /// <summary>
    /// Final result of the battle.
    /// </summary>
    public BattleResult Result { get; set; }
    
    /// <summary>
    /// Number of rounds won by player.
    /// </summary>
    public int RoundsWon { get; set; }
    
    /// <summary>
    /// Number of rounds lost by player.
    /// </summary>
    public int RoundsLost { get; set; }
    
    /// <summary>
    /// Duration of the battle.
    /// </summary>
    public TimeSpan Duration { get; set; }
    
    /// <summary>
    /// Combat statistics for the battle.
    /// </summary>
    public CombatStats Stats { get; set; } = new();
    
    /// <summary>
    /// Detected patterns in the battle.
    /// </summary>
    public List<DetectedPattern> Patterns { get; set; } = new();
    
    /// <summary>
    /// Weaknesses identified for the player.
    /// </summary>
    public List<PlayerWeakness> Weaknesses { get; set; } = new();
    
    /// <summary>
    /// Opportunities for improvement.
    /// </summary>
    public List<ImprovementOpportunity> Opportunities { get; set; } = new();
    
    /// <summary>
    /// Recommended counter-strategies.
    /// </summary>
    public List<CounterStrategy> Recommendations { get; set; } = new();
    
    /// <summary>
    /// AI-generated insights about the battle.
    /// </summary>
    public AiInsights Insights { get; set; } = new();
    
    /// <summary>
    /// Overall performance rating (0-100).
    /// </summary>
    public int PerformanceRating { get; set; }
}

/// <summary>
/// Battle result enumeration.
/// </summary>
public enum BattleResult
{
    Win,
    Loss,
    Draw,
    Incomplete
}

/// <summary>
/// Combat statistics for a battle.
/// </summary>
public class CombatStats
{
    public int TotalAttacks { get; set; }
    public int SuccessfulHits { get; set; }
    public int BlockedAttacks { get; set; }
    public int WhiffedAttacks { get; set; }
    public decimal HitRate => TotalAttacks > 0 ? (decimal)SuccessfulHits / TotalAttacks * 100 : 0;
    
    public int CombosPerformed { get; set; }
    public int MaxComboHits { get; set; }
    public int TotalDamageDealt { get; set; }
    public int TotalDamageTaken { get; set; }
    
    public int BlocksPerformed { get; set; }
    public int PerfectBlocks { get; set; }
    public int Punishes { get; set; }
    
    public int ThrowsAttempted { get; set; }
    public int ThrowsSuccessful { get; set; }
    
    public int SpecialMovesUsed { get; set; }
    public int SuperMovesUsed { get; set; }
    public int MeterBurned { get; set; }
    
    public int JumpIns { get; set; }
    public int AntiAirs { get; set; }
    public int Crossups { get; set; }
    
    public TimeSpan TimeInNeutral { get; set; }
    public TimeSpan TimeInPressure { get; set; }
    public TimeSpan TimeDefending { get; set; }
}

/// <summary>
/// A detected pattern in gameplay.
/// </summary>
public class DetectedPattern
{
    public string Name { get; set; } = string.Empty;
    public PatternType Type { get; set; }
    public string Description { get; set; } = string.Empty;
    public int Frequency { get; set; }
    public decimal SuccessRate { get; set; }
    public int FrameStart { get; set; }
    public int FrameEnd { get; set; }
    public List<string> Sequence { get; set; } = new();
    public bool IsPunishable { get; set; }
    public string? CounterStrategy { get; set; }
}

/// <summary>
/// Types of gameplay patterns.
/// </summary>
public enum PatternType
{
    Offensive,
    Defensive,
    Neutral,
    Pressure,
    Mixup,
    Combo,
    Movement,
    BadHabit
}

/// <summary>
/// A weakness identified in the player's gameplay.
/// </summary>
public class PlayerWeakness
{
    public string Description { get; set; } = string.Empty;
    public WeaknessCategory Category { get; set; }
    public SeverityLevel Severity { get; set; }
    public int Occurrences { get; set; }
    public decimal DamageTaken { get; set; }
    public string? SuggestedFix { get; set; }
    public List<string> Examples { get; set; } = new();
}

/// <summary>
/// Categories of weaknesses.
/// </summary>
public enum WeaknessCategory
{
    Defense,
    AntiAir,
    Punishment,
    Neutral,
    Pressure,
    Execution,
    Adaptation,
    MeterManagement
}

/// <summary>
/// Severity levels for issues.
/// </summary>
public enum SeverityLevel
{
    Low,
    Medium,
    High,
    Critical
}

/// <summary>
/// An opportunity for improvement.
/// </summary>
public class ImprovementOpportunity
{
    public string Area { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal PotentialImpact { get; set; }
    public DifficultyLevel Difficulty { get; set; }
    public List<string> PracticeDrills { get; set; } = new();
}

/// <summary>
/// Difficulty levels for improvements.
/// </summary>
public enum DifficultyLevel
{
    Easy,
    Medium,
    Hard,
    Expert
}

/// <summary>
/// A recommended counter-strategy.
/// </summary>
public class CounterStrategy
{
    public string Situation { get; set; } = string.Empty;
    public string Strategy { get; set; } = string.Empty;
    public string Execution { get; set; } = string.Empty;
    public int RiskLevel { get; set; }
    public int RewardLevel { get; set; }
    public List<string> RequiredInputs { get; set; } = new();
}

/// <summary>
/// AI-generated insights about the battle.
/// </summary>
public class AiInsights
{
    public string Summary { get; set; } = string.Empty;
    public string KeyTakeaway { get; set; } = string.Empty;
    public List<string> Strengths { get; set; } = new();
    public List<string> Weaknesses { get; set; } = new();
    public List<string> Adaptations { get; set; } = new();
    public string? MindsetAdvice { get; set; }
    public string? TechnicalAdvice { get; set; }
    public DateTime GeneratedAt { get; set; }
}

/// <summary>
/// Request to analyze a battle/replay.
/// </summary>
public class BattleAnalysisRequest
{
    public string CharacterName { get; set; } = string.Empty;
    public string OpponentName { get; set; } = string.Empty;
    public string ReplayFilePath { get; set; } = string.Empty;
    public byte[]? ReplayData { get; set; }
    public BattleAnalysisOptions Options { get; set; } = new();
}

/// <summary>
/// Options for battle analysis.
/// </summary>
public class BattleAnalysisOptions
{
    public bool DetectPatterns { get; set; } = true;
    public bool IdentifyWeaknesses { get; set; } = true;
    public bool GenerateRecommendations { get; set; } = true;
    public bool UseAiInsights { get; set; } = true;
    public int AnalysisDepth { get; set; } = 3; // 1-5
    public string? FocusArea { get; set; } // "defense", "offense", "neutral", etc.
}

/// <summary>
/// Comparison between two battle analyses.
/// </summary>
public class BattleComparison
{
    public AiBattleAnalysis Current { get; set; } = null!;
    public AiBattleAnalysis Previous { get; set; } = null!;
    public bool Improved => Current.PerformanceRating > Previous.PerformanceRating;
    public int RatingChange => Current.PerformanceRating - Previous.PerformanceRating;
    public List<string> Improvements { get; set; } = new();
    public List<string> Regressions { get; set; } = new();
}

/// <summary>
/// Training recommendations based on analysis.
/// </summary>
public class TrainingRecommendation
{
    public string Category { get; set; } = string.Empty;
    public string Focus { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Priority { get; set; }
    public TimeSpan EstimatedTime { get; set; }
    public List<TrainingDrill> Drills { get; set; } = new();
}

/// <summary>
/// A specific training drill.
/// </summary>
public class TrainingDrill
{
    public string Name { get; set; } = string.Empty;
    public string Setup { get; set; } = string.Empty;
    public string Goal { get; set; } = string.Empty;
    public int Repetitions { get; set; }
    public DifficultyLevel Difficulty { get; set; }
}
