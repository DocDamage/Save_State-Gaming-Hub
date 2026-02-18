namespace SaveState.Application.Mugen.Services.Training.Engines;

using Microsoft.Extensions.Logging;
using SaveState.Core.Common.Services;

/// <summary>
/// Manages training sessions in memory.
/// </summary>
public class SessionManager
{
    private readonly ILogger<SessionManager> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly Dictionary<string, TrainingSession> _sessions = new();
    private readonly Dictionary<string, List<string>> _userSessions = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="SessionManager"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="timeProvider">The time provider.</param>
    public SessionManager(ILogger<SessionManager> logger, ITimeProvider timeProvider)
    {
        _logger = logger;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Tries to get a session by ID.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <param name="session">The session if found.</param>
    /// <returns>True if the session exists.</returns>
    public bool TryGetSession(string sessionId, out TrainingSession? session)
    {
        return _sessions.TryGetValue(sessionId, out session);
    }

    /// <summary>
    /// Adds a new session.
    /// </summary>
    /// <param name="session">The session to add.</param>
    public void AddSession(TrainingSession session)
    {
        _sessions[session.SessionId] = session;

        if (!_userSessions.TryGetValue(session.UserId, out var userSessionList))
        {
            userSessionList = new List<string>();
            _userSessions[session.UserId] = userSessionList;
        }

        userSessionList.Add(session.SessionId);
        _logger.LogInformation("Added session {SessionId} for user {UserId}", session.SessionId, session.UserId);
    }

    /// <summary>
    /// Removes a session by ID.
    /// </summary>
    /// <param name="sessionId">The session ID to remove.</param>
    /// <returns>True if the session was removed.</returns>
    public bool RemoveSession(string sessionId)
    {
        if (_sessions.TryGetValue(sessionId, out var session))
        {
            _sessions.Remove(sessionId);

            if (_userSessions.TryGetValue(session.UserId, out var userSessionList))
            {
                userSessionList.Remove(sessionId);
            }

            _logger.LogInformation("Removed session {SessionId}", sessionId);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Marks a session as completed.
    /// </summary>
    /// <param name="session">The session to complete.</param>
    public void CompleteSession(TrainingSession session)
    {
        session.Status = SessionStatus.Completed;
        session.CompletedAt = _timeProvider.UtcNow;
        session.LastActivity = _timeProvider.UtcNow;
        _logger.LogInformation("Completed session {SessionId}", session.SessionId);
    }

    /// <summary>
    /// Determines if a session should end based on completion criteria.
    /// </summary>
    /// <param name="session">The session to check.</param>
    /// <returns>True if the session should end.</returns>
    public bool ShouldEndSession(TrainingSession session)
    {
        if (session.Status != SessionStatus.Active)
        {
            return true;
        }

        if (session.Duration.HasValue)
        {
            var elapsed = _timeProvider.UtcNow - session.StartedAt;
            if (elapsed >= session.Duration.Value)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Gets all sessions for a user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <returns>List of user sessions.</returns>
    public IReadOnlyList<TrainingSession> GetUserSessions(string userId)
    {
        if (!_userSessions.TryGetValue(userId, out var sessionIds))
        {
            return Array.Empty<TrainingSession>();
        }

        var sessions = new List<TrainingSession>();
        foreach (var sessionId in sessionIds)
        {
            if (_sessions.TryGetValue(sessionId, out var session))
            {
                sessions.Add(session);
            }
        }

        return sessions;
    }
}
