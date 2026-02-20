using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.Mugen.Services;
using SaveState.Infrastructure.Mugen.SpriteAnimation.Managers;

namespace SaveState.Infrastructure.Mugen.SpriteAnimation;

/// <summary>
/// Implementation of sprite and animation service for MUGEN characters.
/// Acts as a coordinator delegating to specialized managers.
/// </summary>
public class SpriteAnimationService : ISpriteAnimationService
{
    private readonly ILogger<SpriteAnimationService> _logger;
    private readonly ITimeProvider _timeProvider;

    // State dictionaries shared across managers
    private readonly ConcurrentDictionary<int, SpriteGroup> _spriteGroups = new();
    private readonly ConcurrentDictionary<int, Animation> _animations = new();
    private readonly ConcurrentDictionary<int, Palette> _palettes = new();

    // Managers
    private readonly SpriteManager _spriteManager;
    private readonly AnimationManager _animationManager;
    private readonly PaletteManager _paletteManager;
    private readonly PreviewManager _previewManager;
    private readonly BatchOperationManager _batchOperationManager;
    private readonly ProjectManager _projectManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="SpriteAnimationService"/> class.
    /// </summary>
    public SpriteAnimationService(
        ILogger<SpriteAnimationService> logger,
        ITimeProvider timeProvider,
        SpriteManager spriteManager,
        AnimationManager animationManager,
        PaletteManager paletteManager,
        PreviewManager previewManager,
        BatchOperationManager batchOperationManager,
        ProjectManager projectManager)
    {
        _logger = logger;
        _timeProvider = timeProvider;
        _spriteManager = spriteManager;
        _animationManager = animationManager;
        _paletteManager = paletteManager;
        _previewManager = previewManager;
        _batchOperationManager = batchOperationManager;
        _projectManager = projectManager;
    }

    #region Sprite Management

    /// <inheritdoc />
    public Task<Result<SffFile>> LoadSffFileAsync(string filePath, CancellationToken ct = default)
        => _spriteManager.LoadSffFileAsync(filePath, _palettes, ct);

    /// <inheritdoc />
    public Task<Result> SaveSffFileAsync(string filePath, SffFile sffFile, CancellationToken ct = default)
        => _spriteManager.SaveSffFileAsync(filePath, sffFile, ct);

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<Sprite>>> ImportSpritesAsync(
        IReadOnlyList<string> imagePaths,
        ImportSpriteOptions options,
        CancellationToken ct = default)
        => _spriteManager.ImportSpritesAsync(imagePaths, options, ct);

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<string>>> ExportSpritesAsync(
        IReadOnlyList<Sprite> sprites,
        ExportSpriteOptions options,
        CancellationToken ct = default)
        => _spriteManager.ExportSpritesAsync(sprites, options, ct);

    /// <inheritdoc />
    public Task<Result<Sprite>> AddSpriteAsync(
        Guid groupId,
        int imageNumber,
        byte[] imageData,
        SpriteMetadata metadata,
        CancellationToken ct = default)
        => _spriteManager.AddSpriteAsync(groupId, imageNumber, imageData, metadata, ct);

    /// <inheritdoc />
    public Task<Result> RemoveSpriteAsync(
        int groupNumber,
        int imageNumber,
        CancellationToken ct = default)
        => _spriteManager.RemoveSpriteAsync(groupNumber, imageNumber, ct);

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<Sprite>>> GetSpritesByGroupAsync(
        int groupNumber,
        CancellationToken ct = default)
        => _spriteManager.GetSpritesByGroupAsync(groupNumber, ct);

    /// <inheritdoc />
    public Task<Result<Sprite>> GetSpriteAsync(
        int groupNumber,
        int imageNumber,
        CancellationToken ct = default)
        => _spriteManager.GetSpriteAsync(groupNumber, imageNumber, ct);

    /// <inheritdoc />
    public Task<Result<SffOptimizationResult>> OptimizeSffAsync(
        string filePath,
        SpriteOptimizationOptions options,
        CancellationToken ct = default)
        => _spriteManager.OptimizeSffAsync(filePath, options, ct);

