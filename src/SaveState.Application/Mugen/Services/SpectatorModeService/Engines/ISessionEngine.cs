using SaveState.Core.Common;

namespace SaveState.Application.Mugen.Services.SpectatorModeService.Engines;

/// <summary>
/// Interface for managing spectator sessions.
/// </summary>
public interface ISessionEngine
{
    /// <summary>
    /// Creates a new spectator session.
    /// </summary>
    Result<SpectatorSession> CreateSession(string matchId, IReadOnlyList<string> cameraAngles, IReadOnlyList<string> overlays);

    /// <summary>
    /// Stops a spectator session.
    /// </summary>
    Result StopSession(string sessionId);

    /// <summary>
    /// Tries to get a session by ID.
    /// </summary>
    bool TryGetSession(string sessionId, out SpectatorSession session);

    /// <summary>
    /// Tries to get match data by match ID.
    /// </summary>
    bool TryGetMatchData(string matchId, out MatchSpectatorData matchData);

    /// <summary>
    /// Calculates the average session length for a match.
    /// </summary>
    TimeSpan CalculateAverageSessionLength(MatchSpectatorData matchData, Dictionary<string, SpectatorSession> activeSessions);

    /// <summary>
    /// Gets all sessions for a specific match.
    /// </summary>
    IEnumerable<SpectatorSession> GetSessionsForMatch(string matchId);

    /// <summary>
    /// Updates match data with a specified action.
    /// </summary>
    void UpdateMatchData(string matchId, Action<MatchSpectatorData> updateAction);
}
