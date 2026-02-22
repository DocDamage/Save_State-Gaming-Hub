using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;

namespace SaveState.Application.Mugen.Services;

/// <summary>
/// Spectator engine for crowd influence mechanics.
/// Note: Named EmotionalResonanceSpectatorEngine to avoid conflict with existing SpectatorEngine.
/// </summary>
internal class EmotionalResonanceSpectatorEngine
{
    private readonly ILogger<EmotionalResonanceSpectatorEngine> _logger;
    private readonly ITimeProvider _timeProvider;

    public EmotionalResonanceSpectatorEngine(ILogger<EmotionalResonanceSpectatorEngine> logger, ITimeProvider timeProvider)
    {
        _logger = logger;
        _timeProvider = timeProvider;
    }

    public async Task<EmotionalResonanceServiceSpectatorInfluence> ProcessSupportAsync(string spectatorId, string characterId, EmotionalResonanceServiceSpectatorSupport support, CancellationToken ct)
    {
        // Process spectator support
        return new EmotionalResonanceServiceSpectatorInfluence
        {
            InfluenceId = Guid.NewGuid().ToString(),
            SpectatorId = spectatorId,
            CharacterId = characterId,
            EmotionalResonanceServiceSupportType = support.EmotionalResonanceServiceSupportType,
            Intensity = support.Intensity,
            Duration = support.Duration,
            Effects = GenerateSupportEffects(support.EmotionalResonanceServiceSupportType, support.Intensity),
            AppliedAt = _timeProvider.UtcNow,
            ExpiresAt = _timeProvider.UtcNow.Add(support.Duration)
        };
    }

    public async Task<EmotionalResonanceServiceCrowdPsychology> UpdateCrowdStateAsync(string matchId, EmotionalResonanceServiceCrowdEvent crowdEvent, CancellationToken ct)
    {
        // Update crowd psychology based on match events
        return new EmotionalResonanceServiceCrowdPsychology
        {
            MatchId = matchId,
            CollectiveMood = EmotionalResonanceServiceCrowdMood.Excited,
            Intensity = 0.8f,
            AffectedCharacters = new[] { "character1", "character2" },
            MoodTriggers = new[] { crowdEvent.EventType.ToString() },
            Duration = TimeSpan.FromMinutes(2),
            UpdatedAt = _timeProvider.UtcNow
        };
    }

    private Dictionary<string, float> GenerateSupportEffects(EmotionalResonanceServiceSupportType supportType, float intensity)
    {
        // Generate support effect modifiers
        return supportType switch
        {
            EmotionalResonanceServiceSupportType.Encouragement => new Dictionary<string, float> { ["morale"] = intensity * 1.2f },
            EmotionalResonanceServiceSupportType.Intimidation => new Dictionary<string, float> { ["opponent_morale"] = -intensity * 0.8f },
            _ => new Dictionary<string, float>()
        };
    }
}