    #endregion

    #region Animation Management

    /// <inheritdoc />
    public Task<Result<AirFile>> LoadAirFileAsync(string filePath, CancellationToken ct = default)
        => _animationManager.LoadAirFileAsync(filePath, ct);

    /// <inheritdoc />
    public Task<Result> SaveAirFileAsync(string filePath, AirFile airFile, CancellationToken ct = default)
        => _animationManager.SaveAirFileAsync(filePath, airFile, ct);

    /// <inheritdoc />
    public Task<Result<Animation>> CreateAnimationAsync(
        int actionNumber,
        string name,
        SpriteAnimationType type,
        CancellationToken ct = default)
        => _animationManager.CreateAnimationAsync(actionNumber, name, type, ct);

    /// <inheritdoc />
    public Task<Result> AddAnimationFrameAsync(
        int actionNumber,
        AnimationFrame frame,
        int? insertIndex = null,
        CancellationToken ct = default)
        => _animationManager.AddAnimationFrameAsync(actionNumber, frame, insertIndex, ct);

    /// <inheritdoc />
    public Task<Result> RemoveAnimationFrameAsync(
        int actionNumber,
        int frameIndex,
        CancellationToken ct = default)
        => _animationManager.RemoveAnimationFrameAsync(actionNumber, frameIndex, ct);

    /// <inheritdoc />
    public Task<Result> UpdateAnimationFrameAsync(
        int actionNumber,
        int frameIndex,
        AnimationFrame frame,
        CancellationToken ct = default)
        => _animationManager.UpdateAnimationFrameAsync(actionNumber, frameIndex, frame, ct);

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<Animation>>> GetAnimationsAsync(
        SpriteAnimationType? typeFilter = null,
        CancellationToken ct = default)
        => _animationManager.GetAnimationsAsync(typeFilter, ct);

    /// <inheritdoc />
    public Task<Result<Animation>> GetAnimationAsync(
        int actionNumber,
        CancellationToken ct = default)
        => _animationManager.GetAnimationAsync(actionNumber, ct);

    /// <inheritdoc />
    public Task<Result<Animation>> DuplicateAnimationAsync(
        int sourceActionNumber,
        int newActionNumber,
        CancellationToken ct = default)
        => _animationManager.DuplicateAnimationAsync(sourceActionNumber, newActionNumber, ct);

    /// <inheritdoc />
    public Task<Result> DeleteAnimationAsync(
        int actionNumber,
        CancellationToken ct = default)
        => _animationManager.DeleteAnimationAsync(actionNumber, ct);

    #endregion

    #region Palette Management

    /// <inheritdoc />
    public Task<Result<Palette>> LoadPaletteAsync(string filePath, CancellationToken ct = default)
        => _paletteManager.LoadPaletteAsync(filePath, ct);

    /// <inheritdoc />
    public Task<Result> SavePaletteAsync(string filePath, Palette palette, CancellationToken ct = default)
        => _paletteManager.SavePaletteAsync(filePath, palette, ct);

    /// <inheritdoc />
    public Task<Result<Palette>> CreatePaletteAsync(
        string name,
        IReadOnlyList<RgbColor> colors,
        CancellationToken ct = default)
        => _paletteManager.CreatePaletteAsync(name, colors, ct);

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<Sprite>>> ApplyPaletteAsync(
        IReadOnlyList<Sprite> sprites,
        Palette palette,
        CancellationToken ct = default)
        => _paletteManager.ApplyPaletteAsync(sprites, palette, ct);

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<Palette>>> GeneratePaletteVariationsAsync(
        Palette basePalette,
        int variationCount,
        VariationType type,
        CancellationToken ct = default)
        => _paletteManager.GeneratePaletteVariationsAsync(basePalette, variationCount, type, ct);

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<RgbColor>>> GetPaletteColorsAsync(
        int paletteIndex,
        CancellationToken ct = default)
        => _paletteManager.GetPaletteColorsAsync(paletteIndex, ct);

