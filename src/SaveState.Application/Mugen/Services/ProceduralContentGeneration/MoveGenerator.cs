using Microsoft.Extensions.Logging;

namespace SaveState.Application.Mugen.Services.ProceduralContentGeneration;

/// <summary>
/// Move generator for procedural move creation.
/// </summary>
public class ProceduralContentGeneratorMoveGenerator
{
    private readonly ILogger<ProceduralContentGeneratorMoveGenerator> _logger;

    public ProceduralContentGeneratorMoveGenerator(ILogger<ProceduralContentGeneratorMoveGenerator> logger)
    {
        _logger = logger;
    }

    public async Task<ProceduralContentGeneratorMoveParameters> GenerateMoveParametersAsync(ProceduralContentGeneratorMoveGenerationRequest request, ProceduralContentGeneratorCharacterStyleAnalysis styleAnalysis, CancellationToken ct)
    {
        // Generate move parameters based on request and character style
        return new ProceduralContentGeneratorMoveParameters
        {
            Damage = CalculateDamage(request.PowerLevel, request.MoveType),
            StartupFrames = CalculateStartupFrames(request.Difficulty, request.MoveType),
            ActiveFrames = CalculateActiveFrames(request.MoveType),
            RecoveryFrames = CalculateRecoveryFrames(request.PowerLevel, request.MoveType),
            Range = CalculateRange(request.MoveType, request.RequiredMechanics),
            Hitstun = CalculateHitstun(request.PowerLevel),
            Blockstun = CalculateBlockstun(request.PowerLevel),
            Knockback = CalculateKnockback(request.PowerLevel, request.MoveType),
            MeterGain = CalculateMeterGain(request.PowerLevel),
            IsProjectile = request.RequiredMechanics.Contains("projectile"),
            IsAntiAir = request.RequiredMechanics.Contains("anti_air"),
            IsThrow = request.RequiredMechanics.Contains("throw")
        };
    }

    private int CalculateDamage(double powerLevel, ProceduralContentGeneratorMoveType type)
    {
        var baseDamage = type switch
        {
            ProceduralContentGeneratorMoveType.Normal => 30,
            ProceduralContentGeneratorMoveType.Special => 70,
            ProceduralContentGeneratorMoveType.Super => 120,
            _ => 50
        };

        return (int)(baseDamage * powerLevel);
    }

    private int CalculateStartupFrames(ProceduralContentGeneratorDifficultyLevel difficulty, ProceduralContentGeneratorMoveType type)
    {
        var baseFrames = type switch
        {
            ProceduralContentGeneratorMoveType.Normal => 4,
            ProceduralContentGeneratorMoveType.Special => 12,
            ProceduralContentGeneratorMoveType.Super => 20,
            _ => 8
        };

        var difficultyMultiplier = difficulty switch
        {
            ProceduralContentGeneratorDifficultyLevel.Easy => 0.8,
            ProceduralContentGeneratorDifficultyLevel.Hard => 1.2,
            _ => 1.0
        };

        return (int)(baseFrames * difficultyMultiplier);
    }

    private int CalculateActiveFrames(ProceduralContentGeneratorMoveType type)
    {
        return type switch
        {
            ProceduralContentGeneratorMoveType.Normal => 3,
            ProceduralContentGeneratorMoveType.Special => 8,
            ProceduralContentGeneratorMoveType.Super => 15,
            _ => 5
        };
    }

    private int CalculateRecoveryFrames(double powerLevel, ProceduralContentGeneratorMoveType type)
    {
        var baseFrames = type switch
        {
            ProceduralContentGeneratorMoveType.Normal => 8,
            ProceduralContentGeneratorMoveType.Special => 18,
            ProceduralContentGeneratorMoveType.Super => 30,
            _ => 12
        };

        return (int)(baseFrames * (2.0 - powerLevel)); // Higher power = less recovery
    }

    private int CalculateRange(ProceduralContentGeneratorMoveType type, IReadOnlyList<string> mechanics)
    {
        if (mechanics.Contains("projectile")) return 200;
        if (type == ProceduralContentGeneratorMoveType.Super) return 120;
        if (type == ProceduralContentGeneratorMoveType.Special) return 80;
        return 50;
    }

    private int CalculateHitstun(double powerLevel) => (int)(15 * powerLevel);
    private int CalculateBlockstun(double powerLevel) => (int)(12 * powerLevel);
    private int CalculateKnockback(double powerLevel, ProceduralContentGeneratorMoveType type)
    {
        var baseKnockback = type == ProceduralContentGeneratorMoveType.Super ? 20 : 10;
        return (int)(baseKnockback * powerLevel);
    }
    private int CalculateMeterGain(double powerLevel) => (int)(20 * powerLevel);
}
