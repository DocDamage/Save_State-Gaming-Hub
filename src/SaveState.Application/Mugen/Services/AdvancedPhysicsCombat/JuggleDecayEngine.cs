using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using Microsoft.Extensions.Logging;

namespace SaveState.Application.Mugen.Services;

/// <summary>
/// Juggle decay engine for realistic combo scaling.
/// </summary>
public class JuggleDecayEngine
{
    private readonly ILogger<JuggleDecayEngine> _logger;
    private readonly ITimeProvider _timeProvider;

    public JuggleDecayEngine(ILogger<JuggleDecayEngine> logger, ITimeProvider timeProvider)
    {
        _logger = logger;
        _timeProvider = timeProvider;
    }

    public Task<JuggleDecayState> ApplyDecayAsync(string characterId, JuggleHit hit, CancellationToken ct)
    {
        var gravityMultiplier = 1.0f + (hit.ComboLength * 0.1f);
        var momentumLoss = Math.Min(hit.ComboLength * 0.15f, 0.8f);

        var state = new JuggleDecayState
        {
            CharacterId = characterId,
            CurrentComboLength = hit.ComboLength,
            MaxComboLength = hit.ComboLength,
            GravityMultiplier = gravityMultiplier,
            MomentumLoss = momentumLoss,
            BreakPointReached = hit.ComboLength >= 15,
            BreakPointTriggers = hit.ComboLength >= 15 ? 1 : 0,
            LastHitTime = _timeProvider.UtcNow
        };

        return Task.FromResult(state);
    }

    public Task<JuggleMetrics> GetMetricsAsync(string characterId, CancellationToken ct)
    {
        return Task.FromResult(new JuggleMetrics
        {
            CharacterId = characterId,
            AverageComboLength = 8.5f,
            MaxComboLength = 15,
            DecayEfficiency = 0.85f,
            BreakPointFrequency = 0.1f,
            MeasuredAt = _timeProvider.UtcNow
        });
    }
}
