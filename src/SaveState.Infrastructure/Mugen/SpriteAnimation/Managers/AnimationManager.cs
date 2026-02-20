using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Mugen.Services;

namespace SaveState.Infrastructure.Mugen.SpriteAnimation.Managers;

/// <summary>
/// Manages animation operations including AIR file loading, saving, and animation editing.
/// </summary>
public sealed class AnimationManager
{
    private readonly ILogger<AnimationManager> _logger;
    private readonly ConcurrentDictionary<int, Animation> _animations;

    /// <summary>
    /// Initializes a new instance of the <see cref="AnimationManager"/> class.
    /// </summary>
    public AnimationManager(
        ILogger<AnimationManager> logger,
        ConcurrentDictionary<int, Animation> animations)
    {
        _logger = logger;
        _animations = animations;
    }

    /// <summary>
    /// Loads animations from an AIR file.
    /// </summary>
    public async Task<Result<AirFile>> LoadAirFileAsync(
        string filePath,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Loading AIR file: {FilePath}", filePath);

            if (!File.Exists(filePath))
            {
                return Result<AirFile>.Failure($"AIR file not found: {filePath}", ErrorType.NotFound);
            }

            var lines = await File.ReadAllLinesAsync(filePath, ct);
            var animations = ParseAirFile(lines);

            foreach (var animation in animations)
            {
                _animations[animation.ActionNumber] = animation;
            }

            var airFile = new AirFile(
                filePath,
                animations,
                new List<AnimationClsn>());

            return Result<AirFile>.Success(airFile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load AIR file: {FilePath}", filePath);
            return Result<AirFile>.Failure($"AIR load failed: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <summary>
    /// Saves animations to an AIR file.
    /// </summary>
    public async Task<Result> SaveAirFileAsync(
        string filePath,
        AirFile airFile,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Saving AIR file: {FilePath}", filePath);

            using var writer = new StreamWriter(filePath);

            foreach (var animation in airFile.Animations)
            {
                await WriteAnimationAsync(writer, animation, ct);
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save AIR file");
            return Result.Failure($"AIR save failed: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <summary>
    /// Creates a new animation.
    /// </summary>
    public Task<Result<Animation>> CreateAnimationAsync(
        int actionNumber,
        string name,
        SpriteAnimationType type,
        CancellationToken ct = default)
    {
        try
        {
            var animation = new Animation(
                actionNumber,
                name,
                type,
                new List<AnimationFrame>(),
                LoopType.NoLoop);

            _animations[actionNumber] = animation;
            return Task.FromResult(Result<Animation>.Success(animation));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create animation");
            return Task.FromResult(Result<Animation>.Failure($"Create animation failed: {ex.Message}", ErrorType.Internal));
        }
    }

    /// <summary>
    /// Adds a frame to an animation.
    /// </summary>
    public Task<Result> AddAnimationFrameAsync(
        int actionNumber,
        AnimationFrame frame,
        int? insertIndex = null,
        CancellationToken ct = default)
    {
        try
        {
            if (!_animations.TryGetValue(actionNumber, out var animation))
            {
                return Task.FromResult(Result.Failure($"Animation {actionNumber} not found", ErrorType.NotFound));
            }

            var frames = animation.Frames.ToList();
            if (insertIndex.HasValue && insertIndex.Value >= 0 && insertIndex.Value <= frames.Count)
            {
                frames.Insert(insertIndex.Value, frame);
            }
            else
            {
                frames.Add(frame);
            }

            _animations[actionNumber] = animation with { Frames = frames };
            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add animation frame");
            return Task.FromResult(Result.Failure($"Add frame failed: {ex.Message}", ErrorType.Internal));
        }
    }

    /// <summary>
    /// Removes a frame from an animation.
    /// </summary>
    public Task<Result> RemoveAnimationFrameAsync(
        int actionNumber,
        int frameIndex,
        CancellationToken ct = default)
    {
        try
        {
            if (!_animations.TryGetValue(actionNumber, out var animation))
            {
                return Task.FromResult(Result.Failure($"Animation {actionNumber} not found", ErrorType.NotFound));
            }

            var frames = animation.Frames.ToList();
            if (frameIndex < 0 || frameIndex >= frames.Count)
            {
                return Task.FromResult(Result.Failure($"Frame index {frameIndex} out of range", ErrorType.Validation));
            }

            frames.RemoveAt(frameIndex);
            _animations[actionNumber] = animation with { Frames = frames };
            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove animation frame");
            return Task.FromResult(Result.Failure($"Remove frame failed: {ex.Message}", ErrorType.Internal));
        }
    }

    /// <summary>
    /// Updates an animation frame.
    /// </summary>
    public Task<Result> UpdateAnimationFrameAsync(
        int actionNumber,
        int frameIndex,
        AnimationFrame frame,
        CancellationToken ct = default)
    {
        try
        {
            if (!_animations.TryGetValue(actionNumber, out var animation))
            {
                return Task.FromResult(Result.Failure($"Animation {actionNumber} not found", ErrorType.NotFound));
            }

            var frames = animation.Frames.ToList();
            if (frameIndex < 0 || frameIndex >= frames.Count)
            {
                return Task.FromResult(Result.Failure($"Frame index {frameIndex} out of range", ErrorType.Validation));
            }

            frames[frameIndex] = frame;
            _animations[actionNumber] = animation with { Frames = frames };
            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update animation frame");
            return Task.FromResult(Result.Failure($"Update frame failed: {ex.Message}", ErrorType.Internal));
        }
    }

    /// <summary>
    /// Gets all animations.
    /// </summary>
    public Task<Result<IReadOnlyList<Animation>>> GetAnimationsAsync(
        SpriteAnimationType? typeFilter = null,
        CancellationToken ct = default)
    {
        var animations = _animations.Values.AsEnumerable();

        if (typeFilter.HasValue)
        {
            animations = animations.Where(a => a.Type == typeFilter.Value);
        }

        return Task.FromResult(Result<IReadOnlyList<Animation>>.Success(animations.ToList()));
    }

    /// <summary>
    /// Gets a specific animation by action number.
    /// </summary>
    public Task<Result<Animation>> GetAnimationAsync(
        int actionNumber,
        CancellationToken ct = default)
    {
        if (_animations.TryGetValue(actionNumber, out var animation))
        {
            return Task.FromResult(Result<Animation>.Success(animation));
        }

        return Task.FromResult(Result<Animation>.Failure($"Animation {actionNumber} not found", ErrorType.NotFound));
    }

    /// <summary>
    /// Duplicates an animation.
    /// </summary>
    public Task<Result<Animation>> DuplicateAnimationAsync(
        int sourceActionNumber,
        int newActionNumber,
        CancellationToken ct = default)
    {
        try
        {
            if (!_animations.TryGetValue(sourceActionNumber, out var sourceAnimation))
            {
                return Task.FromResult(Result<Animation>.Failure($"Source animation {sourceActionNumber} not found", ErrorType.NotFound));
            }

            var newAnimation = sourceAnimation with
            {
                ActionNumber = newActionNumber,
                Name = $"{sourceAnimation.Name} (Copy)",
                Frames = sourceAnimation.Frames.ToList()
            };

            _animations[newActionNumber] = newAnimation;
            return Task.FromResult(Result<Animation>.Success(newAnimation));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to duplicate animation");
            return Task.FromResult(Result<Animation>.Failure($"Duplicate failed: {ex.Message}", ErrorType.Internal));
        }
    }

    /// <summary>
    /// Deletes an animation.
    /// </summary>
    public Task<Result> DeleteAnimationAsync(
        int actionNumber,
        CancellationToken ct = default)
    {
        _animations.TryRemove(actionNumber, out _);
        return Task.FromResult(Result.Success());
    }

    private IReadOnlyList<Animation> ParseAirFile(string[] lines)
    {
        var animations = new List<Animation>();
        Animation? currentAnimation = null;
        var frames = new List<AnimationFrame>();

        foreach (var line in lines)
        {
            var trimmed = line.Trim();

            if (trimmed.StartsWith("[Begin Action "))
            {
                if (currentAnimation != null)
                {
                    animations.Add(currentAnimation with { Frames = frames });
                }

                var actionNum = int.Parse(trimmed[14..^1]);
                currentAnimation = new Animation(actionNum, $"Action {actionNum}", SpriteAnimationType.Custom, Array.Empty<AnimationFrame>(), LoopType.NoLoop);
                frames = new List<AnimationFrame>();
            }
            else if (!string.IsNullOrEmpty(trimmed) && currentAnimation != null && char.IsDigit(trimmed[0]))
            {
                var parts = trimmed.Split(',').Select(p => p.Trim()).ToArray();
                if (parts.Length >= 5 &&
                    int.TryParse(parts[0], out int groupNum) &&
                    int.TryParse(parts[1], out int imageNum) &&
                    int.TryParse(parts[2], out int x) &&
                    int.TryParse(parts[3], out int y) &&
                    int.TryParse(parts[4], out int time))
                {
                    frames.Add(new AnimationFrame(groupNum, imageNum, x, y, time));
                }
            }
        }

        if (currentAnimation != null)
        {
            animations.Add(currentAnimation with { Frames = frames });
        }

        return animations;
    }

    private async Task WriteAnimationAsync(StreamWriter writer, Animation animation, CancellationToken ct)
    {
        await writer.WriteLineAsync($"[Begin Action {animation.ActionNumber}]");

        foreach (var frame in animation.Frames)
        {
            var line = $"{frame.GroupNumber},{frame.ImageNumber},{frame.X},{frame.Y},{frame.Time}";
            await writer.WriteLineAsync(line);
        }

        await writer.WriteLineAsync();
    }
}
