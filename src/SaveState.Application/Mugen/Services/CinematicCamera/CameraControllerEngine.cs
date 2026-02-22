using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.Mugen.Services;
using Microsoft.Extensions.Logging;

namespace SaveState.Application.Mugen.Services;

/// <summary>
/// Camera controller engine - handles camera movement, positioning, and state management.
/// </summary>
public class CameraControllerEngine
{
    private readonly ILogger<CameraControllerEngine> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly Dictionary<string, CinematicCameraSystemCameraState> _cameraStates = new();

    public CameraControllerEngine(ILogger<CameraControllerEngine> logger, ITimeProvider timeProvider)
    {
        _logger = logger;
        _timeProvider = timeProvider;
    }

    public CinematicCameraSystemCameraState CreateCamera(string name, CinematicCameraSystemCameraPosition position)
    {
        var state = new CinematicCameraSystemCameraState
        {
            CameraId = Guid.NewGuid().ToString(),
            CurrentPosition = position,
            IsActive = true,
            LastUpdate = _timeProvider.UtcNow
        };
        _cameraStates[state.CameraId] = state;
        _logger.LogDebug("Created camera {CameraId} with name {Name}", state.CameraId, name);
        return state;
    }

    public void UpdatePosition(string cameraId, CinematicCameraSystemCameraPosition newPosition)
    {
        if (_cameraStates.TryGetValue(cameraId, out var state))
        {
            state.CurrentPosition = newPosition;
            state.LastUpdate = _timeProvider.UtcNow;
        }
    }

    public CinematicCameraSystemCameraPosition CalculateInterpolatedPosition(
        CinematicCameraSystemCameraPosition start,
        CinematicCameraSystemCameraPosition end,
        float t,
        CinematicCameraSystemEasingFunction easing)
    {
        var easedT = ApplyEasing(t, easing);
        return new CinematicCameraSystemCameraPosition
        {
            Position = new CinematicCameraSystemCameraVector3(
                Lerp(start.Position.X, end.Position.X, easedT),
                Lerp(start.Position.Y, end.Position.Y, easedT),
                Lerp(start.Position.Z, end.Position.Z, easedT)
            ),
            Rotation = new CinematicCameraSystemCameraVector3(
                LerpAngle(start.Rotation.X, end.Rotation.X, easedT),
                LerpAngle(start.Rotation.Y, end.Rotation.Y, easedT),
                LerpAngle(start.Rotation.Z, end.Rotation.Z, easedT)
            ),
            FieldOfView = Lerp(start.FieldOfView, end.FieldOfView, easedT)
        };
    }

    public void ActivateCamera(string cameraId)
    {
        foreach (var state in _cameraStates.Values)
            state.IsActive = state.CameraId == cameraId;
    }

    public CinematicCameraSystemCameraState? GetActiveCamera()
    {
        return _cameraStates.Values.FirstOrDefault(c => c.IsActive);
    }

    private float ApplyEasing(float t, CinematicCameraSystemEasingFunction easing)
    {
        return easing switch
        {
            CinematicCameraSystemEasingFunction.EaseIn => t * t,
            CinematicCameraSystemEasingFunction.EaseOut => 1 - (1 - t) * (1 - t),
            CinematicCameraSystemEasingFunction.EaseInOut => t < 0.5f ? 2 * t * t : 1 - MathF.Pow(-2 * t + 2, 2) / 2,
            _ => t
        };
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
