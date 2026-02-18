namespace SaveState.Application.Mugen.Services.AdvancedCombat.Engines;

using Microsoft.Extensions.Logging;
using SaveState.Application.Mugen.Models.AdvancedCombat;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using System.Collections.Concurrent;

/// <summary>
/// Z-axis movement engine for 3D combat positioning.
/// </summary>
public class ZAxisEngine
{
    private readonly ILogger<ZAxisEngine> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly ConcurrentDictionary<string, List<ZAxisMovement>> _movements = new();

    public ZAxisEngine(ILogger<ZAxisEngine> logger, ITimeProvider timeProvider)
    {
        _logger = logger;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Executes a sidestep movement.
    /// </summary>
    public Task<Result<ZAxisMovement>> ExecuteSidestepAsync(AdvancedCombatSession session, SidestepRequest request, CancellationToken ct = default)
    {
        var movementId = Guid.NewGuid().ToString();
        var movement = new ZAxisMovement
        {
            MovementId = movementId,
            SessionId = session.SessionId,
            Direction = request.Direction,
            Distance = request.Distance,
            Speed = request.Speed,
            Duration = TimeSpan.FromMilliseconds(300),
            ExecutedAt = _timeProvider.UtcNow,
            Success = true
        };

        var list = _movements.GetOrAdd(session.SessionId, _ => new List<ZAxisMovement>());
        lock (list)
        {
            list.Add(movement);
        }

        session.CurrentZPosition += request.Direction == ZDirection.Left ? -request.Distance : request.Distance;

        _logger.LogDebug("Sidestep executed for session {SessionId}: {Direction}", session.SessionId, request.Direction);
        return Task.FromResult(Result.Success(movement));
    }

    /// <summary>
    /// Gets the current Z-axis positioning for a session.
    /// </summary>
    public Task<Result<ZAxisPositioning>> GetPositioningAsync(AdvancedCombatSession session, CancellationToken ct = default)
    {
        var positioning = new ZAxisPositioning
        {
            CurrentZPosition = session.CurrentZPosition,
            AvailableRange = 10.0f,
            OptimalPositions = new[] { -5.0f, 0.0f, 5.0f },
            TacticalAdvantages = new List<TacticalAdvantage>
            {
                new() { Type = "Evasion", Strength = 0.8f },
                new() { Type = "Positioning", Strength = 0.7f }
            },
            MeasuredAt = _timeProvider.UtcNow
        };

        return Task.FromResult(Result.Success(positioning));
    }

    /// <summary>
    /// Gets all movements for a session (used for analysis).
    /// </summary>
    public IReadOnlyList<ZAxisMovement> GetMovementsForSession(string sessionId)
    {
        return _movements.TryGetValue(sessionId, out var movements) ? movements : new List<ZAxisMovement>();
    }

    /// <summary>
    /// Calculates positioning efficiency based on movements.
    /// </summary>
    public float CalculatePositioningEfficiency(List<ZAxisMovement> movements, AdvancedCombatSession session)
    {
        if (movements.Count == 0) return 0f;
        return Math.Min(1.0f, movements.Count / 10.0f);
    }

    /// <summary>
    /// Calculates evasion success rate.
    /// </summary>
    public float CalculateEvasionSuccess(List<ZAxisMovement> movements)
    {
        if (movements.Count == 0) return 0f;
        var successful = movements.Count(m => m.Success);
        return (float)successful / movements.Count;
    }
}
