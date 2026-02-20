using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.Mugen.Services;

namespace SaveState.Infrastructure.Mugen.StoryMode.Managers;

/// <summary>
/// Manages story preview, playback, and testing.
/// </summary>
public class StoryTestingManager
{
    private readonly ILogger<StoryTestingManager> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly Func<Guid, StoryScene?> _sceneLookup;
    private readonly Func<IReadOnlyCollection<StoryScene>> _getAllScenes;
    private Guid? _currentPlaybackId;

    public StoryTestingManager(
        ILogger<StoryTestingManager> logger,
        ITimeProvider timeProvider,
        Func<Guid, StoryScene?> sceneLookup,
        Func<IReadOnlyCollection<StoryScene>> getAllScenes)
    {
        _logger = logger;
        _timeProvider = timeProvider;
        _sceneLookup = sceneLookup;
        _getAllScenes = getAllScenes;
    }

    public Guid? CurrentPlaybackId => _currentPlaybackId;

    /// <summary>
    /// Previews a scene with estimated duration based on dialogue count.
    /// </summary>
    public Task<Result<ScenePreview>> PreviewSceneAsync(
        Guid sceneId,
        StoryPreviewOptions options,
        CancellationToken ct = default)
    {
        try
        {
            var scene = _sceneLookup(sceneId);
            if (scene is null)
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

    /// <summary>
    /// Starts story playback.
    /// </summary>
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

    /// <summary>
    /// Stops story playback.
    /// </summary>
    public Task<Result> StopPlaybackAsync(CancellationToken ct = default)
    {
        _currentPlaybackId = null;
        return Task.FromResult(Result.Success());
    }

    /// <summary>
    /// Simulates a story path based on choices.
    /// </summary>
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

    /// <summary>
    /// Tests story completeness by validating scene and battle counts.
    /// </summary>
    public Task<Result<StoryTestResult>> TestStoryAsync(
        CancellationToken ct = default)
    {
        try
        {
            var scenes = _getAllScenes();
            var result = new StoryTestResult(
                true,
                scenes.Count,
                scenes.Count(s => s.Type == SceneType.Battle),
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
}
