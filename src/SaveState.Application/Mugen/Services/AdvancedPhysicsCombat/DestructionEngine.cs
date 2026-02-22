using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using Microsoft.Extensions.Logging;
using System.Numerics;

namespace SaveState.Application.Mugen.Services;

/// <summary>
/// Destruction engine for environmental destruction.
/// </summary>
public class DestructionEngine
{
    private readonly ILogger<DestructionEngine> _logger;
    private readonly ITimeProvider _timeProvider;

    public DestructionEngine(ILogger<DestructionEngine> logger, ITimeProvider timeProvider)
    {
        _logger = logger;
        _timeProvider = timeProvider;
    }

    public Task<DestructionResult> ProcessDestructionAsync(string stageId, DestructionRequest request, CancellationToken ct)
    {
        var breakThreshold = CalculateBreakThreshold(request.Damage, request.CharacterPower);
        var isBreakTriggered = request.Damage >= breakThreshold;

        if (isBreakTriggered)
        {
            return Task.FromResult(new DestructionResult
            {
                StageId = stageId,
                BreakType = DetermineBreakType(request.ImpactLocation),
                Damage = request.Damage,
                HazardLevel = CalculateHazardLevel(request.ImpactLocation),
                AffectedArea = CalculateAffectedArea(request.ImpactLocation),
                DebrisCount = new Random().Next(5, 15),
                StageTransformation = true,
                ProcessedAt = _timeProvider.UtcNow
            });
        }

        return Task.FromResult(new DestructionResult
        {
            StageId = stageId,
            BreakType = AdvancedPhysicsCombatServiceBreakType.None,
            Damage = request.Damage,
            HazardLevel = 0,
            AffectedArea = new Vector3(0, 0, 0),
            DebrisCount = 0,
            StageTransformation = false,
            ProcessedAt = _timeProvider.UtcNow
        });
    }

    public Task<DestructionMetrics> GetMetricsAsync(string stageId, CancellationToken ct)
    {
        return Task.FromResult(new DestructionMetrics
        {
            StageId = stageId,
            TotalBreaks = 8,
            HazardLevelSum = 25,
            AverageAffectedArea = 150.0f,
            TransformationEvents = 3,
            MeasuredAt = _timeProvider.UtcNow
        });
    }

    private float CalculateBreakThreshold(float damage, float characterPower)
    {
        return 500.0f / characterPower;
    }

    private AdvancedPhysicsCombatServiceBreakType DetermineBreakType(string impactLocation)
    {
        return impactLocation.ToLower() switch
        {
            var l when l.Contains("wall") => AdvancedPhysicsCombatServiceBreakType.WallBreak,
            var l when l.Contains("floor") => AdvancedPhysicsCombatServiceBreakType.FloorBreak,
            var l when l.Contains("ceiling") => AdvancedPhysicsCombatServiceBreakType.CeilingBreak,
            _ => AdvancedPhysicsCombatServiceBreakType.StructureBreak
        };
    }

    private int CalculateHazardLevel(string impactLocation)
    {
        return impactLocation.ToLower() switch
        {
            var l when l.Contains("wall") => 2,
            var l when l.Contains("floor") => 3,
            var l when l.Contains("ceiling") => 1,
            _ => 2
        };
    }

    private Vector3 CalculateAffectedArea(string impactLocation)
    {
        return new Vector3(100, 50, 50);
    }
}
