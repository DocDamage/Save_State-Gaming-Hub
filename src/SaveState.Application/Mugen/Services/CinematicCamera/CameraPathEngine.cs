using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.Mugen.Services;
using Microsoft.Extensions.Logging;

namespace SaveState.Application.Mugen.Services;

/// <summary>
/// Camera path engine - handles path calculation and waypoint interpolation.
/// </summary>
public class CameraPathEngine
{
    private readonly ILogger<CameraPathEngine> _logger;
    private readonly Dictionary<string, CinematicCameraSystemCameraPath> _paths = new();

    public CameraPathEngine(ILogger<CameraPathEngine> logger)
    {
        _logger = logger;
    }

    public CinematicCameraSystemCameraPath CreatePath(CinematicCameraSystemCameraPathRequest request)
    {
        var path = new CinematicCameraSystemCameraPath
        {
            PathId = Guid.NewGuid().ToString(),
            Name = request.Name,
            Waypoints = request.Waypoints,
            InterpolationMode = request.InterpolationMode,
            IsClosedLoop = request.IsClosedLoop,
            Duration = request.Duration
        };
        _paths[path.PathId] = path;
        _logger.LogInformation("Created path {PathId}: {Name} with {WaypointCount} waypoints",
            path.PathId, path.Name, path.Waypoints.Count);
        return path;
    }

    public CinematicCameraSystemCameraPosition GetPositionAtTime(string pathId, float t)
    {
        if (!_paths.TryGetValue(pathId, out var path) || path.Waypoints.Count < 2)
            return new CinematicCameraSystemCameraPosition();

        // Normalize t to 0-1 range
        t = Math.Clamp(t, 0, 1);

        // Calculate which segment we're on
        var segmentCount = path.Waypoints.Count - 1;
        var segmentT = t * segmentCount;
        var segmentIndex = (int)Math.Floor(segmentT);
        var localT = segmentT - segmentIndex;

        if (segmentIndex >= segmentCount)
            return WaypointToPosition(path.Waypoints[^1]);

        var start = path.Waypoints[segmentIndex];
        var end = path.Waypoints[segmentIndex + 1];

        return path.InterpolationMode switch
        {
            CinematicCameraSystemInterpolationMode.Bezier => BezierInterpolate(start, end, localT),
            CinematicCameraSystemInterpolationMode.CatmullRom => CatmullRomInterpolate(path.Waypoints, segmentIndex, localT),
            _ => LinearInterpolate(start, end, localT)
        };
    }

    public float GetPathLength(string pathId)
    {
        if (!_paths.TryGetValue(pathId, out var path))
            return 0;

        float length = 0;
        for (int i = 1; i < path.Waypoints.Count; i++)
        {
            length += Distance(path.Waypoints[i - 1].Position, path.Waypoints[i].Position);
        }
        return length;
    }

    private CinematicCameraSystemCameraPosition LinearInterpolate(CinematicCameraSystemCameraWaypoint a, CinematicCameraSystemCameraWaypoint b, float t)
    {
        return new CinematicCameraSystemCameraPosition
        {
            Position = new CinematicCameraSystemCameraVector3(
                Lerp(a.Position.X, b.Position.X, t),
                Lerp(a.Position.Y, b.Position.Y, t),
                Lerp(a.Position.Z, b.Position.Z, t)
            ),
            Rotation = new CinematicCameraSystemCameraVector3(
                LerpAngle(a.Rotation.X, b.Rotation.X, t),
                LerpAngle(a.Rotation.Y, b.Rotation.Y, t),
                LerpAngle(a.Rotation.Z, b.Rotation.Z, t)
            ),
            FieldOfView = Lerp(a.FieldOfView, b.FieldOfView, t)
        };
    }

    private CinematicCameraSystemCameraPosition BezierInterpolate(CinematicCameraSystemCameraWaypoint a, CinematicCameraSystemCameraWaypoint b, float t)
    {
        // Simplified quadratic bezier
        var easedT = t * t * (3 - 2 * t); // Smoothstep
        return LinearInterpolate(a, b, easedT);
    }

    private CinematicCameraSystemCameraPosition CatmullRomInterpolate(List<CinematicCameraSystemCameraWaypoint> waypoints, int index, float t)
    {
        // Simplified Catmull-Rom (fall back to linear for simplicity)
        return LinearInterpolate(waypoints[index], waypoints[index + 1], t);
    }

    private CinematicCameraSystemCameraPosition WaypointToPosition(CinematicCameraSystemCameraWaypoint waypoint)
    {
        return new CinematicCameraSystemCameraPosition
        {
            Position = waypoint.Position,
            Rotation = waypoint.Rotation,
            FieldOfView = waypoint.FieldOfView
        };
    }

    private float Distance(CinematicCameraSystemCameraVector3 a, CinematicCameraSystemCameraVector3 b)
    {
        var dx = b.X - a.X;
        var dy = b.Y - a.Y;
        var dz = b.Z - a.Z;
        return MathF.Sqrt(dx * dx + dy * dy + dz * dz);
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
