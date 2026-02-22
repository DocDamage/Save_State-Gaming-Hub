using SaveState.Core.Common;

namespace SaveState.Core.Mugen.Services;

/// <summary>
/// Marker interface for story mode services.
/// Use specific sub-interfaces (IStoryProjectService, IStoryChapterService, etc.) for actual operations.
/// </summary>
public interface IStoryModeService
{
}

/// <summary>
/// Service for story project lifecycle management.
/// </summary>
public interface IStoryProjectService
{
    /// <summary>
    /// Creates a new story mode project.
    /// </summary>
    Task<Result<StoryProject>> CreateProjectAsync(
        string title,
        string? description = null,
        CancellationToken ct = default);

    /// <summary>
    /// Opens an existing story project.
    /// </summary>
    Task<Result<StoryProject>> OpenProjectAsync(
        string projectPath,
        CancellationToken ct = default);

    /// <summary>
    /// Saves the current story project.
    /// </summary>
    Task<Result> SaveProjectAsync(
        string? projectPath = null,
        CancellationToken ct = default);

    /// <summary>
    /// Gets project metadata and statistics.
    /// </summary>
    Task<Result<StoryProjectStats>> GetProjectStatsAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Exports story mode for MUGEN.
    /// </summary>
    Task<Result<string>> ExportForMugenAsync(
        string outputDirectory,
        MugenStoryExportOptions options,
        CancellationToken ct = default);
}

/// <summary>
/// Service for story chapter management.
/// </summary>
public interface IStoryChapterService
{
    /// <summary>
    /// Creates a new story chapter.
    /// </summary>
    Task<Result<StoryChapter>> CreateChapterAsync(
        string title,
        int? orderIndex = null,
        CancellationToken ct = default);

    /// <summary>
    /// Gets all chapters in the story.
    /// </summary>
    Task<Result<IReadOnlyList<StoryChapter>>> GetChaptersAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Reorders chapters.
    /// </summary>
    Task<Result> ReorderChaptersAsync(
        IReadOnlyList<Guid> chapterIds,
        CancellationToken ct = default);

    /// <summary>
    /// Deletes a chapter.
    /// </summary>
    Task<Result> DeleteChapterAsync(
        Guid chapterId,
        CancellationToken ct = default);

    /// <summary>
    /// Duplicates a chapter.
    /// </summary>
    Task<Result<StoryChapter>> DuplicateChapterAsync(
        Guid chapterId,
        CancellationToken ct = default);
}

/// <summary>
/// Service for story scene management.
/// </summary>
public interface IStorySceneService
{
    /// <summary>
    /// Creates a new scene.
    /// </summary>
    Task<Result<StoryScene>> CreateSceneAsync(
        Guid chapterId,
        string name,
        SceneType type,
        CancellationToken ct = default);

    /// <summary>
    /// Gets scenes in a chapter.
    /// </summary>
    Task<Result<IReadOnlyList<StoryScene>>> GetScenesAsync(
        Guid chapterId,
        CancellationToken ct = default);

    /// <summary>
    /// Updates scene content.
    /// </summary>
    Task<Result> UpdateSceneContentAsync(
        Guid sceneId,
        SceneContent content,
        CancellationToken ct = default);

    /// <summary>
    /// Sets scene background.
    /// </summary>
    Task<Result> SetSceneBackgroundAsync(
        Guid sceneId,
        string backgroundPath,
        BackgroundSettings settings,
        CancellationToken ct = default);

    /// <summary>
    /// Sets scene music.
    /// </summary>
    Task<Result> SetSceneMusicAsync(
        Guid sceneId,
        string musicPath,
        MusicSettings settings,
        CancellationToken ct = default);

    /// <summary>
    /// Configures scene transition.
    /// </summary>
    Task<Result> SetSceneTransitionAsync(
        Guid sceneId,
        SceneTransition transition,
        CancellationToken ct = default);
}

/// <summary>
/// Service for story dialogue management.
/// </summary>
public interface IStoryDialogueService
{
    /// <summary>
    /// Adds dialogue line.
    /// </summary>
    Task<Result<DialogueLine>> AddDialogueAsync(
        Guid sceneId,
        DialogueLine line,
        int? insertIndex = null,
        CancellationToken ct = default);

    /// <summary>
    /// Updates dialogue line.
    /// </summary>
    Task<Result> UpdateDialogueAsync(
        Guid dialogueId,
        DialogueLine line,
        CancellationToken ct = default);

    /// <summary>
    /// Removes dialogue line.
    /// </summary>
    Task<Result> RemoveDialogueAsync(
        Guid dialogueId,
        CancellationToken ct = default);

    /// <summary>
    /// Sets dialogue speaker.
    /// </summary>
    Task<Result> SetSpeakerAsync(
        Guid dialogueId,
        Guid? castMemberId,
        SpeakerPosition position,
        CancellationToken ct = default);

    /// <summary>
    /// Adds voice line to dialogue.
    /// </summary>
    Task<Result> SetVoiceLineAsync(
        Guid dialogueId,
        string voicePath,
        CancellationToken ct = default);

    /// <summary>
    /// Configures text display options.
    /// </summary>
    Task<Result> SetTextSettingsAsync(
        Guid dialogueId,
        TextDisplaySettings settings,
        CancellationToken ct = default);
}

