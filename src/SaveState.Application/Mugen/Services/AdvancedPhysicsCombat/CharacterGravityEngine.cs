using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using Microsoft.Extensions.Logging;

namespace SaveState.Application.Mugen.Services;

/// <summary>
/// Character gravity engine for individual character physics.
/// </summary>
public class CharacterGravityEngine
{
    private readonly ILogger<CharacterGravityEngine> _logger;
    private readonly ITimeProvider _timeProvider;

    public CharacterGravityEngine(ILogger<CharacterGravityEngine> logger, ITimeProvider timeProvider)
    {
        _logger = logger;
        _timeProvider = timeProvider;
    }

    public Task<CharacterGravityState> CalculateGravityAsync(string characterId, GravityCalculationRequest request, CancellationToken ct)
    {
        var baseGravity = 1.0f;
        var characterMultiplier = GetCharacterGravityMultiplier(characterId);

        return Task.FromResult(new CharacterGravityState
        {
            CharacterId = characterId,
            FallSpeed = baseGravity * characterMultiplier,
            JumpHeight = 100.0f / characterMultiplier,
            AirControl = 1.0f / characterMultiplier,
            DashSpeed = 8.0f * characterMultiplier,
            TerminalVelocity = 15.0f * characterMultiplier,
            CalculatedAt = _timeProvider.UtcNow
        });
    }

    private float GetCharacterGravityMultiplier(string characterId)
    {
        return characterId.ToLower() switch
        {
            var c when c.Contains("light") => 0.8f,
            var c when c.Contains("heavy") => 1.2f,
            var c when c.Contains("fast") => 0.9f,
            _ => 1.0f
        };
    }
}
