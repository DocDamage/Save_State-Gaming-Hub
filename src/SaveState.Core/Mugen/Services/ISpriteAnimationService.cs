using SaveState.Core.Common;
using SaveState.Core.Mugen.ValueObjects;

namespace SaveState.Core.Mugen.Services;

/// <summary>
/// Marker interface for sprite and animation services.
/// Use specific sub-interfaces for actual operations.
/// </summary>
public interface ISpriteAnimationService
{
}

/// <summary>
/// Service for sprite file (SFF) management.
/// </summary>
public interface ISpriteManagementService
{
    /// <summary>
    /// Loads sprites from an SFF file.
    /// </summary>
    Task<Result<SffFile>> LoadSffFileAsync(string filePath, CancellationToken ct = default);

    /// <summary>
    /// Saves sprites to an SFF file.
    /// </summary>
    Task<Result> SaveSffFileAsync(string filePath, SffFile sffFile, CancellationToken ct = default);

    /// <summary>
    /// Imports sprites from image files.
    /// </summary>
    Task<Result<IReadOnlyList<Sprite>>> ImportSpritesAsync(
        IReadOnlyList<string> imagePaths,
        ImportSpriteOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Exports sprites to image files.
    /// </summary>
    Task<Result<IReadOnlyList<string>>> ExportSpritesAsync(
        IReadOnlyList<Sprite> sprites,
            ExportSpriteOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Adds a sprite to an SFF file.
    /// </summary>
    Task<Result<Sprite>> AddSpriteAsync(
        Guid groupId,
        int imageNumber,
        byte[] imageData,
        SpriteMetadata metadata,
        CancellationToken ct = default);

    /// <summary>
    /// Removes a sprite from an SFF file.
    /// </summary>
    Task<Result> RemoveSpriteAsync(
        int groupNumber,
        int imageNumber,
        CancellationToken ct = default);

    /// <summary>
    /// Gets sprites by group number.
    /// </summary>
    Task<Result<IReadOnlyList<Sprite>>> GetSpritesByGroupAsync(
        int groupNumber,
        CancellationToken ct = default);

    /// <summary>
    /// Gets a specific sprite.
    /// </summary>
    Task<Result<Sprite>> GetSpriteAsync(
        int groupNumber,
        int imageNumber,
        CancellationToken ct = default);

    /// <summary>
    /// Optimizes sprite file size.
    /// </summary>
    Task<Result<SffOptimizationResult>> OptimizeSffAsync(
        string filePath,
        SpriteOptimizationOptions options,
        CancellationToken ct = default);
}

/// <summary>
/// Service for animation (AIR) management.
/// </summary>
public interface IAnimationManagementService
{
    /// <summary>
    /// Loads animations from an AIR file.
    /// </summary>
    Task<Result<AirFile>> LoadAirFileAsync(string filePath, CancellationToken ct = default);

    /// <summary>
    /// Saves animations to an AIR file.
    /// </summary>
    Task<Result> SaveAirFileAsync(string filePath, AirFile airFile, CancellationToken ct = default);

    /// <summary>
    /// Creates a new animation.
    /// </summary>
    Task<Result<Animation>> CreateAnimationAsync(
        int actionNumber,
        string name,
        SpriteAnimationType type,
        CancellationToken ct = default);

    /// <summary>
    /// Adds a frame to an animation.
    /// </summary>
    Task<Result> AddAnimationFrameAsync(
        int actionNumber,
        AnimationFrame frame,
        int? insertIndex = null,
        CancellationToken ct = default);

    /// <summary>
    /// Removes a frame from an animation.
    /// </summary>
    Task<Result> RemoveAnimationFrameAsync(
        int actionNumber,
        int frameIndex,
        CancellationToken ct = default);

    /// <summary>
    /// Updates an animation frame.
    /// </summary>
    Task<Result> UpdateAnimationFrameAsync(
        int actionNumber,
        int frameIndex,
        AnimationFrame frame,
        CancellationToken ct = default);

    /// <summary>
    /// Gets all animations.
    /// </summary>
    Task<Result<IReadOnlyList<Animation>>> GetAnimationsAsync(
        SpriteAnimationType? typeFilter = null,
        CancellationToken ct = default);

    /// <summary>
    /// Gets a specific animation by action number.
    /// </summary>
    Task<Result<Animation>> GetAnimationAsync(
        int actionNumber,
        CancellationToken ct = default);

    /// <summary>
    /// Duplicates an animation.
    /// </summary>
    Task<Result<Animation>> DuplicateAnimationAsync(
        int sourceActionNumber,
        int newActionNumber,
        CancellationToken ct = default);

    /// <summary>
    /// Deletes an animation.
    /// </summary>
    Task<Result> DeleteAnimationAsync(
        int actionNumber,
        CancellationToken ct = default);
}

/// <summary>
/// Service for palette management.
/// </summary>
public interface IPaletteManagementService
{
    /// <summary>
    /// Loads palette from a file (ACT, PAL, etc.).
    /// </summary>
    Task<Result<Palette>> LoadPaletteAsync(string filePath, CancellationToken ct = default);

    /// <summary>
    /// Saves palette to a file.
    /// </summary>
    Task<Result> SavePaletteAsync(string filePath, Palette palette, CancellationToken ct = default);

    /// <summary>
    /// Creates a new palette.
    /// </summary>
    Task<Result<Palette>> CreatePaletteAsync(
        string name,
        IReadOnlyList<RgbColor> colors,
        CancellationToken ct = default);

    /// <summary>
    /// Applies palette to sprites.
    /// </summary>
    Task<Result<IReadOnlyList<Sprite>>> ApplyPaletteAsync(
        IReadOnlyList<Sprite> sprites,
        Palette palette,
        CancellationToken ct = default);

    /// <summary>
    /// Generates palette variations.
    /// </summary>
    Task<Result<IReadOnlyList<Palette>>> GeneratePaletteVariationsAsync(
        Palette basePalette,
        int variationCount,
        VariationType type,
        CancellationToken ct = default);

    /// <summary>
    /// Gets palette colors.
    /// </summary>
    Task<Result<IReadOnlyList<RgbColor>>> GetPaletteColorsAsync(
        int paletteIndex,
        CancellationToken ct = default);

    /// <summary>
    /// Sets palette color.
    /// </summary>
    Task<Result> SetPaletteColorAsync(
        int paletteIndex,
        int colorIndex,
        RgbColor color,
        CancellationToken ct = default);
}

/// <summary>
/// Service for animation preview and playback.
/// </summary>
public interface IAnimationPreviewService
{
    /// <summary>
    /// Renders animation frame as preview.
    /// </summary>
    Task<Result<byte[]>> RenderFramePreviewAsync(
        int groupNumber,
        int imageNumber,
        RenderOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Renders complete animation as GIF/webp.
    /// </summary>
    Task<Result<byte[]>> RenderAnimationAsync(
        int actionNumber,
        RenderOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Gets animation playback state.
    /// </summary>
    Task<Result<AnimationPlaybackState>> GetPlaybackStateAsync(
        int actionNumber,
        CancellationToken ct = default);

    /// <summary>
    /// Plays animation preview.
    /// </summary>
    Task<Result> PlayAnimationAsync(
        int actionNumber,
        PlaybackOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Pauses animation playback.
    /// </summary>
    Task<Result> PauseAnimationAsync(CancellationToken ct = default);

    /// <summary>
    /// Stops animation playback.
    /// </summary>
    Task<Result> StopAnimationAsync(CancellationToken ct = default);

    /// <summary>
    /// Seeks to specific frame.
    /// </summary>
    Task<Result> SeekToFrameAsync(
        int frameIndex,
        CancellationToken ct = default);
}

/// <summary>
/// Service for batch sprite operations.
/// </summary>
public interface ISpriteBatchService
{
    /// <summary>
    /// Batch processes sprites.
    /// </summary>
    Task<Result<BatchOperationResult>> BatchProcessSpritesAsync(
        BatchSpriteOperation operation,
        CancellationToken ct = default);

    /// <summary>
    /// Converts between SFF versions.
    /// </summary>
    Task<Result> ConvertSffVersionAsync(
        string sourcePath,
        string destinationPath,
        SffVersion targetVersion,
        CancellationToken ct = default);

    /// <summary>
    /// Validates sprite consistency.
    /// </summary>
    Task<Result<SpriteValidationReport>> ValidateSpritesAsync(
        SpriteValidationOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Merges multiple SFF files.
    /// </summary>
    Task<Result<SffFile>> MergeSffFilesAsync(
        IReadOnlyList<string> filePaths,
        SpriteMergeOptions options,
        CancellationToken ct = default);
}

/// <summary>
/// Service for sprite project management.
/// </summary>
public interface ISpriteProjectService
{
    /// <summary>
    /// Creates a new sprite project.
    /// </summary>
    Task<Result<SpriteProject>> CreateProjectAsync(
        string name,
        ProjectSettings settings,
        CancellationToken ct = default);

    /// <summary>
    /// Opens an existing sprite project.
    /// </summary>
    Task<Result<SpriteProject>> OpenProjectAsync(
        string projectPath,
        CancellationToken ct = default);

    /// <summary>
    /// Saves the current project.
    /// </summary>
    Task<Result> SaveProjectAsync(
        string? projectPath = null,
        CancellationToken ct = default);

    /// <summary>
    /// Gets project statistics.
    /// </summary>
    Task<Result<ProjectStatistics>> GetProjectStatisticsAsync(
        CancellationToken ct = default);
}

#region Request/Response Models

/// <summary>
/// SFF file data.
/// </summary>
public record SffFile(
    string FilePath,
    SffVersion Version,
    IReadOnlyList<SpriteGroup> Groups,
    IReadOnlyList<Palette> Palettes,
    DateTime LastModified);

/// <summary>
/// SFF file version.
/// </summary>
public enum SffVersion
{
    V1_0,
    V2_0,
    V2_1
}

/// <summary>
/// Sprite group.
/// </summary>
public record SpriteGroup(
    int GroupNumber,
    string? Name,
    IReadOnlyList<Sprite> Sprites);

/// <summary>
/// Sprite data.
/// </summary>
public record Sprite(
    int GroupNumber,
    int ImageNumber,
    int Width,
    int Height,
    int X,
    int Y,
    byte[]? ImageData,
    SpriteMetadata Metadata);

/// <summary>
/// Sprite metadata.
/// </summary>
public record SpriteMetadata(
    string? Name,
    string? Description,
    RgbColor? TransparentColor,
    int PaletteIndex,
    bool IsSharedPalette,
    DateTime? ImportedAt);

/// <summary>
/// Color definition.
/// </summary>
public record RgbColor(byte R, byte G, byte B);

/// <summary>
/// Import sprite options.
/// </summary>
public record ImportSpriteOptions(
    int TargetGroup,
    int StartingImageNumber,
    bool AutoCrop,
    bool GeneratePalette,
    RgbColor? TransparentColor,
    SffVersion TargetVersion);

/// <summary>
/// Export sprite options.
/// </summary>
public record ExportSpriteOptions(
    string OutputDirectory,
    string FileNamePattern,
    SpriteExportFormat Format,
    bool IncludePalette,
    int? ScalePercent = null);

/// <summary>
/// Sprite export format.
/// </summary>
public enum SpriteExportFormat
{
    Png,
    Bmp,
    Gif,
    Jpeg,
    Webp
}

/// <summary>
/// Sprite optimization options.
/// </summary>
public record SpriteOptimizationOptions(
    bool RemoveDuplicates,
    bool CompressImages,
    bool OptimizePalettes,
    bool ReorderGroups,
    int QualityLevel);

/// <summary>
/// SFF optimization result.
/// </summary>
public record SffOptimizationResult(
    int OriginalSize,
    int OptimizedSize,
    int SpritesRemoved,
    int PalettesOptimized,
    double CompressionRatio);

/// <summary>
/// AIR file data.
/// </summary>
public record AirFile(
    string FilePath,
    IReadOnlyList<Animation> Animations,
    IReadOnlyList<AnimationClsn> ClsnData);

/// <summary>
/// Animation data.
/// </summary>
public record Animation(
    int ActionNumber,
    string Name,
    SpriteAnimationType Type,
    IReadOnlyList<AnimationFrame> Frames,
    LoopType Loop,
    int? LoopStartFrame = null);

/// <summary>
/// Animation types.
/// </summary>
public enum SpriteAnimationType
{
    Stand,
    Walk,
    Jump,
    Attack,
    Hit,
    Special,
    Hyper,
    Intro,
    Win,
    Lose,
    Custom
}

/// <summary>
/// Loop types.
/// </summary>
public enum LoopType
{
    NoLoop,
    LoopForever,
    LoopUntilEnd
}

/// <summary>
/// Animation frame.
/// </summary>
public record AnimationFrame(
    int GroupNumber,
    int ImageNumber,
    int X,
    int Y,
    int Time,
    int? FlipH = null,
    int? FlipV = null,
    int? AlphaSource = null,
    int? AlphaDest = null,
    int? ScaleX = null,
    int? ScaleY = null,
    int? Angle = null,
    IReadOnlyList<ClsnBox>? Clsn1 = null,
    IReadOnlyList<ClsnBox>? Clsn2 = null);

/// <summary>
/// Collision box.
/// </summary>
public record ClsnBox(
    int X,
    int Y,
    int Width,
    int Height,
    ClsnType Type);

/// <summary>
/// Collision type.
/// </summary>
public enum ClsnType
{
    Attack,
    Defense
}

/// <summary>
/// Animation CLSN data.
/// </summary>
public record AnimationClsn(
    int ActionNumber,
    int FrameIndex,
    IReadOnlyList<ClsnBox> Boxes);

/// <summary>
/// Palette data.
/// </summary>
public record Palette(
    string Name,
    IReadOnlyList<RgbColor> Colors,
    int ColorCount,
    bool IsShared);

/// <summary>
/// Variation type for palette generation.
/// </summary>
public enum VariationType
{
    HueShift,
    SaturationShift,
    BrightnessShift,
    Complementary,
    Analogous,
    Triadic
}

/// <summary>
/// Render options.
/// </summary>
public record RenderOptions(
    int Width,
    int Height,
    RgbColor BackgroundColor,
    bool ShowGrid,
    bool ShowAxis,
    int? ZoomPercent = null,
    bool ShowClsn = false,
    bool ShowShadow = true);

/// <summary>
/// Animation playback state.
/// </summary>
public record AnimationPlaybackState(
    int ActionNumber,
    int CurrentFrame,
    bool IsPlaying,
    bool IsPaused,
    int TotalFrames,
    TimeSpan Duration,
    TimeSpan CurrentTime);

/// <summary>
/// Playback options.
/// </summary>
public record PlaybackOptions(
    int Fps,
    bool Loop,
    bool ShowClsn,
    int? StartFrame = null,
    int? EndFrame = null);

/// <summary>
/// Batch sprite operation.
/// </summary>
public record BatchSpriteOperation(
    BatchOperationType Type,
    IReadOnlyList<Sprite> TargetSprites,
    IReadOnlyDictionary<string, object> Parameters);

/// <summary>
/// Batch operation types.
/// </summary>
public enum BatchOperationType
{
    Resize,
    Recolor,
    Crop,
    Flip,
    Rotate,
    ApplyPalette,
    ConvertFormat
}

/// <summary>
/// Batch operation result.
/// </summary>
public record BatchOperationResult(
    int ProcessedCount,
    int FailedCount,
    IReadOnlyList<string> Errors,
    TimeSpan Duration);

/// <summary>
/// Sprite validation report.
/// </summary>
public record SpriteValidationReport(
    bool IsValid,
    int ErrorCount,
    int WarningCount,
    IReadOnlyList<ValidationIssue> Issues);

/// <summary>
/// Validation issue.
/// </summary>
public record ValidationIssue(
    ValidationSeverity Severity,
    string Code,
    string Message,
    string? FilePath = null);

/// <summary>
/// Validation severity.
/// </summary>
public enum ValidationSeverity
{
    Info,
    Warning,
    Error
}

/// <summary>
/// Validation options.
/// </summary>
public record SpriteValidationOptions(
    bool CheckMissingSprites,
    bool CheckAnimationTiming,
    bool CheckClsnConsistency,
    bool CheckPaletteUsage,
    bool CheckFileIntegrity);

/// <summary>
/// Merge options.
/// </summary>
public record SpriteMergeOptions(
    int StartingGroupNumber,
    bool RenameConflicts,
    bool MergePalettes,
    bool OptimizeAfterMerge);

/// <summary>
/// Sprite project.
/// </summary>
public record SpriteProject(
    string Name,
    string FilePath,
    ProjectSettings Settings,
    SffFile? SffFile,
    AirFile? AirFile,
    IReadOnlyList<Palette> Palettes,
    DateTime CreatedAt,
    DateTime ModifiedAt);

/// <summary>
/// Project settings.
/// </summary>
public record ProjectSettings(
    string CharacterName,
    string Author,
    string Version,
    int DefaultPaletteIndex,
    SffVersion TargetSffVersion,
    bool AutoSave,
    int AutoSaveIntervalMinutes);

/// <summary>
/// Project statistics.
/// </summary>
public record ProjectStatistics(
    int TotalSprites,
    int TotalAnimations,
    int TotalFrames,
    int TotalPalettes,
    long FileSize,
    DateTime LastSaved);

#endregion