/// <summary>
/// Service for story cutscene editing.
/// </summary>
public interface IStoryCutsceneService
{
    /// <summary>
    /// Adds cutscene element.
    /// </summary>
    Task<Result<CutsceneElement>> AddCutsceneElementAsync(
        Guid sceneId,
        CutsceneElement element,
        int? insertIndex = null,
        CancellationToken ct = default);

    /// <summary>
    /// Updates cutscene element.
    /// </summary>
    Task<Result> UpdateCutsceneElementAsync(
        Guid elementId,
        CutsceneElement element,
        CancellationToken ct = default);

    /// <summary>
    /// Sets camera movement.
    /// </summary>
    Task<Result> SetCameraMovementAsync(
        Guid sceneId,
        CameraPath cameraPath,
        CancellationToken ct = default);

    /// <summary>
    /// Adds visual effect.
    /// </summary>
    Task<Result> AddVisualEffectAsync(
        Guid sceneId,
        VisualEffect effect,
        CancellationToken ct = default);

    /// <summary>
    /// Sets character animation in scene.
    /// </summary>
    Task<Result> SetCharacterAnimationAsync(
        Guid sceneId,
        Guid castMemberId,
        string animationName,
        AnimationSettings settings,
        CancellationToken ct = default);

    /// <summary>
    /// Sets character position in scene.
    /// </summary>
    Task<Result> SetCharacterPositionAsync(
        Guid sceneId,
        Guid castMemberId,
        Position3D position,
        CancellationToken ct = default);
}

/// <summary>
/// Service for story branching and choices.
/// </summary>
public interface IStoryBranchingService
{
    /// <summary>
    /// Adds player choice.
    /// </summary>
    Task<Result<StoryChoice>> AddChoiceAsync(
        Guid sceneId,
        StoryChoice choice,
        CancellationToken ct = default);

    /// <summary>
    /// Sets choice consequences.
    /// </summary>
    Task<Result> SetChoiceConsequencesAsync(
        Guid choiceId,
        ChoiceConsequences consequences,
        CancellationToken ct = default);

    /// <summary>
    /// Configures branching path.
    /// </summary>
    Task<Result> SetBranchConditionAsync(
        Guid sceneId,
        BranchCondition condition,
        CancellationToken ct = default);

    /// <summary>
    /// Sets variable for story state.
    /// </summary>
    Task<Result> SetStoryVariableAsync(
        string variableName,
        object value,
        CancellationToken ct = default);

    /// <summary>
    /// Gets story variable.
    /// </summary>
    Task<Result<object>> GetStoryVariableAsync(
        string variableName,
        CancellationToken ct = default);

    /// <summary>
    /// Validates story branching logic.
    /// </summary>
    Task<Result<BranchValidationResult>> ValidateBranchingAsync(
        CancellationToken ct = default);
}

/// <summary>
/// Service for story battle integration.
/// </summary>
public interface IStoryBattleIntegrationService
{
    /// <summary>
    /// Adds battle to story.
    /// </summary>
    Task<Result<StoryBattle>> AddBattleAsync(
        Guid sceneId,
        StoryBattle battle,
        CancellationToken ct = default);

    /// <summary>
    /// Configures battle conditions.
    /// </summary>
    Task<Result> SetBattleConditionsAsync(
        Guid battleId,
        BattleConditions conditions,
        CancellationToken ct = default);

    /// <summary>
    /// Sets post-battle scene.
    /// </summary>
    Task<Result> SetPostBattleSceneAsync(
        Guid battleId,
        Guid? winSceneId,
        Guid? loseSceneId,
        CancellationToken ct = default);

    /// <summary>
    /// Configures boss battle.
    /// </summary>
    Task<Result> ConfigureBossBattleAsync(
        Guid battleId,
        BossBattleSettings settings,
        CancellationToken ct = default);
}

/// <summary>
/// Service for story testing and preview.
/// </summary>
public interface IStoryTestingService
{
    /// <summary>
    /// Previews scene.
    /// </summary>
    Task<Result<ScenePreview>> PreviewSceneAsync(
        Guid sceneId,
        StoryPreviewOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Plays story from current position.
    /// </summary>
    Task<Result> PlayStoryAsync(
        Guid? startChapterId = null,
        CancellationToken ct = default);

    /// <summary>
    /// Stops story playback.
    /// </summary>
    Task<Result> StopPlaybackAsync(CancellationToken ct = default);

    /// <summary>
    /// Simulates story path.
    /// </summary>
    Task<Result<StoryPathSimulation>> SimulatePathAsync(
        IReadOnlyList<int> choices,
        CancellationToken ct = default);

    /// <summary>
    /// Tests story completeness.
    /// </summary>
    Task<Result<StoryTestResult>> TestStoryAsync(
        CancellationToken ct = default);
}

/// <summary>
/// Service for story asset management.
/// </summary>
public interface IStoryAssetService
{
    /// <summary>
    /// Imports story asset.
    /// </summary>
    Task<Result<StoryAsset>> ImportAssetAsync(
        string filePath,
        AssetType type,
        CancellationToken ct = default);

    /// <summary>
    /// Gets story assets.
    /// </summary>
    Task<Result<IReadOnlyList<StoryAsset>>> GetAssetsAsync(
        AssetType? typeFilter = null,
        CancellationToken ct = default);

    /// <summary>
    /// Validates asset references.
    /// </summary>
    Task<Result<AssetValidationResult>> ValidateAssetsAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Optimizes story assets.
    /// </summary>
    Task<Result<StoryAssetOptimizationResult>> OptimizeAssetsAsync(
        StoryAssetOptimizationOptions options,
        CancellationToken ct = default);
}
