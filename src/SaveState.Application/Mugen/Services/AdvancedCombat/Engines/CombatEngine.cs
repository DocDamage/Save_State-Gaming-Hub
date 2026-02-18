namespace SaveState.Application.Mugen.Services.AdvancedCombat.Engines;

using Microsoft.Extensions.Logging;
using SaveState.Application.Mugen.Models.AdvancedCombat;
using SaveState.Core.Common.Services;
using System.Collections.Concurrent;

/// <summary>
/// Core combat engine for advanced combat mechanics.
/// </summary>
public class CombatEngine
{
    private readonly ILogger<CombatEngine> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly ConcurrentDictionary<string, AdvancedCombatSession> _sessions = new();

    public CombatEngine(ILogger<CombatEngine> logger, ITimeProvider timeProvider)
    {
        _logger = logger;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Initializes a new combat session.
    /// </summary>
    public Task<Result<AdvancedCombatSession>> InitializeSessionAsync(AdvancedCombatSessionRequest request, CancellationToken ct = default)
    {
        var sessionId = Guid.NewGuid().ToString();
        var session = new AdvancedCombatSession
        {
            SessionId = sessionId,
            Player1Id = request.Player1Id,
            Player2Id = request.Player2Id,
            EnableZAxisMovement = request.EnableZAxisMovement,
            EnableJuggleScaling = request.EnableJuggleScaling,
            EnableFrameDataDisplay = request.EnableFrameDataDisplay,
            EnableInputBuffering = request.EnableInputBuffering,
            CurrentZPosition = 0f,
            GravityScale = 1.0f,
            JuggleHeight = 0f,
            BufferWindow = 8,
            StartedAt = _timeProvider.UtcNow,
            Status = CombatStatus.Active
        };

        _sessions[sessionId] = session;
        _logger.LogInformation("Combat session {SessionId} initialized", sessionId);

        return Task.FromResult(Result.Success(session));
    }

    /// <summary>
    /// Gets an existing combat session by ID.
    /// </summary>
    public Task<Result<AdvancedCombatSession>> GetSessionAsync(string sessionId, CancellationToken ct = default)
    {
        if (_sessions.TryGetValue(sessionId, out var session))
        {
            return Task.FromResult(Result.Success(session));
        }

        return Task.FromResult(Result.Failure<AdvancedCombatSession>($"Session {sessionId} not found", ErrorType.NotFound));
    }

    /// <summary>
    /// Ends an active combat session.
    /// </summary>
    public Task<Result<bool>> EndSessionAsync(string sessionId, CancellationToken ct = default)
    {
        if (_sessions.TryGetValue(sessionId, out var session))
        {
            session.Status = CombatStatus.Completed;
            session.EndedAt = _timeProvider.UtcNow;
            _sessions.TryRemove(sessionId, out _);
            _logger.LogInformation("Combat session {SessionId} ended", sessionId);
            return Task.FromResult(Result.Success(true));
        }

        return Task.FromResult(Result.Failure<bool>($"Session {sessionId} not found", ErrorType.NotFound));
    }
}
