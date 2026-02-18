using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;

namespace SaveState.Application.Mugen.Services.SpectatorModeService.Engines;

/// <summary>
/// Engine for managing spectator camera angles.
/// </summary>
public class CameraEngine : ICameraEngine
{
    private readonly ILogger<CameraEngine> _logger;
    private readonly ITimeProvider _timeProvider;
    private static readonly IReadOnlyList<string> DefaultCameraAngles = new List<string>
    {
        "Default",
        "SideView",
        "Overhead",
        "Player1CloseUp",
        "Player2CloseUp",
        "Dynamic",
        "Cinematic",
        "FreeCam"
    };

    public CameraEngine(ILogger<CameraEngine> logger, ITimeProvider timeProvider)
    {
        _logger = logger;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Gets all available camera angles.
    /// </summary>
    public IReadOnlyList<string> GetAvailableCameraAngles()
    {
        return DefaultCameraAngles;
    }

    /// <summary>
    /// Changes the camera angle for a session.
    /// </summary>
    public Result ChangeCameraAngle(SpectatorSession session, string cameraAngle)
    {
        if (!DefaultCameraAngles.Contains(cameraAngle))
        {
            return Result.Failure($"Invalid camera angle: {cameraAngle}");
        }

        session.CurrentCameraAngle = cameraAngle;
        session.LastCameraChange = _timeProvider.UtcNow;

        _logger.LogInformation(
            "Changed camera angle to {CameraAngle} for session {SessionId}",
            cameraAngle,
            session.SessionId);

        return Result.Success();
    }

    /// <summary>
    /// Gets the most popular camera angles for a match based on active sessions.
    /// </summary>
    public IReadOnlyDictionary<string, int> GetPopularCameraAngles(
        string matchId,
        Dictionary<string, SpectatorSession> activeSessions)
    {
        var matchSessions = activeSessions.Values.Where(s => s.MatchId == matchId);
        
        var angleCounts = matchSessions
            .GroupBy(s => s.CurrentCameraAngle ?? "Default")
            .ToDictionary(
                g => g.Key,
                g => g.Count());

        // Ensure all angles are represented
        foreach (var angle in DefaultCameraAngles)
        {
            if (!angleCounts.ContainsKey(angle))
            {
                angleCounts[angle] = 0;
            }
        }

        return angleCounts;
    }
}
