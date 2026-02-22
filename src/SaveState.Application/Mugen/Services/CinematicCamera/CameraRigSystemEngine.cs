using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.Mugen.Services;
using Microsoft.Extensions.Logging;

namespace SaveState.Application.Mugen.Services;

/// <summary>
/// Camera rig system engine - handles rig setup, constraints, and automation.
/// </summary>
public class CameraRigSystemEngine
{
    private readonly ILogger<CameraRigSystemEngine> _logger;
    private readonly Dictionary<string, CinematicCameraSystemCameraRig> _rigs = new();

    public CameraRigSystemEngine(ILogger<CameraRigSystemEngine> logger)
    {
        _logger = logger;
    }

    public CinematicCameraSystemCameraRig CreateRig(CinematicCameraSystemCameraRigRequest request)
    {
        var rig = new CinematicCameraSystemCameraRig
        {
            RigId = Guid.NewGuid().ToString(),
            Name = request.Name,
            Settings = request.Settings,
            BasePosition = request.BasePosition,
            Cameras = new List<CinematicCameraSystemCamera>()
        };
        _rigs[rig.RigId] = rig;
        _logger.LogInformation("Created rig {RigId}: {Name} of type {Type}", rig.RigId, rig.Name, rig.Settings.RigType);
        return rig;
    }

    public CinematicCameraSystemCameraPosition ApplyConstraints(
        CinematicCameraSystemCameraRig rig,
        CinematicCameraSystemCameraPosition desiredPosition,
        string? targetId = null)
    {
        var result = desiredPosition;

        foreach (var constraint in rig.Settings.Constraints)
        {
            result = constraint.ConstraintType switch
            {
                CinematicCameraSystemConstraintType.LookAt when targetId != null => ApplyLookAtConstraint(result, targetId),
                CinematicCameraSystemConstraintType.Follow when targetId != null => ApplyFollowConstraint(result, targetId, constraint.Offset),
                CinematicCameraSystemConstraintType.Orbit when targetId != null => ApplyOrbitConstraint(result, targetId, constraint),
                _ => result
            };
        }

        return result;
    }

    public CinematicCameraSystemCameraPosition ApplyAutomation(
        CinematicCameraSystemCameraRig rig,
        CinematicCameraSystemCameraPosition currentPosition,
        float deltaTime)
    {
        var settings = rig.Settings.Automation;
        if (!settings.AutoTrackTarget || string.IsNullOrEmpty(settings.TargetId))
            return currentPosition;

        // Simulate tracking
        var targetPosition = GetTargetPosition(settings.TargetId);
        var direction = Normalize(Subtract(targetPosition, currentPosition.Position));
        var speed = settings.TrackingSpeed * deltaTime;

        return new CinematicCameraSystemCameraPosition
        {
            Position = new CinematicCameraSystemCameraVector3(
                currentPosition.Position.X + direction.X * speed,
                currentPosition.Position.Y + direction.Y * speed,
                currentPosition.Position.Z + direction.Z * speed
            ),
            Rotation = CalculateLookRotation(currentPosition.Position, targetPosition),
            FieldOfView = currentPosition.FieldOfView
        };
    }

    public void AddCameraToRig(string rigId, CinematicCameraSystemCamera camera)
    {
        if (_rigs.TryGetValue(rigId, out var rig))
        {
            rig.Cameras.Add(camera);
        }
    }

    private CinematicCameraSystemCameraPosition ApplyLookAtConstraint(CinematicCameraSystemCameraPosition position, string targetId)
    {
        var targetPos = GetTargetPosition(targetId);
        position.Rotation = CalculateLookRotation(position.Position, targetPos);
        return position;
    }

    private CinematicCameraSystemCameraPosition ApplyFollowConstraint(CinematicCameraSystemCameraPosition position, string targetId, CinematicCameraSystemCameraVector3 offset)
    {
        var targetPos = GetTargetPosition(targetId);
        position.Position = new CinematicCameraSystemCameraVector3(
            targetPos.X + offset.X,
            targetPos.Y + offset.Y,
            targetPos.Z + offset.Z
        );
        return position;
    }

    private CinematicCameraSystemCameraPosition ApplyOrbitConstraint(CinematicCameraSystemCameraPosition position, string targetId, CinematicCameraSystemRigConstraint constraint)
    {
        // Simplified orbit logic
        return position;
    }

    private CinematicCameraSystemCameraVector3 GetTargetPosition(string targetId)
    {
        // Placeholder - would look up actual target
        return new CinematicCameraSystemCameraVector3(0, 0, 0);
    }

    private CinematicCameraSystemCameraVector3 CalculateLookRotation(CinematicCameraSystemCameraVector3 from, CinematicCameraSystemCameraVector3 to)
    {
        var direction = Normalize(Subtract(to, from));
        // Simplified - convert direction to euler angles
        return new CinematicCameraSystemCameraVector3(0, 0, 0);
    }

    private CinematicCameraSystemCameraVector3 Normalize(CinematicCameraSystemCameraVector3 v)
    {
        var len = MathF.Sqrt(v.X * v.X + v.Y * v.Y + v.Z * v.Z);
        return len > 0 ? new CinematicCameraSystemCameraVector3(v.X / len, v.Y / len, v.Z / len) : v;
    }

    private CinematicCameraSystemCameraVector3 Subtract(CinematicCameraSystemCameraVector3 a, CinematicCameraSystemCameraVector3 b)
    {
        return new CinematicCameraSystemCameraVector3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
    }
}
