using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.Mugen.Services;
using SaveState.Infrastructure.Mugen.StoryMode.Managers;

namespace SaveState.Infrastructure.Mugen.StoryMode;

/// <summary>
/// Implementation of story mode service for MUGEN.
/// Provides comprehensive tools for creating narrative-driven gameplay.
/// Acts as a thin coordinator delegating to specialized managers.
/// </summary>
public class StoryModeService : IStoryModeService
{
    private readonly ILogger<StoryModeService> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly StoryProjectManager _projectManager;
    private readonly StoryChapterManager _chapterManager;
    private readonly StorySceneManager _sceneManager;
    private readonly StoryCastingManager _castingManager;
    private readonly StoryContentManager _contentManager;
    private readonly StoryBattleManager _battleManager;
    private readonly StoryTestingManager _testingManager;
    private readonly StoryAssetManager _assetManager;

    public StoryModeService(
        ILogger<StoryModeService> logger,
        ITimeProvider timeProvider,
        StoryProjectManager projectManager,
        StoryChapterManager chapterManager,
        StorySceneManager sceneManager,
        StoryCastingManager castingManager,
        StoryContentManager contentManager,
        StoryBattleManager battleManager,
        StoryTestingManager testingManager,
        StoryAssetManager assetManager)
    {
        _logger = logger;
        _timeProvider = timeProvider;
        _projectManager = projectManager;
        _chapterManager = chapterManager;
        _sceneManager = sceneManager;
        _castingManager = castingManager;
        _contentManager = contentManager;
        _battleManager = battleManager;
        _testingManager = testingManager;
        _assetManager = assetManager;
    }

    #region Story Project Management

    /// <inheritdoc />
    public Task<Result<StoryProject>> CreateProjectAsync(
        string title,
        string? description = null,
        CancellationToken ct = default)
        => _projectManager.CreateProjectAsync(title, description, ct);

    /// <inheritdoc />
    public Task<Result<StoryProject>> OpenProjectAsync(
        string projectPath,
        CancellationToken ct = default)
        => _projectManager.OpenProjectAsync(projectPath, ct);

    /// <inheritdoc />
    public Task<Result> SaveProjectAsync(
        string? projectPath = null,
        CancellationToken ct = default)
        => _projectManager.SaveProjectAsync(projectPath, ct);

    /// <inheritdoc />
    public Task<Result<StoryProjectStats>> GetProjectStatsAsync(
        CancellationToken ct = default)
    {
        var dialogueCount = _sceneManager.Scenes.Values.Sum(s => s.Content.Dialogue?.Count ?? 0);
        var battleCount = _sceneManager.Scenes.Values.Count(s => s.Type == SceneType.Battle);
        var choiceCount = _sceneManager.Scenes.Values.Sum(s => s.Content.Choices?.Count ?? 0);
        var totalAssetSize = _assetManager.Assets.Values.Sum(a => a.FileSize);

        return _projectManager.GetProjectStatsAsync(
            _chapterManager.Chapters.Count,
            _sceneManager.Scenes.Count,
            dialogueCount,
            battleCount,
            choiceCount,
            _castingManager.Cast.Count,
            totalAssetSize,
            ct);
    }

    /// <inheritdoc />
    public Task<Result<string>> ExportForMugenAsync(
        string outputDirectory,
        MugenStoryExportOptions options,
        CancellationToken ct = default)
        => _projectManager.ExportForMugenAsync(
            outputDirectory,
            options,
            _chapterManager.Chapters.Count,
            _sceneManager.Scenes.Count,
            _castingManager.Cast.Count,
            ct);

    #endregion

    #region Chapter Management

