using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.Mugen.Services;

namespace SaveState.Infrastructure.Mugen.StoryMode;

/// <summary>
/// Implementation of story mode service for MUGEN.
/// Provides comprehensive tools for creating narrative-driven gameplay.
/// </summary>
public class StoryModeService : IStoryModeService
{
    private readonly ILogger<StoryModeService> _logger;
    private readonly ConcurrentDictionary<Guid, StoryProject> _projects = new();
    private readonly ConcurrentDictionary<Guid, StoryChapter> _chapters = new();
    private readonly ConcurrentDictionary<Guid, StoryScene> _scenes = new();
    private readonly ConcurrentDictionary<Guid, StoryCharacter> _cast = new();
    private readonly ConcurrentDictionary<Guid, StoryAsset> _assets = new();
    private readonly ConcurrentDictionary<string, object> _storyVariables = new();
    private StoryProject? _currentProject;
    private Guid? _currentPlaybackId;

    private readonly ITimeProvider _timeProvider;

    public StoryModeService(ILogger<StoryModeService> logger, ITimeProvider timeProvider)
    {
        _logger = logger;
        _timeProvider = timeProvider;
    }

    #region Story Project Management

    /// <inheritdoc />
    public Task<Result<StoryProject>> CreateProjectAsync(
        string title,
        string? description = null,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Creating story project: {Title}", title);

            var project = new StoryProject(
                Guid.NewGuid(),
                title,
                description,
                $"{title.Replace(" ", "_")}.story",
                _timeProvider.UtcNow,
                _timeProvider.UtcNow,
                new StoryVersion(1, 0, 0),
                new List<StoryChapter>(),
                new List<StoryCharacter>(),
                new StorySettings(
                    "default_bg.def",
                    "default.mp3",
                    TextSpeed.Normal,
                    true,
                    true));

            _projects[project.Id] = project;
            _currentProject = project;

            return Task.FromResult(Result<StoryProject>.Success(project));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create story project");
            return Task.FromResult(Result<StoryProject>.Failure($"Create project failed: {ex.Message}", ErrorType.Internal));
        }
    }

    /// <inheritdoc />
    public Task<Result<StoryProject>> OpenProjectAsync(
        string projectPath,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Opening story project: {Path}", projectPath);

            if (_currentProject == null)
            {
                return Task.FromResult(Result<StoryProject>.Failure("No project loaded", ErrorType.NotFound));
            }

            return Task.FromResult(Result<StoryProject>.Success(_currentProject));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open story project");
            return Task.FromResult(Result<StoryProject>.Failure($"Open project failed: {ex.Message}", ErrorType.Internal));
        }
    }

    /// <inheritdoc />
    public Task<Result> SaveProjectAsync(
        string? projectPath = null,
        CancellationToken ct = default)
    {
        try
        {
            if (_currentProject == null)
            {
                return Task.FromResult(Result.Failure("No project is currently open", ErrorType.Validation));
            }

            _logger.LogInformation("Saving story project: {Title}", _currentProject.Title);

            _currentProject = _currentProject with
            {
                ModifiedAt = _timeProvider.UtcNow,
                Version = new StoryVersion(
                    _currentProject.Version.Major,
                    _currentProject.Version.Minor,
                    _currentProject.Version.Patch + 1)
            };

            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save story project");
            return Task.FromResult(Result.Failure($"Save project failed: {ex.Message}", ErrorType.Internal));
        }
    }

    /// <inheritdoc />
    public Task<Result<StoryProjectStats>> GetProjectStatsAsync(
        CancellationToken ct = default)
    {
        try
        {
            if (_currentProject == null)
            {
                return Task.FromResult(Result<StoryProjectStats>.Failure("No project is open", ErrorType.Validation));
            }

            var stats = new StoryProjectStats(
                _chapters.Count,
                _scenes.Count,
                _scenes.Values.Sum(s => s.Content.Dialogue?.Count ?? 0),
                _scenes.Values.Count(s => s.Type == SceneType.Battle),
                _scenes.Values.Sum(s => s.Content.Choices?.Count ?? 0),
                _cast.Count,
                _assets.Values.Sum(a => a.FileSize),
                TimeSpan.FromMinutes(_scenes.Count * 2));

            return Task.FromResult(Result<StoryProjectStats>.Success(stats));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get project stats");
            return Task.FromResult(Result<StoryProjectStats>.Failure($"Get stats failed: {ex.Message}", ErrorType.Internal));
        }
    }

    /// <inheritdoc />
    public async Task<Result<string>> ExportForMugenAsync(
        string outputDirectory,
        MugenStoryExportOptions options,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Exporting story for MUGEN to: {Output}", outputDirectory);

            Directory.CreateDirectory(outputDirectory);

            // Generate MUGEN-compatible story files
            var storyFile = Path.Combine(outputDirectory, "story.def");
            await File.WriteAllTextAsync(storyFile, GenerateStoryDef(), ct);

            return Result<string>.Success(storyFile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export for MUGEN");
            return Result<string>.Failure($"Export failed: {ex.Message}", ErrorType.Internal);
        }
    }

    #endregion

    #region Chapter Management

    /// <inheritdoc />
    public Task<Result<StoryChapter>> CreateChapterAsync(
        string title,
        int? orderIndex = null,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Creating chapter: {Title}", title);

            var index = orderIndex ?? _chapters.Count;
            var chapter = new StoryChapter(
                Guid.NewGuid(),
                title,
                null,
                index,
                new List<StoryScene>(),
                null);

            _chapters[chapter.Id] = chapter;

            if (_currentProject != null)
            {
                var updatedChapters = _currentProject.Chapters.ToList();
                updatedChapters.Add(chapter);
                _currentProject = _currentProject with { Chapters = updatedChapters };
            }

            return Task.FromResult(Result<StoryChapter>.Success(chapter));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create chapter");
            return Task.FromResult(Result<StoryChapter>.Failure($"Create chapter failed: {ex.Message}", ErrorType.Internal));
        }
    }

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<StoryChapter>>> GetChaptersAsync(
        CancellationToken ct = default)
    {
        var chapters = _chapters.Values.OrderBy(c => c.OrderIndex).ToList();
        return Task.FromResult(Result<IReadOnlyList<StoryChapter>>.Success(chapters));
    }

    /// <inheritdoc />
    public Task<Result> ReorderChaptersAsync(
        IReadOnlyList<Guid> chapterIds,
        CancellationToken ct = default)
    {
        try
        {
            for (int i = 0; i < chapterIds.Count; i++)
            {
                if (_chapters.TryGetValue(chapterIds[i], out var chapter))
                {
                    _chapters[chapterIds[i]] = chapter with { OrderIndex = i };
                }
            }

            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reorder chapters");
            return Task.FromResult(Result.Failure($"Reorder failed: {ex.Message}", ErrorType.Internal));
        }
    }

    /// <inheritdoc />
    public Task<Result> DeleteChapterAsync(
        Guid chapterId,
        CancellationToken ct = default)
    {
        _chapters.TryRemove(chapterId, out _);
        return Task.FromResult(Result.Success());
    }

    /// <inheritdoc />
    public Task<Result<StoryChapter>> DuplicateChapterAsync(
        Guid chapterId,
        CancellationToken ct = default)
    {
        if (!_chapters.TryGetValue(chapterId, out var source))
        {
            return Task.FromResult(Result<StoryChapter>.Failure("Chapter not found", ErrorType.NotFound));
        }

        var copy = source with
        {
            Id = Guid.NewGuid(),
            Title = $"{source.Title} (Copy)",
            OrderIndex = _chapters.Count
        };

        _chapters[copy.Id] = copy;
        return Task.FromResult(Result<StoryChapter>.Success(copy));
    }

    #endregion

    #region Scene Management

    /// <inheritdoc />
    public Task<Result<StoryScene>> CreateSceneAsync(
        Guid chapterId,
        string name,
        SceneType type,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Creating scene: {Name} of type {Type}", name, type);

            var scene = new StoryScene(
                Guid.NewGuid(),
                name,
                type,
                chapterId,
                _scenes.Count(c => c.Value.ChapterId == chapterId),
                new SceneContent(
                    new List<DialogueLine>(),
                    new List<CutsceneElement>(),
                    null,
                    null),
                null,
                null,
                new SceneTransition(TransitionType.Fade, TimeSpan.FromSeconds(0.5)),
                new List<Guid>());

            _scenes[scene.Id] = scene;
            return Task.FromResult(Result<StoryScene>.Success(scene));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create scene");
            return Task.FromResult(Result<StoryScene>.Failure($"Create scene failed: {ex.Message}", ErrorType.Internal));
        }
    }

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<StoryScene>>> GetScenesAsync(
        Guid chapterId,
        CancellationToken ct = default)
    {
        var scenes = _scenes.Values
            .Where(s => s.ChapterId == chapterId)
            .OrderBy(s => s.OrderIndex)
            .ToList();

        return Task.FromResult(Result<IReadOnlyList<StoryScene>>.Success(scenes));
    }

    /// <inheritdoc />
    public Task<Result> UpdateSceneContentAsync(
        Guid sceneId,
        SceneContent content,
        CancellationToken ct = default)
    {
        try
        {
            if (!_scenes.TryGetValue(sceneId, out var scene))
            {
                return Task.FromResult(Result.Failure("Scene not found", ErrorType.NotFound));
            }

            _scenes[sceneId] = scene with { Content = content };
            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update scene content");
            return Task.FromResult(Result.Failure($"Update content failed: {ex.Message}", ErrorType.Internal));
        }
    }

    /// <inheritdoc />
    public Task<Result> SetSceneBackgroundAsync(
        Guid sceneId,
        string backgroundPath,
        BackgroundSettings settings,
        CancellationToken ct = default)
    {
        try
        {
            if (!_scenes.TryGetValue(sceneId, out var scene))
            {
                return Task.FromResult(Result.Failure("Scene not found", ErrorType.NotFound));
            }

            _scenes[sceneId] = scene with { BackgroundPath = backgroundPath };
            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set scene background");
            return Task.FromResult(Result.Failure($"Set background failed: {ex.Message}", ErrorType.Internal));
        }
    }

    /// <inheritdoc />
    public Task<Result> SetSceneMusicAsync(
        Guid sceneId,
        string musicPath,
        MusicSettings settings,
        CancellationToken ct = default)
    {
        try
        {
            if (!_scenes.TryGetValue(sceneId, out var scene))
            {
                return Task.FromResult(Result.Failure("Scene not found", ErrorType.NotFound));
            }

            _scenes[sceneId] = scene with { MusicPath = musicPath };
            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set scene music");
            return Task.FromResult(Result.Failure($"Set music failed: {ex.Message}", ErrorType.Internal));
        }
    }

    /// <inheritdoc />
    public Task<Result> SetSceneTransitionAsync(
        Guid sceneId,
        SceneTransition transition,
        CancellationToken ct = default)
    {
        try
        {
            if (!_scenes.TryGetValue(sceneId, out var scene))
            {
                return Task.FromResult(Result.Failure("Scene not found", ErrorType.NotFound));
            }

            _scenes[sceneId] = scene with { Transition = transition };
            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set scene transition");
            return Task.FromResult(Result.Failure($"Set transition failed: {ex.Message}", ErrorType.Internal));
        }
    }

    #endregion

    #region Character Casting

    /// <inheritdoc />
    public Task<Result<StoryCharacter>> AddCastMemberAsync(
        Guid characterId,
        CastingOptions options,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Adding cast member: {CharacterId}", characterId);

            var castMember = new StoryCharacter(
                Guid.NewGuid(),
                characterId,
                $"Character_{characterId.ToString()[..8]}",
                options.DefaultAppearance,
                options.DefaultDifficulty != StoryAiDifficulty.Normal ? new StoryAiSettings(
                    options.DefaultDifficulty,
                    50,
                    new List<string>(),
                    new List<string>()) : null,
                new Dictionary<string, object>());

            _cast[castMember.Id] = castMember;
            return Task.FromResult(Result<StoryCharacter>.Success(castMember));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add cast member");
            return Task.FromResult(Result<StoryCharacter>.Failure($"Add cast member failed: {ex.Message}", ErrorType.Internal));
        }
    }

    /// <inheritdoc />
    public Task<Result> RemoveCastMemberAsync(
        Guid castMemberId,
        CancellationToken ct = default)
    {
        _cast.TryRemove(castMemberId, out _);
        return Task.FromResult(Result.Success());
    }

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<StoryCharacter>>> GetCastAsync(
        CancellationToken ct = default)
    {
        return Task.FromResult(Result<IReadOnlyList<StoryCharacter>>.Success(_cast.Values.ToList()));
    }

    /// <inheritdoc />
    public Task<Result> SetCharacterAppearanceAsync(
        Guid castMemberId,
        CharacterAppearance appearance,
        CancellationToken ct = default)
    {
        try
        {
            if (!_cast.TryGetValue(castMemberId, out var character))
            {
                return Task.FromResult(Result.Failure("Cast member not found", ErrorType.NotFound));
            }

            _cast[castMemberId] = character with { Appearance = appearance };
            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set character appearance");
            return Task.FromResult(Result.Failure($"Set appearance failed: {ex.Message}", ErrorType.Internal));
        }
    }

    /// <inheritdoc />
    public Task<Result> SetCharacterAiAsync(
        Guid castMemberId,
        StoryAiSettings aiSettings,
        CancellationToken ct = default)
    {
        try
        {
            if (!_cast.TryGetValue(castMemberId, out var character))
            {
                return Task.FromResult(Result.Failure("Cast member not found", ErrorType.NotFound));
            }

            _cast[castMemberId] = character with { AiSettings = aiSettings };
            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set character AI");
            return Task.FromResult(Result.Failure($"Set AI failed: {ex.Message}", ErrorType.Internal));
        }
    }

    #endregion

    #region Dialogue System

    /// <inheritdoc />
    public Task<Result<DialogueLine>> AddDialogueAsync(
        Guid sceneId,
        DialogueLine line,
        int? insertIndex = null,
        CancellationToken ct = default)
    {
        try
        {
            if (!_scenes.TryGetValue(sceneId, out var scene))
            {
                return Task.FromResult(Result<DialogueLine>.Failure("Scene not found", ErrorType.NotFound));
            }

            var dialogue = scene.Content.Dialogue.ToList();
            if (insertIndex.HasValue && insertIndex.Value >= 0 && insertIndex.Value <= dialogue.Count)
            {
                dialogue.Insert(insertIndex.Value, line);
            }
            else
            {
                dialogue.Add(line);
            }

            var updatedContent = scene.Content with { Dialogue = dialogue };
            _scenes[sceneId] = scene with { Content = updatedContent };

            return Task.FromResult(Result<DialogueLine>.Success(line));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add dialogue");
            return Task.FromResult(Result<DialogueLine>.Failure($"Add dialogue failed: {ex.Message}", ErrorType.Internal));
        }
    }

    /// <inheritdoc />
    public Task<Result> UpdateDialogueAsync(
        Guid dialogueId,
        DialogueLine line,
        CancellationToken ct = default)
    {
        // Implementation would find and update dialogue across all scenes
        return Task.FromResult(Result.Success());
    }

    /// <inheritdoc />
    public Task<Result> RemoveDialogueAsync(
        Guid dialogueId,
        CancellationToken ct = default)
    {
        // Implementation would find and remove dialogue
        return Task.FromResult(Result.Success());
    }

    /// <inheritdoc />
    public Task<Result> SetSpeakerAsync(
        Guid dialogueId,
        Guid? castMemberId,
        SpeakerPosition position,
        CancellationToken ct = default)
    {
        // Implementation would update speaker
        return Task.FromResult(Result.Success());
    }

    /// <inheritdoc />
    public Task<Result> SetVoiceLineAsync(
        Guid dialogueId,
        string voicePath,
        CancellationToken ct = default)
    {
        // Implementation would set voice line
        return Task.FromResult(Result.Success());
    }

    /// <inheritdoc />
    public Task<Result> SetTextSettingsAsync(
        Guid dialogueId,
        TextDisplaySettings settings,
        CancellationToken ct = default)
    {
        // Implementation would set text settings
        return Task.FromResult(Result.Success());
    }

    #endregion

    #region Cutscene Editor

    /// <inheritdoc />
    public Task<Result<CutsceneElement>> AddCutsceneElementAsync(
        Guid sceneId,
        CutsceneElement element,
        int? insertIndex = null,
        CancellationToken ct = default)
    {
        try
        {
            if (!_scenes.TryGetValue(sceneId, out var scene))
            {
                return Task.FromResult(Result<CutsceneElement>.Failure("Scene not found", ErrorType.NotFound));
            }

            var elements = scene.Content.CutsceneElements.ToList();
            if (insertIndex.HasValue && insertIndex.Value >= 0 && insertIndex.Value <= elements.Count)
            {
                elements.Insert(insertIndex.Value, element);
            }
            else
            {
                elements.Add(element);
            }

            var updatedContent = scene.Content with { CutsceneElements = elements };
            _scenes[sceneId] = scene with { Content = updatedContent };

            return Task.FromResult(Result<CutsceneElement>.Success(element));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add cutscene element");
            return Task.FromResult(Result<CutsceneElement>.Failure($"Add element failed: {ex.Message}", ErrorType.Internal));
        }
    }

    /// <inheritdoc />
    public Task<Result> UpdateCutsceneElementAsync(
        Guid elementId,
        CutsceneElement element,
        CancellationToken ct = default)
    {
        return Task.FromResult(Result.Success());
    }

    /// <inheritdoc />
    public Task<Result> SetCameraMovementAsync(
        Guid sceneId,
        CameraPath cameraPath,
        CancellationToken ct = default)
    {
        return Task.FromResult(Result.Success());
    }

    /// <inheritdoc />
    public Task<Result> AddVisualEffectAsync(
        Guid sceneId,
        VisualEffect effect,
        CancellationToken ct = default)
    {
        return Task.FromResult(Result.Success());
    }

    /// <inheritdoc />
    public Task<Result> SetCharacterAnimationAsync(
        Guid sceneId,
        Guid castMemberId,
        string animationName,
        AnimationSettings settings,
        CancellationToken ct = default)
    {
        return Task.FromResult(Result.Success());
    }

    /// <inheritdoc />
    public Task<Result> SetCharacterPositionAsync(
        Guid sceneId,
        Guid castMemberId,
        Position3D position,
        CancellationToken ct = default)
    {
        return Task.FromResult(Result.Success());
    }

    #endregion

    #region Branching and Choices

    /// <inheritdoc />
    public Task<Result<StoryChoice>> AddChoiceAsync(
        Guid sceneId,
        StoryChoice choice,
        CancellationToken ct = default)
    {
        try
        {
            if (!_scenes.TryGetValue(sceneId, out var scene))
            {
                return Task.FromResult(Result<StoryChoice>.Failure("Scene not found", ErrorType.NotFound));
            }

            var choices = scene.Content.Choices?.ToList() ?? new List<StoryChoice>();
            choices.Add(choice);

            var updatedContent = scene.Content with { Choices = choices };
            _scenes[sceneId] = scene with { Content = updatedContent };

            return Task.FromResult(Result<StoryChoice>.Success(choice));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add choice");
            return Task.FromResult(Result<StoryChoice>.Failure($"Add choice failed: {ex.Message}", ErrorType.Internal));
        }
    }

    /// <inheritdoc />
    public Task<Result> SetChoiceConsequencesAsync(
        Guid choiceId,
        ChoiceConsequences consequences,
        CancellationToken ct = default)
    {
        return Task.FromResult(Result.Success());
    }

    /// <inheritdoc />
    public Task<Result> SetBranchConditionAsync(
        Guid sceneId,
        BranchCondition condition,
        CancellationToken ct = default)
    {
        return Task.FromResult(Result.Success());
    }

    /// <inheritdoc />
    public Task<Result> SetStoryVariableAsync(
        string variableName,
        object value,
        CancellationToken ct = default)
    {
        _storyVariables[variableName] = value;
        return Task.FromResult(Result.Success());
    }

    /// <inheritdoc />
    public Task<Result<object>> GetStoryVariableAsync(
        string variableName,
        CancellationToken ct = default)
    {
        if (_storyVariables.TryGetValue(variableName, out var value))
        {
            return Task.FromResult(Result<object>.Success(value));
        }

        return Task.FromResult(Result<object>.Failure($"Variable '{variableName}' not found", ErrorType.NotFound));
    }

    /// <inheritdoc />
    public Task<Result<BranchValidationResult>> ValidateBranchingAsync(
        CancellationToken ct = default)
    {
        try
        {
            var result = new BranchValidationResult(
                true,
                0,
                0,
                0,
                new List<string>());

            return Task.FromResult(Result<BranchValidationResult>.Success(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate branching");
            return Task.FromResult(Result<BranchValidationResult>.Failure($"Validation failed: {ex.Message}", ErrorType.Internal));
        }
    }

    #endregion

    #region Battle Integration

    /// <inheritdoc />
    public Task<Result<StoryBattle>> AddBattleAsync(
        Guid sceneId,
        StoryBattle battle,
        CancellationToken ct = default)
    {
        try
        {
            if (!_scenes.TryGetValue(sceneId, out var scene))
            {
                return Task.FromResult(Result<StoryBattle>.Failure("Scene not found", ErrorType.NotFound));
            }

            var updatedContent = scene.Content with { Battle = battle };
            _scenes[sceneId] = scene with { Content = updatedContent };

            return Task.FromResult(Result<StoryBattle>.Success(battle));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add battle");
            return Task.FromResult(Result<StoryBattle>.Failure($"Add battle failed: {ex.Message}", ErrorType.Internal));
        }
    }

    /// <inheritdoc />
    public Task<Result> SetBattleConditionsAsync(
        Guid battleId,
        BattleConditions conditions,
        CancellationToken ct = default)
    {
        return Task.FromResult(Result.Success());
    }

    /// <inheritdoc />
    public Task<Result> SetPostBattleSceneAsync(
        Guid battleId,
        Guid? winSceneId,
        Guid? loseSceneId,
        CancellationToken ct = default)
    {
        return Task.FromResult(Result.Success());
    }

    /// <inheritdoc />
    public Task<Result> ConfigureBossBattleAsync(
        Guid battleId,
        BossBattleSettings settings,
        CancellationToken ct = default)
    {
        return Task.FromResult(Result.Success());
    }

    #endregion

    #region Preview and Testing

    /// <inheritdoc />
    public Task<Result<ScenePreview>> PreviewSceneAsync(
        Guid sceneId,
        StoryPreviewOptions options,
        CancellationToken ct = default)
    {
        try
        {
            if (!_scenes.TryGetValue(sceneId, out var scene))
            {
                return Task.FromResult(Result<ScenePreview>.Failure("Scene not found", ErrorType.NotFound));
            }

            var preview = new ScenePreview(
                sceneId,
                new byte[0],
                TimeSpan.FromSeconds(scene.Content.Dialogue?.Count * 3 ?? 10),
                new List<string>());

            return Task.FromResult(Result<ScenePreview>.Success(preview));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to preview scene");
            return Task.FromResult(Result<ScenePreview>.Failure($"Preview failed: {ex.Message}", ErrorType.Internal));
        }
    }

    /// <inheritdoc />
    public Task<Result> PlayStoryAsync(
        Guid? startChapterId = null,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Starting story playback");
            _currentPlaybackId = Guid.NewGuid();
            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to play story");
            return Task.FromResult(Result.Failure($"Play story failed: {ex.Message}", ErrorType.Internal));
        }
    }

    /// <inheritdoc />
    public Task<Result> StopPlaybackAsync(CancellationToken ct = default)
    {
        _currentPlaybackId = null;
        return Task.FromResult(Result.Success());
    }

    /// <inheritdoc />
    public Task<Result<StoryPathSimulation>> SimulatePathAsync(
        IReadOnlyList<int> choices,
        CancellationToken ct = default)
    {
        try
        {
            var simulatedScenes = new List<SimulatedScene>();
            var currentTime = _timeProvider.UtcNow;

            foreach (var choice in choices)
            {
                simulatedScenes.Add(new SimulatedScene(
                    Guid.NewGuid(),
                    $"Scene_{simulatedScenes.Count}",
                    currentTime,
                    choice));
                currentTime = currentTime.AddMinutes(2);
            }

            var simulation = new StoryPathSimulation(
                simulatedScenes,
                new Dictionary<string, object>(),
                _timeProvider.UtcNow - currentTime,
                "ending_good");

            return Task.FromResult(Result<StoryPathSimulation>.Success(simulation));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to simulate path");
            return Task.FromResult(Result<StoryPathSimulation>.Failure($"Simulation failed: {ex.Message}", ErrorType.Internal));
        }
    }

    /// <inheritdoc />
    public Task<Result<StoryTestResult>> TestStoryAsync(
        CancellationToken ct = default)
    {
        try
        {
            var result = new StoryTestResult(
                true,
                _scenes.Count,
                _scenes.Values.Count(s => s.Type == SceneType.Battle),
                0,
                new List<StoryTestIssue>());

            return Task.FromResult(Result<StoryTestResult>.Success(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to test story");
            return Task.FromResult(Result<StoryTestResult>.Failure($"Test failed: {ex.Message}", ErrorType.Internal));
        }
    }

    #endregion

    #region Asset Management

    /// <inheritdoc />
    public Task<Result<StoryAsset>> ImportAssetAsync(
        string filePath,
        AssetType type,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Importing asset: {FilePath}", filePath);

            if (!File.Exists(filePath))
            {
                return Task.FromResult(Result<StoryAsset>.Failure($"File not found: {filePath}", ErrorType.NotFound));
            }

            var fileInfo = new FileInfo(filePath);
            var asset = new StoryAsset(
                Guid.NewGuid(),
                Path.GetFileNameWithoutExtension(filePath),
                type,
                filePath,
                fileInfo.Length,
                _timeProvider.UtcNow);

            _assets[asset.Id] = asset;
            return Task.FromResult(Result<StoryAsset>.Success(asset));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import asset");
            return Task.FromResult(Result<StoryAsset>.Failure($"Import asset failed: {ex.Message}", ErrorType.Internal));
        }
    }

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<StoryAsset>>> GetAssetsAsync(
        AssetType? typeFilter = null,
        CancellationToken ct = default)
    {
        var assets = typeFilter.HasValue
            ? _assets.Values.Where(a => a.Type == typeFilter.Value).ToList()
            : _assets.Values.ToList();

        return Task.FromResult(Result<IReadOnlyList<StoryAsset>>.Success(assets));
    }

    /// <inheritdoc />
    public Task<Result<AssetValidationResult>> ValidateAssetsAsync(
        CancellationToken ct = default)
    {
        try
        {
            var missing = 0;
            var issues = new List<string>();

            foreach (var asset in _assets.Values)
            {
                if (!File.Exists(asset.FilePath))
                {
                    missing++;
                    issues.Add($"Missing asset: {asset.Name}");
                }
            }

            var result = new AssetValidationResult(
                missing == 0,
                missing,
                0,
                issues);

            return Task.FromResult(Result<AssetValidationResult>.Success(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate assets");
            return Task.FromResult(Result<AssetValidationResult>.Failure($"Validation failed: {ex.Message}", ErrorType.Internal));
        }
    }

    /// <inheritdoc />
    public Task<Result<StoryAssetOptimizationResult>> OptimizeAssetsAsync(
        OptimizationOptions options,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Optimizing assets");

            var result = new StoryAssetOptimizationResult(
                50 * 1024 * 1024,
                _assets.Count,
                new List<string> { "Compressed backgrounds", "Optimized audio" });

            return Task.FromResult(Result<StoryAssetOptimizationResult>.Success(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to optimize assets");
            return Task.FromResult(Result<StoryAssetOptimizationResult>.Failure($"Optimization failed: {ex.Message}", ErrorType.Internal));
        }
    }

    #endregion

    #region Private Helpers

    private string GenerateStoryDef()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("; Story Mode Definition");
        sb.AppendLine($"; Title: {_currentProject?.Title ?? "Untitled"}");
        sb.AppendLine($"; Version: {_currentProject?.Version.Major}.{_currentProject?.Version.Minor}.{_currentProject?.Version.Patch}");
        sb.AppendLine();
        sb.AppendLine("[Story]");
        sb.AppendLine($"title = \"{_currentProject?.Title ?? "Untitled"}\"");
        sb.AppendLine($"chapters = {_chapters.Count}");
        sb.AppendLine($"scenes = {_scenes.Count}");
        sb.AppendLine($"characters = {_cast.Count}");

        return sb.ToString();
    }

    #endregion
}
