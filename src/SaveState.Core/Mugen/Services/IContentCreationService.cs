using SaveState.Core.Common;
using SaveState.Core.Mugen.ValueObjects;

namespace SaveState.Core.Mugen.Services;

/// <summary>
/// Service for content creation and modding tools.
/// </summary>
public interface IContentCreationService
{
    /// <summary>
    /// Creates a new character from scratch or template.
    /// </summary>
    Task<Result<CharacterCreationResult>> CreateCharacterAsync(CharacterCreationRequest request, CancellationToken ct = default);

    /// <summary>
    /// Modifies an existing character's moveset.
    /// </summary>
    Task<Result> ModifyCharacterMovesetAsync(string characterName, MovesetModification modification, CancellationToken ct = default);

    /// <summary>
    /// Creates a custom stage.
    /// </summary>
    Task<Result<StageCreationResult>> CreateStageAsync(StageCreationRequest request, CancellationToken ct = default);

    /// <summary>
    /// Generates AI scripts for characters.
    /// </summary>
    Task<Result<AiScriptResult>> GenerateAiScriptAsync(string characterName, AiDifficulty difficulty, CancellationToken ct = default);

    /// <summary>
    /// Validates character or stage files for compatibility.
    /// </summary>
    Task<Result<ValidationResult>> ValidateContentAsync(ContentValidationRequest request, CancellationToken ct = default);

    /// <summary>
    /// Packages content for distribution.
    /// </summary>
    Task<Result<PackageResult>> PackageContentAsync(ContentPackageRequest request, CancellationToken ct = default);

    /// <summary>
    /// Merges multiple characters or mods.
    /// </summary>
    Task<Result<MergeResult>> MergeContentAsync(ContentMergeRequest request, CancellationToken ct = default);
}

/// <summary>
/// Request to create a new character.
/// </summary>
public record CharacterCreationRequest(
    string CharacterName,
    string BaseTemplate,
    CharacterCreationStats Stats,
    IReadOnlyList<ContentCreationMoveDefinition> Moves,
    IReadOnlyDictionary<string, string> Sprites,
    IReadOnlyDictionary<string, string> Sounds,
    CharacterCreationOptions Options);

/// <summary>
/// Character creation statistics.
/// </summary>
public record CharacterCreationStats(
    int Health,
    int Power,
    int Attack,
    int Defense,
    decimal Speed,
    decimal JumpHeight);

/// <summary>
/// Definition of a character move for content creation.
/// </summary>
public record ContentCreationMoveDefinition(
    string Name,
    string Input,
    MoveType Type,
    int Damage,
    string Animation,
    string? SoundEffect,
    MoveProperties Properties);

/// <summary>
/// Options for character creation.
/// </summary>
public record CharacterCreationOptions(
    bool IncludeAi,
    bool IncludeTrainingMode,
    bool GeneratePalettes,
    string Author,
    string Description);

/// <summary>
/// Result of character creation.
/// </summary>
public record CharacterCreationResult(
    string CharacterName,
    string FilePath,
    IReadOnlyList<string> GeneratedFiles,
    IReadOnlyList<string> Warnings,
    bool Success);

/// <summary>
/// Request to modify a character's moveset.
/// </summary>
public record MovesetModification(
    string CharacterName,
    IReadOnlyList<ContentCreationMoveDefinition> NewMoves,
    IReadOnlyList<string> ModifiedMoves,
    IReadOnlyList<string> RemovedMoves,
    bool BackupOriginal);

/// <summary>
/// Request to create a custom stage.
/// </summary>
public record StageCreationRequest(
    string StageName,
    string BackgroundImage,
    IReadOnlyList<StageElement> Elements,
    StageBounds Bounds,
    IReadOnlyDictionary<string, string> Music,
    StageCreationOptions Options);

/// <summary>
/// Element in a stage (platforms, decorations, etc.).
/// </summary>
public record StageElement(
    string Type,
    Position Position,
    Size Size,
    string Sprite,
    ElementProperties Properties);

/// <summary>
/// Properties of a stage element.
/// </summary>
public record ElementProperties(
    bool Collidable,
    int ZIndex,
    AnimationProperties? Animation);

/// <summary>
/// Animation properties for stage elements.
/// </summary>
public record AnimationProperties(
    int FrameCount,
    int FrameDuration,
    bool Loop);

/// <summary>
/// Stage boundaries.
/// </summary>
public record StageBounds(
    int Left,
    int Right,
    int Top,
    int Bottom,
    int CameraLeft,
    int CameraRight);

/// <summary>
/// Options for stage creation.
/// </summary>
public record StageCreationOptions(
    string Author,
    string Description,
    bool IncludeLighting,
    bool IncludeParticles);

/// <summary>
/// Result of stage creation.
/// </summary>
public record StageCreationResult(
    string StageName,
    string FilePath,
    IReadOnlyList<string> GeneratedFiles,
    IReadOnlyList<string> Warnings,
    bool Success);

/// <summary>
/// Difficulty levels for AI.
/// </summary>
public enum AiDifficulty
{
    VeryEasy,
    Easy,
    Medium,
    Hard,
    VeryHard,
    Expert
}

/// <summary>
/// Result of AI script generation.
/// </summary>
public record AiScriptResult(
    string CharacterName,
    string ScriptContent,
    AiDifficulty Difficulty,
    IReadOnlyList<string> Behaviors,
    bool Success);

/// <summary>
/// Request to validate content.
/// </summary>
public record ContentValidationRequest(
    string ContentType,
    string FilePath,
    IReadOnlyList<string> ValidationRules);

/// <summary>
/// Request to package content.
/// </summary>
public record ContentPackageRequest(
    string PackageName,
    string Version,
    IReadOnlyList<string> ContentFiles,
    PackageMetadata Metadata,
    string OutputPath);

/// <summary>
/// Package metadata.
/// </summary>
public record PackageMetadata(
    string Author,
    string Description,
    IReadOnlyList<string> Tags,
    string Website,
    IReadOnlyDictionary<string, string> Dependencies);

/// <summary>
/// Result of content packaging.
/// </summary>
public record PackageResult(
    string PackagePath,
    long PackageSize,
    IReadOnlyList<string> IncludedFiles,
    string Checksum,
    bool Success);

/// <summary>
/// Request to merge content.
/// </summary>
public record ContentMergeRequest(
    string MergeType,
    IReadOnlyList<string> SourceFiles,
    string OutputPath,
    MergeOptions Options);

/// <summary>
/// Options for content merging.
/// </summary>
public record MergeOptions(
    bool ResolveConflicts,
    string ConflictResolutionStrategy,
    bool BackupSources,
    IReadOnlyList<string> PriorityOrder);

/// <summary>
/// Result of content merging.
/// </summary>
public record MergeResult(
    string OutputPath,
    IReadOnlyList<string> MergedFiles,
    IReadOnlyList<MergeConflict> Conflicts,
    IReadOnlyList<string> Warnings,
    bool Success);

/// <summary>
/// Merge conflict.
/// </summary>
public record MergeConflict(
    string File,
    string ConflictType,
    IReadOnlyList<string> ConflictingSources,
    string Resolution);
