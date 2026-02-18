using SaveState.Core.Common.Base;

namespace SaveState.Core.Mugen.ComboDatabase;

/// <summary>
/// Represents a discovered/stored combo for a character.
/// </summary>
public class ComboEntry : EntityBase
{
    /// <summary>
    /// Character this combo belongs to.
    /// </summary>
    public string CharacterName { get; set; } = string.Empty;
    
    /// <summary>
    /// Character variation/version if applicable.
    /// </summary>
    public string? CharacterVersion { get; set; }
    
    /// <summary>
    /// Display name for the combo.
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Optional description or notes.
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// Combo difficulty rating.
    /// </summary>
    public ComboDifficulty Difficulty { get; set; } = ComboDifficulty.Medium;
    
    /// <summary>
    /// Number of hits in the combo.
    /// </summary>
    public int HitCount { get; set; }
    
    /// <summary>
    /// Total damage dealt.
    /// </summary>
    public int Damage { get; set; }
    
    /// <summary>
    /// Damage scaling percentage (100 = no scaling).
    /// </summary>
    public decimal ScalingPercentage { get; set; } = 100m;
    
    /// <summary>
    /// Starting requirement (e.g., "Standing", "Crouching", "Air", "Counter Hit").
    /// </summary>
    public string StartingPosition { get; set; } = "Standing";
    
    /// <summary>
    /// Meter/resources required.
    /// </summary>
    public int MeterRequired { get; set; }
    
    /// <summary>
    /// Drive/meter gained during combo.
    /// </summary>
    public int MeterGained { get; set; }
    
    /// <summary>
    /// List of moves in the combo sequence.
    /// </summary>
    public List<ComboMoveEntry> Moves { get; set; } = new();
    
    /// <summary>
    /// Full input notation string.
    /// </summary>
    public string InputNotation { get; set; } = string.Empty;
    
    /// <summary>
    /// Video demonstration URL or path.
    /// </summary>
    public string? VideoUrl { get; set; }
    
    /// <summary>
    /// Screenshot/image path.
    /// </summary>
    public string? ImagePath { get; set; }
    
    /// <summary>
    /// Creator/discoverer of the combo.
    /// </summary>
    public string? Creator { get; set; }
    
    /// <summary>
    /// Source (e.g., "Training Mode", "Match", "Community").
    /// </summary>
    public string Source { get; set; } = "Training Mode";
    
    /// <summary>
    /// When the combo was discovered/created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// When the combo was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Whether this is a community submission pending approval.
    /// </summary>
    public bool IsPendingApproval { get; set; }
    
    /// <summary>
    /// Whether the combo is verified/confirmed.
    /// </summary>
    public bool IsVerified { get; set; }
    
    /// <summary>
    /// Whether the combo is optimal (highest damage for its starter).
    /// </summary>
    public bool IsOptimal { get; set; }
    
    /// <summary>
    /// Whether this is a Touch of Death combo.
    /// </summary>
    public bool IsTouchOfDeath { get; set; }
    
    /// <summary>
    /// Tags for categorization.
    /// </summary>
    public List<string> Tags { get; set; } = new();
    
    /// <summary>
    /// Frame data information.
    /// </summary>
    public ComboFrameData FrameData { get; set; } = new();
    
    /// <summary>
    /// Timing information for execution.
    /// </summary>
    public ComboTiming Timing { get; set; } = new();
    
    /// <summary>
    /// Usage statistics.
    /// </summary>
    public ComboUsageStats UsageStats { get; set; } = new();
    
    /// <summary>
    /// Ratings from community.
    /// </summary>
    public ComboRatings Ratings { get; set; } = new();
    
    /// <summary>
    /// Related combos (variants, extensions).
    /// </summary>
    public List<Guid> RelatedComboIds { get; set; } = new();
    
    /// <summary>
    /// Prerequisites for executing this combo.
    /// </summary>
    public List<string> Prerequisites { get; set; } = new();
    
