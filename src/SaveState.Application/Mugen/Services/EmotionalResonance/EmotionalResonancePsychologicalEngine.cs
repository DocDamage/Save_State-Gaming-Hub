using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;

namespace SaveState.Application.Mugen.Services;

/// <summary>
/// Psychological engine for breaking point mechanics.
/// </summary>
internal class EmotionalResonancePsychologicalEngine
{
    private readonly ILogger<EmotionalResonancePsychologicalEngine> _logger;
    private readonly ITimeProvider _timeProvider;

    public EmotionalResonancePsychologicalEngine(ILogger<EmotionalResonancePsychologicalEngine> logger, ITimeProvider timeProvider)
    {
        _logger = logger;
        _timeProvider = timeProvider;
    }

    public async Task<EmotionalResonanceServiceBreakingPoint> CheckBreakingPointAsync(EmotionalResonanceServiceResonanceEmotionalState state, CancellationToken ct)
    {
        // Check if emotional state triggers breaking point
        var isTriggered = state.Intensity > 0.8f && state.Stability < 0.3f;

        return new EmotionalResonanceServiceBreakingPoint
        {
            CharacterId = state.CharacterId,
            IsTriggered = isTriggered,
            EmotionalResonanceServiceBreakingPointType = isTriggered ? EmotionalResonanceServiceBreakingPointType.RageMode : EmotionalResonanceServiceBreakingPointType.None,
            TriggerEmotion = state.PrimaryEmotion,
            Intensity = state.Intensity,
            Effects = isTriggered ? GenerateBreakingPointEffects(EmotionalResonanceServiceBreakingPointType.RageMode) : new List<EmotionalResonanceServiceBreakingPointEffect>(),
            Duration = isTriggered ? TimeSpan.FromSeconds(30) : TimeSpan.Zero,
            CheckedAt = _timeProvider.UtcNow
        };
    }

    private List<EmotionalResonanceServiceBreakingPointEffect> GenerateBreakingPointEffects(EmotionalResonanceServiceBreakingPointType type)
    {
        // Generate breaking point effects
        return type switch
        {
            EmotionalResonanceServiceBreakingPointType.RageMode => new List<EmotionalResonanceServiceBreakingPointEffect>
            {
                new EmotionalResonanceServiceBreakingPointEffect { EffectType = "damage_boost", Magnitude = 1.5f, Duration = TimeSpan.FromSeconds(30) },
                new EmotionalResonanceServiceBreakingPointEffect { EffectType = "speed_boost", Magnitude = 1.3f, Duration = TimeSpan.FromSeconds(30) }
            },
            _ => new List<EmotionalResonanceServiceBreakingPointEffect>()
        };
    }
}
