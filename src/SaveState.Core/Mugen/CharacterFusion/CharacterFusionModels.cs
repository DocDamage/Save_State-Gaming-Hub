using SaveState.Core.Common.Base;

namespace SaveState.Core.Mugen.CharacterFusion;

/// <summary>
/// Represents a fused character created from two parent characters.
/// Vegito-style: Complete fusion creating a new unique character.
/// </summary>
public class FusedCharacter : EntityBase
{
    /// <summary>
    /// Name of the fused character (e.g., "Goku + Vegeta = Vegito").
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Display name shown in-game.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;
    
    /// <summary>
    /// First parent character ID.
    /// </summary>
    public Guid Parent1Id { get; set; }
    
    /// <summary>
    /// First parent character name.
    /// </summary>
    public string Parent1Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Second parent character ID.
    /// </summary>
    public Guid Parent2Id { get; set; }
    
    /// <summary>
    /// Second parent character name.
    /// </summary>
    public string Parent2Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Fusion type determines how stats are combined.
    /// </summary>
    public FusionType FusionType { get; set; } = FusionType.Potara;
    
    /// <summary>
    /// Fused character statistics.
    /// </summary>
    public FusionStats Stats { get; set; } = new();
    
    /// <summary>
    /// Moves inherited from both parents.
    /// </summary>
    public List<FusedMove> Moves { get; set; } = new();
    
    /// <summary>
    /// Visual appearance settings.
    /// </summary>
    public FusionAppearance Appearance { get; set; } = new();
    
    /// <summary>
    /// Generated .def file content for MUGEN.
    /// </summary>
    public string? MugenDefContent { get; set; }
    
    /// <summary>
    /// Path to the fused character folder.
    /// </summary>
    public string? CharacterFolderPath { get; set; }
    
    /// <summary>
    /// Whether this fusion has been generated as a playable character.
    /// </summary>
    public bool IsGenerated { get; set; }
    
    /// <summary>
    /// When the fusion was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// Number of battles this fusion has participated in.
    /// </summary>
    public int BattleCount { get; set; }
    
    /// <summary>
    /// Win rate percentage.
    /// </summary>
    public decimal WinRate { get; set; }
    
    /// <summary>
    /// User who created this fusion.
    /// </summary>
    public Guid CreatedBy { get; set; }
    
    /// <summary>
    /// Is this a preset fusion (official) or user-created.
    /// </summary>
    public bool IsPreset { get; set; }
    
    /// <summary>
    /// Tags for categorization.
    /// </summary>
    public List<string> Tags { get; set; } = new();
    
    /// <summary>
    /// Compatibility score between parents (0-100).
    /// </summary>
    public int CompatibilityScore { get; set; }
}

/// <summary>
/// Types of fusion with different mechanics.
/// </summary>
public enum FusionType
{
    /// <summary>
    /// Potara fusion - multiplies power levels (DBZ style).
    /// Stats = (Parent1 + Parent2) × 1.5
    /// </summary>
    Potara,
    
    /// <summary>
    /// Fusion Dance - adds power levels evenly.
    /// Stats = (Parent1 + Parent2) × 1.2
    /// </summary>
    FusionDance,
    
    /// <summary>
    /// DNA Fusion - averages stats with hybrid bonuses.
    /// Stats = Average + 10% boost
    /// </summary>
    DNAFusion,
    
    /// <summary>
    /// Custom fusion - user-defined stat combination.
    /// </summary>
    Custom
}

/// <summary>
/// Statistics for a fused character.
/// </summary>
public class FusionStats
{
    public int Health { get; set; }
    public int Attack { get; set; }
    public int Defense { get; set; }
    public int Speed { get; set; }
    public int Power { get; set; }
    public int Special { get; set; }
    public int Combo { get; set; }
    
    /// <summary>
    /// Overall power level calculation.
    /// </summary>
    public int PowerLevel => (Health + Attack + Defense + Speed + Power + Special + Combo) / 7;
    
    /// <summary>
    /// Tier rating based on power level.
    /// </summary>
    public FusionTier Tier => PowerLevel switch
    {
        >= 90 => FusionTier.SSPlus,
        >= 80 => FusionTier.SS,
        >= 70 => FusionTier.S,
        >= 60 => FusionTier.A,
        >= 50 => FusionTier.B,
        >= 40 => FusionTier.C,
        _ => FusionTier.D
    };
}

/// <summary>
/// Tier rankings for fused characters.
/// </summary>
public enum FusionTier
{
    D, C, B, A, S, SS, SSPlus, God
}

/// <summary>
/// A move inherited from a parent in fusion.
/// </summary>
public class FusedMove
{
    public string Name { get; set; } = string.Empty;
    public string Command { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Damage { get; set; }
    public int StartupFrames { get; set; }
    public int ActiveFrames { get; set; }
    public int RecoveryFrames { get; set; }
    public int MeterCost { get; set; }
    public MoveSource Source { get; set; }
    public string? ParentName { get; set; }
    public bool IsEnhanced { get; set; }
    public string? EnhancementDescription { get; set; }
}

/// <summary>
/// Source of the fused move.
/// </summary>
public enum MoveSource
{
    Parent1,
    Parent2,
    Combined,
    NewFusionMove
}

/// <summary>
/// Visual appearance settings for fusion.
/// </summary>
public class FusionAppearance
{
    /// <summary>
    /// Primary color scheme (hex).
    /// </summary>
    public string PrimaryColor { get; set; } = "#FFFFFF";
    