    /// <inheritdoc />
    public async Task<Result<StoryChapter>> CreateChapterAsync(
        string title,
        int? orderIndex = null,
        CancellationToken ct = default)
    {
        var result = await _chapterManager.CreateChapterAsync(title, orderIndex, ct);

        if (result.IsSuccess && _projectManager.CurrentProject != null)
        {
            var updatedChapters = _projectManager.CurrentProject.Chapters.ToList();
            updatedChapters.Add(result.Value!);
            _projectManager.SetCurrentProject(_projectManager.CurrentProject with { Chapters = updatedChapters });
        }

        return result;
    }

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<StoryChapter>>> GetChaptersAsync(
        CancellationToken ct = default)
        => _chapterManager.GetChaptersAsync(ct);

    /// <inheritdoc />
    public Task<Result> ReorderChaptersAsync(
        IReadOnlyList<Guid> chapterIds,
        CancellationToken ct = default)
        => _chapterManager.ReorderChaptersAsync(chapterIds, ct);

    /// <inheritdoc />
    public Task<Result> DeleteChapterAsync(
        Guid chapterId,
        CancellationToken ct = default)
        => _chapterManager.DeleteChapterAsync(chapterId, ct);

    /// <inheritdoc />
    public Task<Result<StoryChapter>> DuplicateChapterAsync(
        Guid chapterId,
        CancellationToken ct = default)
        => _chapterManager.DuplicateChapterAsync(chapterId, ct);

    #endregion

    #region Scene Management

    /// <inheritdoc />
    public Task<Result<StoryScene>> CreateSceneAsync(
        Guid chapterId,
        string name,
        SceneType type,
        CancellationToken ct = default)
        => _sceneManager.CreateSceneAsync(chapterId, name, type, ct);

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<StoryScene>>> GetScenesAsync(
        Guid chapterId,
        CancellationToken ct = default)
        => _sceneManager.GetScenesAsync(chapterId, ct);

    /// <inheritdoc />
    public Task<Result> UpdateSceneContentAsync(
        Guid sceneId,
        SceneContent content,
        CancellationToken ct = default)
        => _sceneManager.UpdateSceneContentAsync(sceneId, content, ct);

    /// <inheritdoc />
    public Task<Result> SetSceneBackgroundAsync(
        Guid sceneId,
        string backgroundPath,
        BackgroundSettings settings,
        CancellationToken ct = default)
        => _sceneManager.SetSceneBackgroundAsync(sceneId, backgroundPath, settings, ct);

    /// <inheritdoc />
    public Task<Result> SetSceneMusicAsync(
        Guid sceneId,
        string musicPath,
        MusicSettings settings,
        CancellationToken ct = default)
        => _sceneManager.SetSceneMusicAsync(sceneId, musicPath, settings, ct);

    /// <inheritdoc />
    public Task<Result> SetSceneTransitionAsync(
        Guid sceneId,
        SceneTransition transition,
        CancellationToken ct = default)
        => _sceneManager.SetSceneTransitionAsync(sceneId, transition, ct);

    #endregion

    #region Character Casting

    /// <inheritdoc />
    public Task<Result<StoryCharacter>> AddCastMemberAsync(
        Guid characterId,
        CastingOptions options,
        CancellationToken ct = default)
        => _castingManager.AddCastMemberAsync(characterId, options, ct);

    /// <inheritdoc />
    public Task<Result> RemoveCastMemberAsync(
        Guid castMemberId,
        CancellationToken ct = default)
        => _castingManager.RemoveCastMemberAsync(castMemberId, ct);

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<StoryCharacter>>> GetCastAsync(
        CancellationToken ct = default)
        => _castingManager.GetCastAsync(ct);

    /// <inheritdoc />
    public Task<Result> SetCharacterAppearanceAsync(
        Guid castMemberId,
        CharacterAppearance appearance,
        CancellationToken ct = default)
        => _castingManager.SetCharacterAppearanceAsync(castMemberId, appearance, ct);

    /// <inheritdoc />
    public Task<Result> SetCharacterAiAsync(
        Guid castMemberId,
        StoryAiSettings aiSettings,
        CancellationToken ct = default)
        => _castingManager.SetCharacterAiAsync(castMemberId, aiSettings, ct);

    #endregion

    #region Dialogue System