    /// <summary>
    /// Common mistakes and how to avoid them.
    /// </summary>
    public List<string> Tips { get; set; } = new();
    
    /// <summary>
    /// Position after combo (corner carry, screen position).
    /// </summary>
    public string EndingPosition { get; set; } = "Mid-Screen";
    
    /// <summary>
    /// Okizeme/setup options after combo.
    /// </summary>
    public List<string> OkizemeOptions { get; set; } = new();
    
    /// <summary>
    /// Whether the combo works on all characters.
    /// </summary>
    public bool Universal { get; set; } = true;
    
    /// <summary>
    /// Characters this combo doesn't work on (if not universal).
    /// </summary>
    public List<string> CharacterExceptions { get; set; } = new();
    
    /// <summary>
    /// Game version this combo was verified on.
    /// </summary>
    public string? GameVersion { get; set; }
}

/// <summary>
/// Individual move within a combo.
/// </summary>
public class ComboMoveEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    /// <summary>
    /// Move name.
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Input notation.
    /// </summary>
    public string Input { get; set; } = string.Empty;
    
    /// <summary>
    /// Position in the combo sequence.
    /// </summary>
    public int SequenceOrder { get; set; }
    
    /// <summary>
    /// Damage dealt by this move.
    /// </summary>
    public int Damage { get; set; }
    
    /// <summary>
    /// Whether this move is optional.
    /// </summary>
    public bool IsOptional { get; set; }
    
    /// <summary>
    /// Alternative moves that can be substituted.
    /// </summary>
    public List<string> Alternatives { get; set; } = new();
    
    /// <summary>
    /// Timing window for this move.
    /// </summary>
    public string Timing { get; set; } = string.Empty;
    
    /// <summary>
    /// Special properties (cancel, link, etc).
    /// </summary>
    public List<string> Properties { get; set; } = new();
    
    /// <summary>
    /// Whether this move requires meter.
    /// </summary>
    public bool UsesMeter { get; set; }
    
    /// <summary>
    /// How much meter this move uses.
    /// </summary>
    public int MeterCost { get; set; }
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
    Expert,
    TOD // Touch of Death
}

/// <summary>
/// Frame data information for a combo.
/// </summary>
public class ComboFrameData
{
    /// <summary>
    /// Total startup frames.
    /// </summary>
    public int StartupFrames { get; set; }
    
    /// <summary>
    /// Total active frames.
    /// </summary>
    public int ActiveFrames { get; set; }
    
    /// <summary>
    /// Total recovery frames.
    /// </summary>
    public int RecoveryFrames { get; set; }
    
    /// <summary>
    /// Frame advantage on block (if applicable).
    /// </summary>
    public int? BlockAdvantage { get; set; }
    
    /// <summary>
    /// Frame advantage on hit.
    /// </summary>
    public int HitAdvantage { get; set; }
    
    /// <summary>
    /// Invincibility frames.
    /// </summary>
    public int? InvincibilityFrames { get; set; }
    
    /// <summary>
    /// Whether the combo has armor.
    /// </summary>
    public bool HasArmor { get; set; }
}

/// <summary>
/// Timing information for executing a combo.
/// </summary>
public class ComboTiming
{
    /// <summary>
    /// Overall execution difficulty (1-10).
    /// </summary>
    public int ExecutionDifficulty { get; set; }
    
    /// <summary>
    /// Timing windows for each link/cancel (in frames).
    /// </summary>
    public List<TimingWindow> TimingWindows { get; set; } = new();
    
    /// <summary>
    /// Ideal input rhythm description.
    /// </summary>
    public string? RhythmDescription { get; set; }
    
    /// <summary>
    /// Slow-motion breakdown available.
    /// </summary>
    public bool HasSlowMotionGuide { get; set; }
    
    /// <summary>
    /// Path to slow-motion guide.
    /// </summary>
    public string? SlowMotionVideoPath { get; set; }
    
