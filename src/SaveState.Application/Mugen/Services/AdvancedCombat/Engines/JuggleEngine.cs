namespace SaveState.Application.Mugen.Services.AdvancedCombat.Engines;

using Microsoft.Extensions.Logging;
using SaveState.Application.Mugen.Models.AdvancedCombat;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using System.Collections.Concurrent;

/// <summary>
/// Juggle combo engine for air combo mechanics.
/// </summary>
public class JuggleEngine
{
    private readonly ILogger<JuggleEngine> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly ConcurrentDictionary<string, List<JuggleState>> _juggles = new();

    public JuggleEngine(ILogger<JuggleEngine> logger, ITimeProvider timeProvider)
    {
        _logger = logger;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Applies juggle gravity to an airborne opponent.
    /// </summary>
    public Task<Result<JuggleState>> ApplyJuggleGravityAsync(AdvancedCombatSession session, JuggleRequest request, CancellationToken ct = default)
    {
        var juggleId = Guid.NewGuid().ToString();
        var gravityMultiplier = request.ApplyScaling
            ? Math.Max(0.5f, 1.0f - (request.ComboLength * 0.05f))
            : 1.0f;

        var state = new JuggleState
        {
            JuggleId = juggleId,
            SessionId = session.SessionId,
            CurrentHeight = request.CurrentHeight,
            GravityMultiplier = gravityMultiplier,
            ComboLength = request.ComboLength,
            MomentumFactor = Math.Min(1.0f, request.ComboLength * 0.1f),
            AppliedAt = _timeProvider.UtcNow,
            Active = true,
            State = request.CurrentHeight > 0 ? JuggleStateType.Airborne : JuggleStateType.Grounded
        };

        var list = _juggles.GetOrAdd(session.SessionId, _ => new List<JuggleState>());
        lock (list)
        {
            list.Add(state);
        }

        session.GravityScale = gravityMultiplier;
        session.JuggleHeight = request.CurrentHeight;

        _logger.LogDebug("Juggle gravity applied for session {SessionId}: {Multiplier}x", session.SessionId, gravityMultiplier);
        return Task.FromResult(Result.Success(state));
    }

    /// <summary>
    /// Gets the current physics state for a session.
    /// </summary>
    public Task<Result<PhysicsState>> GetPhysicsStateAsync(AdvancedCombatSession session, CancellationToken ct = default)
    {
        var state = new PhysicsState
        {
            GravityScale = session.GravityScale,
            JuggleHeight = session.JuggleHeight,
            AirControl = 0.8f,
            LandingLag = 6,
            MeasuredAt = _timeProvider.UtcNow
        };

        return Task.FromResult(Result.Success(state));
    }

    /// <summary>
    /// Gets all juggle states for a session (used for analysis).
    /// </summary>
    public IReadOnlyList<JuggleState> GetJugglesForSession(string sessionId)
    {
        return _juggles.TryGetValue(sessionId, out var juggles) ? juggles : new List<JuggleState>();
    }

    /// <summary>
    /// Calculates combo extension rate.
    /// </summary>
    public float CalculateComboExtension(List<JuggleState> juggles)
    {
        if (juggles.Count == 0) return 0f;
        var extendedJuggles = juggles.Count(j => j.ComboLength > 3);
        return (float)extendedJuggles / juggles.Count;
    }
}