    /// <inheritdoc />
    public Task<Result> SetPaletteColorAsync(
        int paletteIndex,
        int colorIndex,
        RgbColor color,
        CancellationToken ct = default)
        => _paletteManager.SetPaletteColorAsync(paletteIndex, colorIndex, color, ct);

    #endregion

    #region Preview and Playback

    /// <inheritdoc />
    public Task<Result<byte[]>> RenderFramePreviewAsync(
        int groupNumber,
        int imageNumber,
        RenderOptions options,
        CancellationToken ct = default)
        => _previewManager.RenderFramePreviewAsync(groupNumber, imageNumber, options, ct);

    /// <inheritdoc />
    public Task<Result<byte[]>> RenderAnimationAsync(
        int actionNumber,
        RenderOptions options,
        CancellationToken ct = default)
        => _previewManager.RenderAnimationAsync(actionNumber, options, ct);

    /// <inheritdoc />
    public Task<Result<AnimationPlaybackState>> GetPlaybackStateAsync(
        int actionNumber,
        CancellationToken ct = default)
        => _previewManager.GetPlaybackStateAsync(actionNumber, ct);

    /// <inheritdoc />
    public Task<Result> PlayAnimationAsync(
        int actionNumber,
        PlaybackOptions options,
        CancellationToken ct = default)
        => _previewManager.PlayAnimationAsync(actionNumber, options, ct);

    /// <inheritdoc />
    public Task<Result> PauseAnimationAsync(CancellationToken ct = default)
        => _previewManager.PauseAnimationAsync(ct);

    /// <inheritdoc />
    public Task<Result> StopAnimationAsync(CancellationToken ct = default)
        => _previewManager.StopAnimationAsync(ct);

    /// <inheritdoc />
    public Task<Result> SeekToFrameAsync(
        int frameIndex,
        CancellationToken ct = default)
        => _previewManager.SeekToFrameAsync(frameIndex, ct);

    #endregion

    #region Batch Operations

    /// <inheritdoc />
    public Task<Result<BatchOperationResult>> BatchProcessSpritesAsync(
        BatchSpriteOperation operation,
        CancellationToken ct = default)
        => _batchOperationManager.BatchProcessSpritesAsync(operation, ct);

    /// <inheritdoc />
    public Task<Result> ConvertSffVersionAsync(
        string sourcePath,
        string destinationPath,
        SffVersion targetVersion,
        CancellationToken ct = default)
        => _spriteManager.ConvertSffVersionAsync(sourcePath, destinationPath, targetVersion, ct);

    /// <inheritdoc />
    public Task<Result<SpriteValidationReport>> ValidateSpritesAsync(
        SpriteValidationOptions options,
        CancellationToken ct = default)
        => _batchOperationManager.ValidateSpritesAsync(options, ct);

    /// <inheritdoc />
    public Task<Result<SffFile>> MergeSffFilesAsync(
        IReadOnlyList<string> filePaths,
        SpriteMergeOptions options,
        CancellationToken ct = default)
        => _batchOperationManager.MergeSffFilesAsync(filePaths, options, LoadSffFileAsync, ct);

    #endregion

    #region Project Management

    /// <inheritdoc />
    public Task<Result<SpriteProject>> CreateProjectAsync(
        string name,
        ProjectSettings settings,
        CancellationToken ct = default)
        => _projectManager.CreateProjectAsync(name, settings, ct);

    /// <inheritdoc />
    public Task<Result<SpriteProject>> OpenProjectAsync(
        string projectPath,
        CancellationToken ct = default)
        => _projectManager.OpenProjectAsync(projectPath, ct);

    /// <inheritdoc />
    public Task<Result> SaveProjectAsync(
        string? projectPath = null,
        CancellationToken ct = default)
        => _projectManager.SaveProjectAsync(projectPath, ct);

    /// <inheritdoc />
    public Task<Result<ProjectStatistics>> GetProjectStatisticsAsync(
        CancellationToken ct = default)
        => _projectManager.GetProjectStatisticsAsync(ct);

    #endregion
}