    /// <summary>
    /// Visual/audio cues for timing.
    /// </summary>
    public List<string> VisualCues { get; set; } = new();
}

/// <summary>
/// Timing window for a specific move/link.
/// </summary>
public class TimingWindow
{
    public int MoveIndex { get; set; }
    public string MoveName { get; set; } = string.Empty;
    public int FrameWindow { get; set; }
    public string Type { get; set; } = string.Empty; // "Link", "Cancel", "Chain"
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Usage statistics for a combo.
/// </summary>
public class ComboUsageStats
{
    /// <summary>
    /// Number of times combo has been viewed.
    /// </summary>
    public int ViewCount { get; set; }
    
    /// <summary>
    /// Number of times combo has been practiced.
    /// </summary>
    public int PracticeCount { get; set; }
    
    /// <summary>
    /// Number of times combo has been used in matches.
    /// </summary>
    public int MatchUsageCount { get; set; }
    
    /// <summary>
    /// Success rate when practiced.
    /// </summary>
    public decimal PracticeSuccessRate { get; set; }
    
    /// <summary>
    /// Average execution time in practice.
    /// </summary>
    public TimeSpan AverageExecutionTime { get; set; }
}

/// <summary>
/// Community ratings for a combo.
/// </summary>
public class ComboRatings
{
    /// <summary>
    /// Average rating (1-5).
    /// </summary>
    public decimal AverageRating { get; set; }
    
    /// <summary>
    /// Number of ratings.
    /// </summary>
    public int RatingCount { get; set; }
    
    /// <summary>
    /// Number of upvotes.
    /// </summary>
    public int Upvotes { get; set; }
    
    /// <summary>
    /// Number of downvotes.
    /// </summary>
    public int Downvotes { get; set; }
    
    /// <summary>
    /// Distribution of ratings (1-5 stars).
    /// </summary>
    public Dictionary<int, int> RatingDistribution { get; set; } = new();
}

/// <summary>
/// Character's combo database summary.
/// </summary>
public class CharacterComboDatabase
{
    public string CharacterName { get; set; } = string.Empty;
    public int TotalCombos { get; set; }
    public int EasyCombos { get; set; }
    public int MediumCombos { get; set; }
    public int HardCombos { get; set; }
    public int ExpertCombos { get; set; }
    public int OptimalCombos { get; set; }
    public int TouchOfDeathCombos { get; set; }
    public decimal AverageDamage { get; set; }
    public int MaxComboHits { get; set; }
    public int HighestDamage { get; set; }
    public List<ComboEntry> FeaturedCombos { get; set; } = new();
    public Dictionary<string, int> CombosByStarter { get; set; } = new();
    public Dictionary<string, int> CombosByPosition { get; set; } = new();
}

/// <summary>
/// Request to add a new combo.
/// </summary>
public class AddComboRequest
{
    public string CharacterName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ComboDifficulty Difficulty { get; set; } = ComboDifficulty.Medium;
    public int HitCount { get; set; }
    public int Damage { get; set; }
    public string StartingPosition { get; set; } = "Standing";
    public int MeterRequired { get; set; }
    public List<ComboMoveEntry> Moves { get; set; } = new();
    public string InputNotation { get; set; } = string.Empty;
    public string? VideoUrl { get; set; }
    public string? Creator { get; set; }
    public List<string> Tags { get; set; } = new();
    public bool IsTouchOfDeath { get; set; }
    public string? GameVersion { get; set; }
}

/// <summary>
/// Request to update an existing combo.
/// </summary>
public class UpdateComboRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public ComboDifficulty? Difficulty { get; set; }
    public int? HitCount { get; set; }
    public int? Damage { get; set; }
    public List<ComboMoveEntry>? Moves { get; set; }
    public string? InputNotation { get; set; }
    public string? VideoUrl { get; set; }
    public List<string>? Tags { get; set; }
    public bool? IsVerified { get; set; }
    public bool? IsOptimal { get; set; }
}

