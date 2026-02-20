using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Mugen.Services;

namespace SaveState.Infrastructure.Mugen.StoryMode.Managers;

/// <summary>
/// Manages story scenes and their properties.
/// </summary>
public class StorySceneManager
{
    private readonly ILogger<StorySceneManager> _logger;
    private readonly ConcurrentDictionary<Guid, StoryScene> _scenes;

    public StorySceneManager(ILogger<StorySceneManager> logger)
    {
        _logger = logger;
        _scenes = new ConcurrentDictionary<Guid, StoryScene>();
    }

    public ConcurrentDictionary<Guid, StoryScene> Scenes => _scenes;

    /// <summary>
    /// Creates a new scene.
    /// </summary>
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

    /// <summary>
    /// Gets scenes in a chapter.
    /// </summary>
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

    /// <summary>
    /// Updates scene content.
    /// </summary>
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

    /// <summary>
    /// Sets scene background.
    /// </summary>
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

    /// <summary>
    /// Sets scene music.
    /// </summary>
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

    /// <summary>
    /// Configures scene transition.
    /// </summary>
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
}
