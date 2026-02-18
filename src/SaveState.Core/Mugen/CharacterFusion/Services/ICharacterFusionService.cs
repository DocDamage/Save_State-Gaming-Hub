using SaveState.Core.Common;

namespace SaveState.Core.Mugen.CharacterFusion.Services;

/// <summary>
/// Service for fusing MUGEN characters DBZ/Vegito style.
/// </summary>
public interface ICharacterFusionService
{
    /// <summary>
    /// Analyzes potential fusion between two characters without creating it.
    /// </summary>
    Task<Result<FusionAnalysis>> AnalyzeFusionPotentialAsync(
        Guid parent1Id,
        Guid parent2Id,
        CancellationToken ct = default);
    
    /// <summary>
    /// Creates a fused character from two parents.
    /// </summary>
    Task<Result<FusedCharacter>> FuseCharactersAsync(
        FusionRequest request,
        CancellationToken ct = default);
    
    /// <summary>
    /// Generates the playable MUGEN character files.
    /// </summary>
    Task<Result<string>> GenerateMugenCharacterAsync(
        Guid fusionId,
        string outputDirectory,
        CancellationToken ct = default);
    
    /// <summary>
    /// Gets a fused character by ID.
    /// </summary>
    Task<Result<FusedCharacter>> GetFusionAsync(
        Guid fusionId,
        CancellationToken ct = default);
    
    /// <summary>
    /// Gets all fusions for a user.
    /// </summary>
    Task<Result<List<FusedCharacter>>> GetUserFusionsAsync(
        Guid userId,
        CancellationToken ct = default);
    
    /// <summary>
    /// Gets fusions involving a specific character.
    /// </summary>
    Task<Result<List<FusedCharacter>>> GetFusionsByCharacterAsync(
        Guid characterId,
        CancellationToken ct = default);
    
    /// <summary>
    /// Deletes a fused character.
    /// </summary>
    Task<Result> DeleteFusionAsync(
        Guid fusionId,
        CancellationToken ct = default);
    
    /// <summary>
    /// Records a battle result for a fused character.
    /// </summary>
    Task<Result> RecordBattleResultAsync(
        Guid fusionId,
        FusionBattleHistory battle,
        CancellationToken ct = default);
    
    /// <summary>
    /// Gets battle history for a fused character.
    /// </summary>
    Task<Result<List<FusionBattleHistory>>> GetBattleHistoryAsync(
        Guid fusionId,
        CancellationToken ct = default);
    
    /// <summary>
    /// Gets the global fusion leaderboard.
    /// </summary>
    Task<Result<List<FusionLeaderboardEntry>>> GetLeaderboardAsync(
        int top = 100,
        CancellationToken ct = default);
    
    /// <summary>
    /// Gets available preset fusions.
    /// </summary>
    Task<Result<List<PresetFusion>>> GetPresetFusionsAsync(
        bool unlockedOnly = true,
        CancellationToken ct = default);
    
    /// <summary>
    /// Unlocks a preset fusion.
    /// </summary>
    Task<Result> UnlockPresetFusionAsync(
        Guid presetId,
        Guid userId,
        CancellationToken ct = default);
    
    /// <summary>
    /// Suggests optimal fusion combinations for a character.
    /// </summary>
    Task<Result<List<FusionSuggestion>>> GetFusionSuggestionsAsync(
        Guid characterId,
        int count = 5,
        CancellationToken ct = default);
    
    /// <summary>
    /// Compares multiple fused characters.
    /// </summary>
    Task<Result<FusionComparison>> CompareFusionsAsync(
        Guid fusionId1,
        Guid fusionId2,
        CancellationToken ct = default);
    
    /// <summary>
    /// Exports a fused character for sharing.
    /// </summary>
    Task<Result<byte[]>> ExportFusionAsync(
        Guid fusionId,
        CancellationToken ct = default);
    
    /// <summary>
    /// Imports a fused character from shared data.
    /// </summary>
    Task<Result<FusedCharacter>> ImportFusionAsync(
        byte[] data,
        Guid userId,
        CancellationToken ct = default);
    
    /// <summary>
    /// Validates if two characters can be fused.
    /// </summary>
    Task<Result<bool>> CanFuseAsync(
        Guid parent1Id,
        Guid parent2Id,
        CancellationToken ct = default);
}

/// <summary>
/// Suggested fusion combination.
/// </summary>
public class FusionSuggestion
{
    public Guid CharacterId { get; set; }
    public string CharacterName { get; set; } = string.Empty;
    public int CompatibilityScore { get; set; }
    public FusionCompatibility Compatibility { get; set; }
    public string SuggestedFusionName { get; set; } = string.Empty;
    public FusionStats PredictedStats { get; set; } = new();
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// Comparison between two fused characters.
/// </summary>
public class FusionComparison
{
    public FusedCharacter Fusion1 { get; set; } = null!;
    public FusedCharacter Fusion2 { get; set; } = null!;
    public int Winner { get; set; } // 1, 2, or 0 for tie
    public Dictionary<string, StatComparison> StatComparisons { get; set; } = new();
    public List<string> Fusion1Advantages { get; set; } = new();
    public List<string> Fusion2Advantages { get; set; } = new();
    public string PredictedOutcome { get; set; } = string.Empty;
}

/// <summary>
/// Single stat comparison.
/// </summary>
public class StatComparison
{
    public string StatName { get; set; } = string.Empty;
    public int Fusion1Value { get; set; }
    public int Fusion2Value { get; set; }
    public int Difference => Fusion1Value - Fusion2Value;
    public string Winner => Difference > 0 ? "Fusion1" : Difference < 0 ? "Fusion2" : "Tie";
}
