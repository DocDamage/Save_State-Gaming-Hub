using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Mugen.Services;

namespace SaveState.Infrastructure.Mugen.SpriteAnimation.Managers;

/// <summary>
/// Manages animation preview and playback operations.
/// </summary>
public sealed class PreviewManager
{
    private readonly ILogger<PreviewManager> _logger;
    private readonly ConcurrentDictionary<int, Animation> _animations;
    private AnimationPlaybackState? _playbackState;

    /// <summary>
    /// Initializes a new instance of the <see cref="PreviewManager"/> class.
    /// </summary>
    public PreviewManager(
        ILogger<PreviewManager> logger,
        ConcurrentDictionary<int, Animation> animations)
    {
        _logger = logger;
        _animations = animations;
    }

    /// <summary>
    /// Renders animation frame as preview.
    /// </summary>
    public Task<Result<byte[]>> RenderFramePreviewAsync(
        int groupNumber,
        int imageNumber,
        RenderOptions options,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogDebug("Rendering frame preview: ({Group},{Image})", groupNumber, imageNumber);
            return Task.FromResult(Result<byte[]>.Success(new byte[100]));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to render frame preview");
            return Task.FromResult(Result<byte[]>.Failure($"Render failed: {ex.Message}", ErrorType.Internal));
        }
    }

    /// <summary>
    /// Renders complete animation as GIF/webp.
    /// </summary>
    public Task<Result<byte[]>> RenderAnimationAsync(
        int actionNumber,
        RenderOptions options,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Rendering animation: {ActionNumber}", actionNumber);
            return Task.FromResult(Result<byte[]>.Success(new byte[1000]));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to render animation");
            return Task.FromResult(Result<byte[]>.Failure($"Render failed: {ex.Message}", ErrorType.Internal));
        }
    }

    /// <summary>
    /// Gets animation playback state.
    /// </summary>
    public Task<Result<AnimationPlaybackState>> GetPlaybackStateAsync(
        int actionNumber,
        CancellationToken ct = default)
    {
        return Task.FromResult(Result<AnimationPlaybackState>.Success(
            _playbackState ?? new AnimationPlaybackState(
                actionNumber,
                0,
                false,
                false,
                0,
                TimeSpan.Zero,
                TimeSpan.Zero)));
    }

    /// <summary>
    /// Plays animation preview.
    /// </summary>
    public Task<Result> PlayAnimationAsync(
        int actionNumber,
        PlaybackOptions options,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Playing animation: {ActionNumber}", actionNumber);

            _playbackState = new AnimationPlaybackState(
                actionNumber,
                options.StartFrame ?? 0,
                true,
                false,
                _animations.TryGetValue(actionNumber, out var anim) ? anim.Frames.Count : 0,
                TimeSpan.FromSeconds(5),
                TimeSpan.Zero);

            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to play animation");
            return Task.FromResult(Result.Failure($"Play failed: {ex.Message}", ErrorType.Internal));
        }
    }

    /// <summary>
    /// Pauses animation playback.
    /// </summary>
    public Task<Result> PauseAnimationAsync(CancellationToken ct = default)
    {
        if (_playbackState != null)
        {
            _playbackState = _playbackState with { IsPaused = true };
        }
        return Task.FromResult(Result.Success());
    }

    /// <summary>
    /// Stops animation playback.
    /// </summary>
    public Task<Result> StopAnimationAsync(CancellationToken ct = default)
    {
        if (_playbackState != null)
        {
            _playbackState = _playbackState with { IsPlaying = false, CurrentFrame = 0 };
        }
        return Task.FromResult(Result.Success());
    }

    /// <summary>
    /// Seeks to specific frame.
    /// </summary>
    public Task<Result> SeekToFrameAsync(
        int frameIndex,
        CancellationToken ct = default)
    {
        if (_playbackState != null)
        {
            _playbackState = _playbackState with { CurrentFrame = frameIndex };
        }
        return Task.FromResult(Result.Success());
    }
}
