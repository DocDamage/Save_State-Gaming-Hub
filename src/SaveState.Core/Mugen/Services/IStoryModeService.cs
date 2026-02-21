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

#region Request/Response Models

/// <summary>
/// Story project.
/// </summary>
public record StoryProject(
    Guid Id,
    string Title,
    string? Description,
    string FilePath,
    DateTime CreatedAt,
    DateTime ModifiedAt,
    StoryVersion Version,
    IReadOnlyList<StoryChapter> Chapters,
    IReadOnlyList<StoryCharacter> Cast,
    StorySettings Settings);

/// <summary>
/// Story version.
/// </summary>
public record StoryVersion(int Major, int Minor, int Patch);

/// <summary>
/// Story settings.
/// </summary>
public record StorySettings(
    string DefaultBackground,
    string DefaultMusic,
    TextSpeed TextSpeed,
    bool AutoSave,
    bool SkipReadText);

/// <summary>
/// Text speed.
/// </summary>
public enum TextSpeed
{
    Slow,
    Normal,
    Fast,
    Instant
}

/// <summary>
/// Story chapter.
/// </summary>
public record StoryChapter(
    Guid Id,
    string Title,
    string? Description,
    int OrderIndex,
    IReadOnlyList<StoryScene> Scenes,
    ChapterUnlockCondition? UnlockCondition);

/// <summary>
/// Chapter unlock condition.
/// </summary>
public record ChapterUnlockCondition(
    UnlockType Type,
    string? RequiredChapterId,
    int? RequiredCompletionPercent);

/// <summary>
/// Unlock type.
/// </summary>
public enum UnlockType
{
    Immediate,
    PreviousCompleted,
    StoryProgress,
    SpecialCondition
}

/// <summary>
/// Story scene.
/// </summary>
public record StoryScene(
    Guid Id,
    string Name,
    SceneType Type,
    Guid ChapterId,
    int OrderIndex,
    SceneContent Content,
    string? BackgroundPath,
    string? MusicPath,
    SceneTransition Transition,
    IReadOnlyList<Guid> NextScenes);

/// <summary>
/// Scene type.
/// </summary>
public enum SceneType
{
    Dialogue,
    Cutscene,
    Battle,
    Choice,
    Transition,
    Ending
}

/// <summary>
/// Scene content.
/// </summary>
public record SceneContent(
    IReadOnlyList<DialogueLine> Dialogue,
    IReadOnlyList<CutsceneElement> CutsceneElements,
    StoryBattle? Battle,
    IReadOnlyList<StoryChoice>? Choices);

/// <summary>
/// Scene transition.
/// </summary>
public record SceneTransition(
    TransitionType Type,
    TimeSpan Duration);

/// <summary>
/// Transition type.
/// </summary>
public enum StoryTransitionType
{
    Fade,
    Wipe,
    Dissolve,
    Slide,
    Instant
}

/// <summary>
/// Background settings.
/// </summary>
public record BackgroundSettings(
    BackgroundDisplayMode DisplayMode,
    Position2D? ScrollOffset,
    double ZoomLevel);

/// <summary>
/// Background display mode.
/// </summary>
public enum BackgroundDisplayMode
{
    Static,
    Scrolling,
    Parallax,
    Animated
}

/// <summary>
/// Music settings.
/// </summary>
public record MusicSettings(
    double Volume,
    bool Loop,
    TimeSpan? LoopStart,
    TimeSpan? LoopEnd,
    double FadeInDuration,
    double FadeOutDuration);

/// <summary>
/// Dialogue line.
/// </summary>
public record DialogueLine(
    Guid Id,
    Guid? SpeakerId,
    string Text,
    string? VoicePath,
    TextDisplaySettings TextSettings,
    SpeakerPosition SpeakerPosition);

/// <summary>
/// Text display settings.
/// </summary>
public record TextDisplaySettings(
    string? FontName,
    int FontSize,
    RgbColor TextColor,
    RgbColor? OutlineColor,
    TextAlignment Alignment,
    TextAnimation Animation);

/// <summary>
/// Text alignment.
/// </summary>
public enum TextAlignment
{
    Left,
    Center,
    Right
}

/// <summary>
/// Text animation.
/// </summary>
public enum TextAnimation
{
    None,
    Typewriter,
    Fade,
    Scroll
}