    /// <inheritdoc />
    public Task<Result<DialogueLine>> AddDialogueAsync(
        Guid sceneId,
        DialogueLine line,
        int? insertIndex = null,
        CancellationToken ct = default)
        => _contentManager.AddDialogueAsync(sceneId, line, insertIndex, ct);

    /// <inheritdoc />
    public Task<Result> UpdateDialogueAsync(
        Guid dialogueId,
        DialogueLine line,
        CancellationToken ct = default)
        => _contentManager.UpdateDialogueAsync(dialogueId, line, ct);

    /// <inheritdoc />
    public Task<Result> RemoveDialogueAsync(
        Guid dialogueId,
        CancellationToken ct = default)
        => _contentManager.RemoveDialogueAsync(dialogueId, ct);

    /// <inheritdoc />
    public Task<Result> SetSpeakerAsync(
        Guid dialogueId,
        Guid? castMemberId,
        SpeakerPosition position,
        CancellationToken ct = default)
        => _contentManager.SetSpeakerAsync(dialogueId, castMemberId, position, ct);

    /// <inheritdoc />
    public Task<Result> SetVoiceLineAsync(
        Guid dialogueId,
        string voicePath,
        CancellationToken ct = default)
        => _contentManager.SetVoiceLineAsync(dialogueId, voicePath, ct);

    /// <inheritdoc />
    public Task<Result> SetTextSettingsAsync(
        Guid dialogueId,
        TextDisplaySettings settings,
        CancellationToken ct = default)
        => _contentManager.SetTextSettingsAsync(dialogueId, settings, ct);

    #endregion

    #region Cutscene Editor

    /// <inheritdoc />
    public Task<Result<CutsceneElement>> AddCutsceneElementAsync(
        Guid sceneId,
        CutsceneElement element,
        int? insertIndex = null,
        CancellationToken ct = default)
        => _contentManager.AddCutsceneElementAsync(sceneId, element, insertIndex, ct);

    /// <inheritdoc />
    public Task<Result> UpdateCutsceneElementAsync(
        Guid elementId,
        CutsceneElement element,
        CancellationToken ct = default)
        => _contentManager.UpdateCutsceneElementAsync(elementId, element, ct);

    /// <inheritdoc />
    public Task<Result> SetCameraMovementAsync(
        Guid sceneId,
        CameraPath cameraPath,
        CancellationToken ct = default)
        => _contentManager.SetCameraMovementAsync(sceneId, cameraPath, ct);

    /// <inheritdoc />
    public Task<Result> AddVisualEffectAsync(
        Guid sceneId,
        VisualEffect effect,
        CancellationToken ct = default)
        => _contentManager.AddVisualEffectAsync(sceneId, effect, ct);

    /// <inheritdoc />
    public Task<Result> SetCharacterAnimationAsync(
        Guid sceneId,
        Guid castMemberId,
        string animationName,
        AnimationSettings settings,
        CancellationToken ct = default)
        => _contentManager.SetCharacterAnimationAsync(sceneId, castMemberId, animationName, settings, ct);

    /// <inheritdoc />
    public Task<Result> SetCharacterPositionAsync(
        Guid sceneId,
        Guid castMemberId,
        Position3D position,
        CancellationToken ct = default)
        => _contentManager.SetCharacterPositionAsync(sceneId, castMemberId, position, ct);

    #endregion

    #region Branching and Choices

    /// <inheritdoc />
    public Task<Result<StoryChoice>> AddChoiceAsync(
        Guid sceneId,
        StoryChoice choice,
        CancellationToken ct = default)
        => _contentManager.AddChoiceAsync(sceneId, choice, ct);

    /// <inheritdoc />
    public Task<Result> SetChoiceConsequencesAsync(
        Guid choiceId,
        ChoiceConsequences consequences,
        CancellationToken ct = default)
        => _contentManager.SetChoiceConsequencesAsync(choiceId, consequences, ct);

    /// <inheritdoc />
    public Task<Result> SetBranchConditionAsync(
        Guid sceneId,
        BranchCondition condition,
        CancellationToken ct = default)
        => _contentManager.SetBranchConditionAsync(sceneId, condition, ct);

