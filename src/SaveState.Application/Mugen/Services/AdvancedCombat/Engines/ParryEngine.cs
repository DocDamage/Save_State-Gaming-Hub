namespace SaveState.Application.Mugen.Services.AdvancedCombat.Engines;

using Microsoft.Extensions.Logging;
using SaveState.Application.Mugen.Models.AdvancedCombat;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using System.Collections.Concurrent;

/// <summary>
/// Parry and counter engine for defensive mechanics.
/// </summary>
public class ParryEngine
{
    private readonly ILogger<ParryEngine> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly ConcurrentDictionary<string, ParryWindow> _activeWindows = new();

    public ParryEngine(ILogger<ParryEngine> logger, ITimeProvider timeProvider)
    {
        _logger = logger;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Attempts to execute a parry.
    /// </summary>
    public Task<Result<ParryResult>> AttemptParryAsync(AdvancedCombatSession session, ParryRequest request, CancellationToken ct = default)
    {
        var parryId = Guid.NewGuid().ToString();
        var success = request.InputFrame <= request.ReactionWindow;

        CounterAttack? counter = null;
        if (success)
        {
            counter = new CounterAttack
            {
                CounterId = Guid.NewGuid().ToString(),
                SessionId = session.SessionId,
                OriginalAttack = request.ExpectedAttack,
                CounterMove = "Counter Strike",
                DamageMultiplier = 1.5f,
                FrameAdvantage = 12,
                IsGuaranteed = true,
                ExecutedAt = _timeProvider.UtcNow
            };
        }

        var result = new ParryResult
        {
            Success = success,
            ParryId = parryId,
            SessionId = session.SessionId,
            Type = request.Type,
            TimingPrecision = request.ReactionWindow - request.InputFrame,
            Counter = counter,
            ExecutedAt = _timeProvider.UtcNow
        };

        _logger.LogDebug("Parry attempted for session {SessionId}: Success={Success}", session.SessionId, success);
        return Task.FromResult(Result.Success(result));
    }

    /// <summary>
    /// Activates a parry window for a session.
    /// </summary>
    public Task<Result<ParryWindow>> ActivateParryWindowAsync(string sessionId, ParryType type, CancellationToken ct = default)
    {
        var windowId = Guid.NewGuid().ToString();
        var (startup, active, recovery) = type switch
        {
            ParryType.Light => (2, 6, 12),
            ParryType.Heavy => (4, 8, 16),
            ParryType.Special => (3, 10, 14),
            ParryType.Perfect => (1, 4, 8),
            _ => (3, 6, 12)
        };

        var window = new ParryWindow
        {
            WindowId = windowId,
            SessionId = sessionId,
            Type = type,
            StartupFrames = startup,
            ActiveFrames = active,
            RecoveryFrames = recovery,
            ActivatedAt = _timeProvider.UtcNow,
            IsActive = true
        };

        _activeWindows[windowId] = window;

        _logger.LogDebug("Parry window activated for session {SessionId}: Type={Type}", sessionId, type);
        return Task.FromResult(Result.Success(window));
    }
}
