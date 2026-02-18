using SaveState.Core.Common.Base;

namespace SaveState.Core.Mugen.DeathBattle;

/// <summary>
/// Represents a Death Battle simulation between two characters.
/// YouTube Death Battle style with research, analysis, and dramatic outcomes.
/// </summary>
public class DeathBattleMatch : EntityBase
{
    /// <summary>
    /// Unique battle identifier.
    /// </summary>
    public string BattleCode { get; set; } = string.Empty;
    
    /// <summary>
    /// First combatant.
    /// </summary>
    public DeathBattleCombatant Combatant1 { get; set; } = null!;
    
    /// <summary>
    /// Second combatant.
    /// </summary>
    public DeathBattleCombatant Combatant2 { get; set; } = null!;
    
    /// <summary>
    /// Current state of the battle.
    /// </summary>
    public DeathBattleState State { get; set; } = DeathBattleState.Preparation;
    
    /// <summary>
    /// Winner of the battle (null until decided).
    /// </summary>
    public DeathBattleWinner? Winner { get; set; }
    
    /// <summary>
    /// How the battle ended.
    /// </summary>
    public DeathBattleOutcome Outcome { get; set; }
    
    /// <summary>
    /// Research and analysis for both combatants.
    /// </summary>
    public DeathBattleResearch Research { get; set; } = new();
    
    /// <summary>
    /// Battle phases (intro, analysis, fight, verdict).
    /// </summary>
    public List<DeathBattlePhase> Phases { get; set; } = new();
    
    /// <summary>
    /// Current phase index.
    /// </summary>
    public int CurrentPhaseIndex { get; set; }
    
    /// <summary>
    /// Battle simulation results.
    /// </summary>
    public DeathBattleSimulation Simulation { get; set; } = new();
    
    /// <summary>
    /// Viewer statistics.
    /// </summary>
    public DeathBattleStats Stats { get; set; } = new();
    
    /// <summary>
    /// When the battle was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// When the battle concluded.
    /// </summary>
    public DateTime? CompletedAt { get; set; }
    
    /// <summary>
    /// Is this battle publicly viewable.
    /// </summary>
    public bool IsPublic { get; set; }
    
    /// <summary>
    /// Tags for categorization.
    /// </summary>
    public List<string> Tags { get; set; } = new();
    
    /// <summary>
    /// Suggested by user (community suggestions).
    /// </summary>
    public Guid? SuggestedByUserId { get; set; }
    
    /// <summary>
    /// Total runtime of the battle.
    /// </summary>
    public TimeSpan Duration { get; set; }
}

/// <summary>
/// Represents a combatant in a Death Battle.
/// </summary>
public class DeathBattleCombatant
{
    public Guid CharacterId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty; // Game/Anime/Comic origin
    public string? AvatarUrl { get; set; }
    public string? Description { get; set; }
    public DeathBattleStatsProfile Stats { get; set; } = new();
    public List<DeathBattleFeat> Feats { get; set; } = new();
    public List<DeathBattleAbility> Abilities { get; set; } = new();
    public List<string> Strengths { get; set; } = new();
    public List<string> Weaknesses { get; set; } = new();
    public int PreMatchVotes { get; set; }
}

/// <summary>
/// Combatant statistics profile.
/// </summary>
public class DeathBattleStatsProfile
{
    public int Strength { get; set; } // 1-100
    public int Speed { get; set; }
    public int Durability { get; set; }
    public int Intelligence { get; set; }
    public int CombatSkill { get; set; }
    public int Power { get; set; }
    public int Experience { get; set; }
    public int Hax { get; set; } // Abilities that break conventional rules
    
    /// <summary>
    /// Overall power level.
    /// </summary>
    public int Overall => (Strength + Speed + Durability + Intelligence + CombatSkill + Power + Experience + Hax) / 8;
    
    /// <summary>
    /// Power tier classification.
    /// </summary>
    public string Tier => Overall switch
    {
        >= 95 => "Tier: Cosmic / Universe+",
        >= 85 => "Tier: Planetary",
        >= 75 => "Tier: Mountain - Continental",
        >= 65 => "Tier: City - Mountain",
        >= 55 => "Tier: Building - City Block",
        >= 45 => "Tier: Street - Wall",
        _ => "Tier: Below Street"
    };
}

/// <summary>
/// Notable feat/achievement of a combatant.
/// </summary>
public class DeathBattleFeat
{
    public string Description { get; set; } = string.Empty;
    public FeatType Type { get; set; }
    public string? Source { get; set; }
    public int ImpressiveScore { get; set; } // 1-10
}

/// <summary>
/// Types of feats.
/// </summary>
public enum FeatType
{
    Strength,
    Speed,
    Durability,
    Destruction,
    Intelligence,
    Skill,
    Hax
}

/// <summary>
/// Special ability of a combatant.
/// </summary>
public class DeathBattleAbility
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public AbilityType Type { get; set; }
    public int PowerLevel { get; set; }
    public List<string> Counters { get; set; } = new(); // Abilities that counter this
}

/// <summary>
/// Types of abilities.
/// </summary>
public enum AbilityType
{
    Physical,
    Energy,
    Magic,
    Technology,
    Mental,
    Time,
    Spatial,
    RealityWarping
}

/// <summary>
/// Research and analysis for the battle.
/// </summary>
public class DeathBattleResearch
{
    public CombatantAnalysis Combatant1Analysis { get; set; } = new();
    public CombatantAnalysis Combatant2Analysis { get; set; } = new();
    public List<DeathBattleComparison> Comparisons { get; set; } = new();
    public string ResearcherNotes { get; set; } = string.Empty;
    public List<string> KeyFactors { get; set; } = new();
    public List<string> PotentialOutcomes { get; set; } = new();
}

