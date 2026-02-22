using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.Mugen.Services;
using Microsoft.Extensions.Logging;

namespace SaveState.Application.Mugen.Services;

/// <summary>
/// Camera transition engine - handles transitions and easing functions.
/// </summary>
public class CameraTransitionEngine
{
    private readonly ILogger<CameraTransitionEngine> _logger;
    private readonly Dictionary<string, CinematicCameraSystemCameraTransition> _transitions = new();

    public CameraTransitionEngine(ILogger<CameraTransitionEngine> logger)
    {
        _logger = logger;
    }

    public CinematicCameraSystemCameraTransition CreateTransition(CinematicCameraSystemCameraTransitionRequest request)
    {
        var transition = new CinematicCameraSystemCameraTransition
        {
            TransitionId = Guid.NewGuid().ToString(),
            TransitionType = request.TransitionType,
            EasingFunction = request.EasingFunction,
            Duration = request.Duration,
            StartPosition = request.StartPosition,
            EndPosition = request.EndPosition
        };
        _transitions[transition.TransitionId] = transition;
        _logger.LogDebug("Created transition {TransitionId}: {Type} over {Duration}",
            transition.TransitionId, transition.TransitionType, transition.Duration);
        return transition;
    }

    public async Task ExecuteTransitionAsync(
        CinematicCameraSystemCameraTransition transition,
        Func<CinematicCameraSystemCameraPosition, Task> applyPosition,
        CancellationToken ct)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        while (stopwatch.Elapsed < transition.Duration)
        {
            var t = (float)(stopwatch.Elapsed.TotalMilliseconds / transition.Duration.TotalMilliseconds);
            var easedT = ApplyEasing(t, transition.EasingFunction);

            var position = InterpolatePosition(transition.StartPosition, transition.EndPosition, easedT);
            await applyPosition(position);

            await Task.Delay(16, ct); // ~60fps
        }

        // Ensure final position
        await applyPosition(transition.EndPosition);
    }

    public float ApplyEasing(float t, CinematicCameraSystemEasingFunction easing)
    {
        t = Math.Clamp(t, 0, 1);

        return easing switch
        {
            CinematicCameraSystemEasingFunction.Linear => t,
            CinematicCameraSystemEasingFunction.EaseIn => t * t,
            CinematicCameraSystemEasingFunction.EaseOut => 1 - (1 - t) * (1 - t),
            CinematicCameraSystemEasingFunction.EaseInOut => t < 0.5f
                ? 2 * t * t
                : 1 - MathF.Pow(-2 * t + 2, 2) / 2,
            CinematicCameraSystemEasingFunction.Spring => SpringEasing(t),
            CinematicCameraSystemEasingFunction.Bounce => BounceEasing(t),
            _ => t
        };
    }

    private CinematicCameraSystemCameraPosition InterpolatePosition(
        CinematicCameraSystemCameraPosition start,
        CinematicCameraSystemCameraPosition end,
        float t)
    {
        return new CinematicCameraSystemCameraPosition
        {
            Position = new CinematicCameraSystemCameraVector3(
                Lerp(start.Position.X, end.Position.X, t),
                Lerp(start.Position.Y, end.Position.Y, t),
                Lerp(start.Position.Z, end.Position.Z, t)
            ),
            Rotation = new CinematicCameraSystemCameraVector3(
                LerpAngle(start.Rotation.X, end.Rotation.X, t),
                LerpAngle(start.Rotation.Y, end.Rotation.Y, t),
                LerpAngle(start.Rotation.Z, end.Rotation.Z, t)
            ),
            FieldOfView = Lerp(start.FieldOfView, end.FieldOfView, t)
        };
    }

    private float SpringEasing(float t)
    {
        // Simplified spring approximation
        return 1 - MathF.Exp(-5 * t) * MathF.Cos(10 * t);
    }

    private float BounceEasing(float t)
    {
        // Simplified bounce
        if (t < 1 / 2.75f)
            return 7.5625f * t * t;
        else if (t < 2 / 2.75f)
        {
            t -= 1.5f / 2.75f;
            return 7.5625f * t * t + 0.75f;
        }
        else if (t < 2.5f / 2.75f)
        {
            t -= 2.25f / 2.75f;
            return 7.5625f * t * t + 0.9375f;
        }
        else
        {
            t -= 2.625f / 2.75f;
            return 7.5625f * t * t + 0.984375f;
        }
    }

    private float Lerp(float a, float b, float t) => a + (b - a) * t;

    private float LerpAngle(float a, float b, float t)
    {
        var diff = b - a;
        while (diff > 180) diff -= 360;
        while (diff < -180) diff += 360;
        return a + diff * t;
    }
}