    /// <summary>
    /// Secondary color scheme.
    /// </summary>
    public string SecondaryColor { get; set; } = "#000000";
    
    /// <summary>
    /// Aura color for special effects.
    /// </summary>
    public string AuraColor { get; set; } = "#FFD700";
    
    /// <summary>
    /// Portion of appearance from parent 1 (0-100).
    /// </summary>
    public int Parent1VisualDominance { get; set; } = 50;
    
    /// <summary>
    /// Visual traits inherited from parent 1.
    /// </summary>
    public List<string> Parent1Traits { get; set; } = new();
    
    /// <summary>
    /// Visual traits inherited from parent 2.
    /// </summary>
    public List<string> Parent2Traits { get; set; } = new();
    
    /// <summary>
    /// Unique fusion-only visual traits.
    /// </summary>
    public List<string> UniqueTraits { get; set; } = new();
    
    /// <summary>
    /// Path to generated sprite sheet.
    /// </summary>
    public string? SpriteSheetPath { get; set; }
    
    /// <summary>
    /// Path to portrait image.
    /// </summary>
    public string? PortraitPath { get; set; }
}

/// <summary>
/// Request to fuse two characters.
/// </summary>
public class FusionRequest
{
    public Guid Parent1Id { get; set; }
    public Guid Parent2Id { get; set; }
    public string? CustomName { get; set; }
    public FusionType FusionType { get; set; } = FusionType.Potara;
    public FusionCustomizationOptions? Customization { get; set; }
}

/// <summary>
/// Customization options for fusion.
/// </summary>
public class FusionCustomizationOptions
{
    public string? PreferredName { get; set; }
    public int? Parent1StatPercentage { get; set; }
    public int? Parent2StatPercentage { get; set; }
    public List<string>? PreferredMovesFromParent1 { get; set; }
    public List<string>? PreferredMovesFromParent2 { get; set; }
    public string? PrimaryColor { get; set; }
    public string? SecondaryColor { get; set; }
}

/// <summary>
/// Result of analyzing potential fusion.
/// </summary>
public class FusionAnalysis
{
    public Guid Parent1Id { get; set; }
    public Guid Parent2Id { get; set; }
    public string Parent1Name { get; set; } = string.Empty;
    public string Parent2Name { get; set; } = string.Empty;
    public int CompatibilityScore { get; set; }
    public FusionCompatibility Compatibility { get; set; }
    public FusionStats PredictedStats { get; set; } = new();
    public string SuggestedFusionName { get; set; } = string.Empty;
    public List<string> PredictedMoves { get; set; } = new();
    public List<string> Synergies { get; set; } = new();
    public List<string> Conflicts { get; set; } = new();
}

/// <summary>
/// Compatibility level between two characters for fusion.
/// </summary>
public enum FusionCompatibility
{
    Incompatible,
    Poor,
    Fair,
    Good,
    Excellent,
    Perfect
}

/// <summary>
/// History of fusion battles.
/// </summary>
public class FusionBattleHistory : EntityBase
{
    public Guid FusedCharacterId { get; set; }
    public string FusedCharacterName { get; set; } = string.Empty;
    public Guid OpponentId { get; set; }
    public string OpponentName { get; set; } = string.Empty;
    public bool Won { get; set; }
    public int RoundsWon { get; set; }
    public int RoundsLost { get; set; }
    public DateTime BattleDate { get; set; }
    public int DamageDealt { get; set; }
    public int DamageReceived { get; set; }
    public int MaxCombo { get; set; }
    public List<string> SpecialMovesUsed { get; set; } = new();
}

/// <summary>
/// Preset fusion definitions (official fusions like Vegito, Gogeta, etc.).
/// </summary>
public class PresetFusion : EntityBase
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Parent1Name { get; set; } = string.Empty;
    public string Parent2Name { get; set; } = string.Empty;
    public FusionType FusionType { get; set; }
    public FusionStats BaseStats { get; set; } = new();
    public List<string> SignatureMoves { get; set; } = new();
    public string Description { get; set; } = string.Empty;
    public string? Lore { get; set; }
    public bool IsUnlocked { get; set; }
    public int UnlockRequirement { get; set; } // Win count or other requirement
}

/// <summary>
/// Leaderboard entry for fused characters.
/// </summary>
public class FusionLeaderboardEntry
{
    public Guid FusedCharacterId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ParentNames { get; set; } = string.Empty;
    public int PowerLevel { get; set; }
    public FusionTier Tier { get; set; }
    public int TotalBattles { get; set; }
    public int Wins { get; set; }
    public decimal WinRate { get; set; }
    public int Rank { get; set; }
}