/// <summary>
/// Detailed analysis of a combatant.
/// </summary>
public class CombatantAnalysis
{
    public Guid CombatantId { get; set; }
    public string CombatantName { get; set; } = string.Empty;
    public string Overview { get; set; } = string.Empty;
    public string Background { get; set; } = string.Empty;
    public List<string> KeyFeatsExplained { get; set; } = new();
    public List<string> Arsenal { get; set; } = new();
    public List<string> NotableWeaknesses { get; set; } = new();
    public int WinProbability { get; set; } // Calculated percentage
}

/// <summary>
/// Comparison between combatants in a specific category.
/// </summary>
public class DeathBattleComparison
{
    public string Category { get; set; } = string.Empty;
    public int Combatant1Score { get; set; }
    public int Combatant2Score { get; set; }
    public string Analysis { get; set; } = string.Empty;
    public string Advantage { get; set; } = string.Empty; // "Combatant1", "Combatant2", or "Even"
}

/// <summary>
/// A phase of the Death Battle.
/// </summary>
public class DeathBattlePhase
{
    public DeathBattlePhaseType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
    public bool IsComplete { get; set; }
}

/// <summary>
/// Types of battle phases.
/// </summary>
public enum DeathBattlePhaseType
{
    Introduction,
    Combatant1Analysis,
    Combatant2Analysis,
    Comparison,
    FightSimulation,
    Verdict,
    NextTime
}

/// <summary>
/// Battle simulation results.
/// </summary>
public class DeathBattleSimulation
{
    public int TotalSimulationsRun { get; set; }
    public int Combatant1Wins { get; set; }
    public int Combatant2Wins { get; set; }
    public int Draws { get; set; }
    public double Combatant1WinRate => TotalSimulationsRun > 0 ? (double)Combatant1Wins / TotalSimulationsRun * 100 : 0;
    public double Combatant2WinRate => TotalSimulationsRun > 0 ? (double)Combatant2Wins / TotalSimulationsRun * 100 : 0;
    public List<SimulatedRound> KeyMoments { get; set; } = new();
    public string MostLikelyScenario { get; set; } = string.Empty;
}

/// <summary>
/// A key moment from simulation.
/// </summary>
public class SimulatedRound
{
    public int RoundNumber { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? FinishingMove { get; set; }
    public int Combatant1Health { get; set; }
    public int Combatant2Health { get; set; }
}

/// <summary>
/// Winner of the Death Battle.
/// </summary>
public class DeathBattleWinner
{
    public Guid CombatantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string VictoryQuote { get; set; } = string.Empty;
    public string? FinishingMove { get; set; }
    public string Reasoning { get; set; } = string.Empty;
}

/// <summary>
/// How the battle ended.
/// </summary>
public enum DeathBattleOutcome
{
    KO,           // Knockout
    Death,        // Fatality
    Incapacitation, // Unable to continue
    BFR,          // Battlefield Removal
    Surrender,    // Gave up
    Draw,         // Both eliminated
    Interrupted   // Battle stopped
}

/// <summary>
/// Current state of the Death Battle.
/// </summary>
public enum DeathBattleState
{
    Preparation,
    Researching,
    Simulating,
    InProgress,
    Concluded,
    Cancelled
}

/// <summary>
/// Battle statistics.
/// </summary>
public class DeathBattleStats
{
    public int ViewCount { get; set; }
    public int Likes { get; set; }
    public int Comments { get; set; }
    public int Shares { get; set; }
    public int Combatant1Votes { get; set; }
    public int Combatant2Votes { get; set; }
    public List<string> TopComments { get; set; } = new();
}

/// <summary>
/// Request to create a new Death Battle.
/// </summary>
public class CreateDeathBattleRequest
{
    public Guid Combatant1Id { get; set; }
    public Guid Combatant2Id { get; set; }
    public string? CustomBattleCode { get; set; }
    public bool IsPublic { get; set; } = true;
    public List<string>? Tags { get; set; }
}

/// <summary>
/// Suggestion for a future Death Battle.
/// </summary>
public class DeathBattleSuggestion : EntityBase
{
    public Guid SuggestedCombatant1Id { get; set; }
    public string SuggestedCombatant1Name { get; set; } = string.Empty;
    public Guid SuggestedCombatant2Id { get; set; }
    public string SuggestedCombatant2Name { get; set; } = string.Empty;
    public string Reasoning { get; set; } = string.Empty;
    public Guid SuggestedByUserId { get; set; }
    public int Upvotes { get; set; }
    public bool IsAccepted { get; set; }
    public DateTime SuggestedAt { get; set; }
}

/// <summary>
/// Season/series of Death Battles.
/// </summary>
public class DeathBattleSeason : EntityBase
{
    public string Name { get; set; } = string.Empty;
    public int SeasonNumber { get; set; }
    public string Theme { get; set; } = string.Empty;
    public List<Guid> BattleIds { get; set; } = new();
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>
/// Leaderboard entry for Death Battle stats.
/// </summary>
public class DeathBattleLeaderboardEntry
{
    public Guid CharacterId { get; set; }
    public string CharacterName { get; set; } = string.Empty;
    public int BattlesFought { get; set; }
    public int Wins { get; set; }
    public int Losses { get; set; }
    public int Draws { get; set; }
    public decimal WinRate => BattlesFought > 0 ? (decimal)Wins / BattlesFought * 100 : 0;
    public int Rank { get; set; }
    public string Tier { get; set; } = string.Empty;
}