    /// <inheritdoc />
    public Task<Result> SetStoryVariableAsync(
        string variableName,
        object value,
        CancellationToken ct = default)
        => _contentManager.SetStoryVariableAsync(variableName, value, ct);

    /// <inheritdoc />
    public Task<Result<object>> GetStoryVariableAsync(
        string variableName,
        CancellationToken ct = default)
        => _contentManager.GetStoryVariableAsync(variableName, ct);

    /// <inheritdoc />
    public Task<Result<BranchValidationResult>> ValidateBranchingAsync(
        CancellationToken ct = default)
        => _contentManager.ValidateBranchingAsync(ct);

    #endregion

    #region Battle Integration

    /// <inheritdoc />
    public Task<Result<StoryBattle>> AddBattleAsync(
        Guid sceneId,
        StoryBattle battle,
        CancellationToken ct = default)
        => _battleManager.AddBattleAsync(sceneId, battle, _sceneManager.Scenes, ct);

    /// <inheritdoc />
    public Task<Result> SetBattleConditionsAsync(
        Guid battleId,
        BattleConditions conditions,
        CancellationToken ct = default)
        => _battleManager.SetBattleConditionsAsync(battleId, conditions, ct);

    /// <inheritdoc />
    public Task<Result> SetPostBattleSceneAsync(
        Guid battleId,
        Guid? winSceneId,
        Guid? loseSceneId,
        CancellationToken ct = default)
        => _battleManager.SetPostBattleSceneAsync(battleId, winSceneId, loseSceneId, ct);

    /// <inheritdoc />
    public Task<Result> ConfigureBossBattleAsync(
        Guid battleId,
        BossBattleSettings settings,
        CancellationToken ct = default)
        => _battleManager.ConfigureBossBattleAsync(battleId, settings, ct);

    #endregion

    #region Preview and Testing

    /// <inheritdoc />
    public Task<Result<ScenePreview>> PreviewSceneAsync(
        Guid sceneId,
        StoryPreviewOptions options,
        CancellationToken ct = default)
        => _testingManager.PreviewSceneAsync(sceneId, options, ct);

    /// <inheritdoc />
    public Task<Result> PlayStoryAsync(
        Guid? startChapterId = null,
        CancellationToken ct = default)
        => _testingManager.PlayStoryAsync(startChapterId, ct);

    /// <inheritdoc />
    public Task<Result> StopPlaybackAsync(CancellationToken ct = default)
        => _testingManager.StopPlaybackAsync(ct);

    /// <inheritdoc />
    public Task<Result<StoryPathSimulation>> SimulatePathAsync(
        IReadOnlyList<int> choices,
        CancellationToken ct = default)
        => _testingManager.SimulatePathAsync(choices, ct);

    /// <inheritdoc />
    public Task<Result<StoryTestResult>> TestStoryAsync(
        CancellationToken ct = default)
        => _testingManager.TestStoryAsync(ct);

    #endregion

    #region Asset Management

    /// <inheritdoc />
    public Task<Result<StoryAsset>> ImportAssetAsync(
        string filePath,
        AssetType type,
        CancellationToken ct = default)
        => _assetManager.ImportAssetAsync(filePath, type, ct);

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<StoryAsset>>> GetAssetsAsync(
        AssetType? typeFilter = null,
        CancellationToken ct = default)
        => _assetManager.GetAssetsAsync(typeFilter, ct);

    /// <inheritdoc />
    public Task<Result<AssetValidationResult>> ValidateAssetsAsync(
        CancellationToken ct = default)
        => _assetManager.ValidateAssetsAsync(ct);

    /// <inheritdoc />
    public Task<Result<StoryAssetOptimizationResult>> OptimizeAssetsAsync(
        OptimizationOptions options,
        CancellationToken ct = default)
        => _assetManager.OptimizeAssetsAsync(options, ct);

    #endregion
}
