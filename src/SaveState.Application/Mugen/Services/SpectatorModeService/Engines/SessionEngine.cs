using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;

namespace SaveState.Application.Mugen.Services.SpectatorModeService.Engines;

/// <summary>
/// Engine for managing spectator sessions.
/// </summary>
public class SessionEngine : ISessionEngine
{
    private readonly ILogger<SessionEngine> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly Dictionary<string, SpectatorSession> _activeSessions = new();
    private readonly Dictionary<string, MatchSpectatorData> _matchData = new();

    public SessionEngine(ILogger<SessionEngine> logger, ITimeProvider timeProvider)
    {
        _logger = logger;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Creates a new spectator session.
    /// </summary>
    public Result<SpectatorSession> CreateSession(
        string matchId,
        IReadOnlyList<string> cameraAngles,
        IReadOnlyList<string> overlays)
    {
        var sessionId = Guid.NewGuid().ToString("N");
        
        var session = new SpectatorSession
        {
            SessionId = sessionId,
            MatchId = matchId,
            StreamUrl = $"/stream/{matchId}",
            CameraAngles = cameraAngles,
            Overlays = overlays,
            StartedAt = _timeProvider.UtcNow,
            CurrentCameraAngle = cameraAngles.FirstOrDefault(),
            ActiveOverlays = new List<string>(),
            Controls = GetDefaultControls()
        };

        _activeSessions[sessionId] = session;
        
        // Update match data
        if (!_matchData.TryGetValue(matchId, out var matchData))
        {
            matchData = new MatchSpectatorData
            {
                MatchId = matchId,
                Spectators = new List<string>(),
                ViewerCount = 0,
                TotalWatchTime = TimeSpan.Zero,
                PeakViewerCount = 0,
                ChatEnabled = true,
                ChatMessages = new List<ChatMessage>()
            };
            _matchData[matchId] = matchData;
        }

        var spectators = matchData.Spectators.ToList();
        spectators.Add(sessionId);
        matchData.Spectators = spectators;
        matchData.ViewerCount = spectators.Count;
        matchData.PeakViewerCount = Math.Max(matchData.PeakViewerCount, matchData.ViewerCount);

        _logger.LogInformation("Created spectator session {SessionId} for match {MatchId}", sessionId, matchId);
        return Result.Success(session);
    }

    /// <summary>
    /// Stops a spectator session.
    /// </summary>
    public Result StopSession(string sessionId)
    {
        if (!_activeSessions.TryGetValue(sessionId, out var session))
        {
            return Result.Failure($"Session {sessionId} not found");
        }

        _activeSessions.Remove(sessionId);

        // Update match data
        if (_matchData.TryGetValue(session.MatchId, out var matchData))
        {
            var spectators = matchData.Spectators.ToList();
            spectators.Remove(sessionId);
            matchData.Spectators = spectators;
            matchData.ViewerCount = spectators.Count;
            
            // Calculate watch time
            var watchTime = _timeProvider.UtcNow - session.StartedAt;
            matchData.TotalWatchTime += watchTime;
        }

        _logger.LogInformation("Stopped spectator session {SessionId}", sessionId);
        return Result.Success();
    }

    /// <summary>
    /// Tries to get a session by ID.
    /// </summary>
    public bool TryGetSession(string sessionId, out SpectatorSession session)
    {
        return _activeSessions.TryGetValue(sessionId, out session!);
    }

    /// <summary>
    /// Tries to get match data by match ID.
    /// </summary>
    public bool TryGetMatchData(string matchId, out MatchSpectatorData matchData)
    {
        return _matchData.TryGetValue(matchId, out matchData!);
    }

    /// <summary>
    /// Gets all sessions for a specific match.
    /// </summary>
    public IEnumerable<SpectatorSession> GetSessionsForMatch(string matchId)
    {
        return _activeSessions.Values.Where(s => s.MatchId == matchId);
    }

    /// <summary>
    /// Calculates the average session length for a match.
    /// </summary>
    public TimeSpan CalculateAverageSessionLength(
        MatchSpectatorData matchData,
        Dictionary<string, SpectatorSession> activeSessions)
    {
        if (matchData.ViewerCount == 0)
        {
            return TimeSpan.Zero;
        }

        var totalTime = matchData.TotalWatchTime;
        
        // Add current active session times
        foreach (var session in activeSessions.Values.Where(s => s.MatchId == matchData.MatchId))
        {
            totalTime += _timeProvider.UtcNow - session.StartedAt;
        }

        return TimeSpan.FromTicks(totalTime.Ticks / matchData.ViewerCount);
    }

    /// <summary>
    /// Updates match data with a specified action.
    /// </summary>
    public void UpdateMatchData(string matchId, Action<MatchSpectatorData> updateAction)
    {
        if (_matchData.TryGetValue(matchId, out var matchData))
        {
            updateAction(matchData);
        }
    }

    private IReadOnlyList<SpectatorControl> GetDefaultControls()
    {
        return new List<SpectatorControl>
        {
            new() { ControlType = "Camera", Description = "Change camera angle", Enabled = true },
            new() { ControlType = "Overlay", Description = "Toggle overlays", Enabled = true },
            new() { ControlType = "Chat", Description = "Send chat message", Enabled = true },
            new() { ControlType = "Replay", Description = "Request replay", Enabled = true }
        };
    }
}
