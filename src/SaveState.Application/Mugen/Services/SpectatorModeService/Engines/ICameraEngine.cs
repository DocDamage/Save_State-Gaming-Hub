using SaveState.Core.Common;

namespace SaveState.Application.Mugen.Services.SpectatorModeService.Engines;

/// <summary>
/// Interface for managing spectator camera angles.
/// </summary>
public interface ICameraEngine
{
    /// <summary>
    /// Gets all available camera angles.
    /// </summary>
    IReadOnlyList<string> GetAvailableCameraAngles();

    /// <summary>
    /// Changes the camera angle for a session.
    /// </summary>
    Result ChangeCameraAngle(SpectatorSession session, string cameraAngle);

    /// <summary>
    /// Gets the most popular camera angles for a match based on active sessions.
    /// </summary>
    IReadOnlyDictionary<string, int> GetPopularCameraAngles(string matchId, Dictionary<string, SpectatorSession> activeSessions);
}