/// <summary>
/// Speaker position.
/// </summary>
public enum SpeakerPosition
{
    Left,
    Center,
    Right,
    Offscreen
}

/// <summary>
/// Cutscene element.
/// </summary>
public record CutsceneElement(
    Guid Id,
    CutsceneElementType Type,
    TimeSpan StartTime,
    TimeSpan Duration,
    IReadOnlyDictionary<string, object> Properties);

/// <summary>
/// Cutscene element type.
/// </summary>
public enum CutsceneElementType
{
    CharacterEnter,
    CharacterExit,
    CharacterMove,
    CharacterAnimate,
    CameraMove,
    Effect,
    Sound,
    ScreenShake,
    Flash
}

/// <summary>
/// Camera path.
/// </summary>
public record CameraPath(
    Position3D StartPosition,
    Position3D EndPosition,
    CameraMovementType MovementType,
    TimeSpan Duration);

/// <summary>
/// Camera movement type.
/// </summary>
public enum CameraMovementType
{
    Linear,
    Smooth,
    Jump
}

/// <summary>
/// 3D position.
/// </summary>
public record Position3D(double X, double Y, double Z);

/// <summary>
/// 2D position.
/// </summary>
public record Position2D(double X, double Y);

/// <summary>
/// RGB color.
/// </summary>
public record StoryRgbColor(byte R, byte G, byte B);

/// <summary>
/// Visual effect.
/// </summary>
public record VisualEffect(
    StoryEffectType Type,
    TimeSpan StartTime,
    TimeSpan Duration,
    IReadOnlyDictionary<string, object> Parameters);

/// <summary>
/// Effect type.
/// </summary>
public enum StoryEffectType
{
    Fade,
    Flash,
    Shake,
    Blur,
    Particle,
    Lighting
}

/// <summary>
/// Animation settings.
/// </summary>
public record AnimationSettings(
    bool Loop,
    double Speed,
    TransitionType Transition);

/// <summary>
/// Story choice.
/// </summary>
public record StoryChoice(
    Guid Id,
    string Text,
    string? Description,
    Guid? NextSceneId,
    ChoiceConsequences Consequences);

/// <summary>
/// Choice consequences.
/// </summary>
public record ChoiceConsequences(
    IReadOnlyDictionary<string, object> VariableChanges,
    IReadOnlyList<string> UnlockedContent,
    IReadOnlyList<string> LockedContent);

/// <summary>
/// Branch condition.
/// </summary>
public record BranchCondition(
    ConditionType Type,
    string VariableName,
    object? ExpectedValue,
    Guid? TargetSceneId);

/// <summary>
/// Condition type.
/// </summary>
public enum ConditionType
{
    VariableEquals,
    VariableGreater,
    VariableLess,
    FlagSet,
    ChapterCompleted
}

/// <summary>
/// Story character.
/// </summary>
public record StoryCharacter(
    Guid Id,
    Guid CharacterId,
    string CharacterName,
    CharacterAppearance Appearance,
    StoryAiSettings? AiSettings,
    IReadOnlyDictionary<string, object> StoryVariables);

/// <summary>
/// Character appearance.
/// </summary>
public record CharacterAppearance(
    int PaletteIndex,
    string? OutfitName,
    IReadOnlyDictionary<string, object> CustomAttributes);

/// <summary>
/// Story AI settings.
/// </summary>
public record StoryAiSettings(
    StoryAiDifficulty Difficulty,
    int Aggressiveness,
    IReadOnlyList<string> PreferredMoves,
    IReadOnlyList<string> BannedMoves);

/// <summary>
/// AI difficulty.
/// </summary>
public enum StoryAiDifficulty
{
    VeryEasy,
    Easy,
    Normal,
    Hard,
    VeryHard,
    Boss
}

/// <summary>
/// Casting options.
/// </summary>
public record CastingOptions(
    CharacterAppearance DefaultAppearance,
    StoryAiDifficulty DefaultDifficulty,
    bool IsPlayable);

/// <summary>
/// Story battle.
/// </summary>
public record StoryBattle(
    Guid Id,
    string? Title,
    Guid PlayerCharacterId,
    IReadOnlyList<Guid> OpponentIds,
    BattleConditions Conditions,
    Guid? PostWinSceneId,
    Guid? PostLoseSceneId);