/// <summary>
/// Filter for searching combos.
/// </summary>
public class ComboFilter
{
    public string? CharacterName { get; set; }
    public ComboDifficulty? Difficulty { get; set; }
    public int? MinDamage { get; set; }
    public int? MaxDamage { get; set; }
    public int? MinHits { get; set; }
    public int? MaxHits { get; set; }
    public string? StartingPosition { get; set; }
    public int? MaxMeterRequired { get; set; }
    public List<string>? Tags { get; set; }
    public bool? IsVerified { get; set; }
    public bool? IsOptimal { get; set; }
    public bool? IsTouchOfDeath { get; set; }
    public string? SearchTerm { get; set; }
    public ComboSortOption SortBy { get; set; } = ComboSortOption.Damage;
    public bool SortDescending { get; set; } = true;
}

/// <summary>
/// Sort options for combo searches.
/// </summary>
public enum ComboSortOption
{
    Damage,
    HitCount,
    Difficulty,
    DateAdded,
    Rating,
    Usage,
    MeterEfficiency
}

/// <summary>
/// Combo discovery/matchup information.
/// </summary>
public class ComboMatchupInfo
{
    public string CharacterName { get; set; } = string.Empty;
    public string OpponentName { get; set; } = string.Empty;
    public List<ComboEntry> RecommendedCombos { get; set; } = new();
    public List<ComboEntry> OptimalCombos { get; set; } = new();
    public List<ComboEntry> MeterEfficientCombos { get; set; } = new();
    public string Analysis { get; set; } = string.Empty;
    public decimal CharacterAdvantage { get; set; }
}

/// <summary>
/// Practice session for a combo.
/// </summary>
public class ComboPracticeSession : EntityBase
{
    public Guid ComboId { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int Attempts { get; set; }
    public int Successes { get; set; }
    public decimal SuccessRate => Attempts > 0 ? (decimal)Successes / Attempts * 100 : 0;
    public TimeSpan TotalPracticeTime { get; set; }
    public List<PracticeAttempt> AttemptsLog { get; set; } = new();
    public bool IsCompleted { get; set; }
    public int ConsistencyRating { get; set; } // 1-10
}

/// <summary>
/// Individual practice attempt.
/// </summary>
public class PracticeAttempt
{
    public int AttemptNumber { get; set; }
    public bool Success { get; set; }
    public TimeSpan ExecutionTime { get; set; }
    public int Drops { get; set; }
    public string? Notes { get; set; }
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Community submission for a new combo.
/// </summary>
public class ComboSubmission : EntityBase
{
    public Guid ComboId { get; set; }
    public string SubmitterName { get; set; } = string.Empty;
    public string? SubmitterId { get; set; }
    public DateTime SubmittedAt { get; set; }
    public SubmissionStatus Status { get; set; }
    public string? ReviewerNotes { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewedBy { get; set; }
    public int Upvotes { get; set; }
    public int Downvotes { get; set; }
    public List<string> VerificationVideos { get; set; } = new();
}

/// <summary>
/// Submission status.
/// </summary>
public enum SubmissionStatus
{
    Pending,
    UnderReview,
    Approved,
    Rejected,
    NeedsMoreInfo
}

/// <summary>
/// Combo collection/folder.
/// </summary>
public class ComboCollection : EntityBase
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? CharacterName { get; set; }
    public List<Guid> ComboIds { get; set; } = new();
    public string Creator { get; set; } = string.Empty;
    public bool IsPublic { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int ViewCount { get; set; }
    public int LikeCount { get; set; }
}

/// <summary>
/// Damage optimization suggestion.
/// </summary>
public class DamageOptimizationSuggestion
{
    public Guid ComboId { get; set; }
    public string Suggestion { get; set; } = string.Empty;
    public int PotentialExtraDamage { get; set; }
    public string Method { get; set; } = string.Empty;
    public ComboDifficulty NewDifficulty { get; set; }
    public bool Verified { get; set; }
}