/// <summary>
/// Battle conditions.
/// </summary>
public record BattleConditions(
    int Rounds,
    int TimeLimit,
    bool InfiniteTime,
    double PlayerHealthModifier,
    double OpponentHealthModifier,
    IReadOnlyList<SpecialRule> SpecialRules);

/// <summary>
/// Special rule.
/// </summary>
public record SpecialRule(
    string Name,
    string Description,
    RuleEffect Effect);

/// <summary>
/// Rule effect.
/// </summary>
public enum RuleEffect
{
    None,
    PlayerPowerUp,
    OpponentPowerUp,
    NoBlocking,
    NoSpecials,
    TimeLimitExtended
}

/// <summary>
/// Boss battle settings.
/// </summary>
public record BossBattleSettings(
    int PhaseCount,
    IReadOnlyList<BossPhase> Phases,
    string? PreBattleDialogue,
    string? VictoryDialogue,
    string? DefeatDialogue);

/// <summary>
/// Boss phase.
/// </summary>
public record BossPhase(
    int PhaseNumber,
    double HealthThreshold,
    AiDifficulty Difficulty,
    IReadOnlyList<string> NewMoves,
    string? Dialogue);

/// <summary>
/// Scene preview.
/// </summary>
public record ScenePreview(
    Guid SceneId,
    byte[] PreviewImage,
    TimeSpan EstimatedDuration,
    IReadOnlyList<string> Warnings);

/// <summary>
/// Preview options.
/// </summary>
public record StoryPreviewOptions(
    bool IncludeDialogue,
    bool IncludeEffects,
    bool IncludeTransitions);

/// <summary>
/// Story path simulation.
/// </summary>
public record StoryPathSimulation(
    IReadOnlyList<SimulatedScene> Scenes,
    IReadOnlyDictionary<string, object> FinalVariables,
    TimeSpan TotalDuration,
    string? EndingId);

/// <summary>
/// Simulated scene.
/// </summary>
public record SimulatedScene(
    Guid SceneId,
    string Name,
    DateTime Timestamp,
    int ChoiceMade);

/// <summary>
/// Story test result.
/// </summary>
public record StoryTestResult(
    bool IsValid,
    int ScenesTested,
    int BattlesTested,
    int Warnings,
    IReadOnlyList<StoryTestIssue> Issues);

/// <summary>
/// Story test issue.
/// </summary>
public record StoryTestIssue(
    IssueType Type,
    string Message,
    Guid? RelatedElementId);

/// <summary>
/// Issue type.
/// </summary>
public enum IssueType
{
    Error,
    Warning,
    Info
}

/// <summary>
/// Story asset.
/// </summary>
public record StoryAsset(
    Guid Id,
    string Name,
    AssetType Type,
    string FilePath,
    long FileSize,
    DateTime ImportedAt);

/// <summary>
/// Asset type.
/// </summary>
public enum AssetType
{
    Background,
    Music,
    Sound,
    Voice,
    CharacterSprite,
    Effect,
    Font
}

/// <summary>
/// Asset validation result.
/// </summary>
public record AssetValidationResult(
    bool IsValid,
    int MissingAssets,
    int InvalidReferences,
    IReadOnlyList<string> Issues);

/// <summary>
/// Story asset optimization result.
/// </summary>
public record StoryAssetOptimizationResult(
    int AssetsOptimized,
    long BytesSaved,
    IReadOnlyList<string> Warnings);

/// <summary>
/// Story asset optimization options.
/// </summary>
public record StoryAssetOptimizationOptions(
    bool CompressImages,
    bool ConvertAudio,
    int TargetAudioBitrate);

/// <summary>
/// MUGEN story export options.
/// </summary>
public record MugenStoryExportOptions(
    bool IncludeAssets,
    bool MinifyScripts,
    string TargetMugenVersion);

/// <summary>
/// Story project stats.
/// </summary>
public record StoryProjectStats(
    int TotalChapters,
    int TotalScenes,
    int TotalDialogueLines,
    int TotalBattles,
    int TotalChoices,
    int TotalCharacters,
    long TotalAssetSize,
    TimeSpan EstimatedPlayTime);

/// <summary>
/// Branch validation result.
/// </summary>
public record BranchValidationResult(
    bool IsValid,
    int OrphanedBranches,
    int DeadEnds,
    int CircularReferences,
    IReadOnlyList<string> Issues);

#endregion
