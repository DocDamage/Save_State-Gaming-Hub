using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using Microsoft.Extensions.Logging;
using System.Numerics;

namespace SaveState.Application.Mugen.Services;

/// <summary>
/// Hit detection engine for axis-aware collision detection.
/// </summary>
public class HitDetectionEngine
{
    private readonly ILogger<HitDetectionEngine> _logger;
    private readonly ITimeProvider _timeProvider;

    public HitDetectionEngine(ILogger<HitDetectionEngine> logger, ITimeProvider timeProvider)
    {
        _logger = logger;
        _timeProvider = timeProvider;
    }

    public Task<HitDetectionResult> ProcessHitAsync(string attackerId, string defenderId, HitRequest request, CancellationToken ct)
    {
        // Process axis-aware hit detection
        var depthDamage = CalculateDepthDamage((float)request.AttackPosition.Z, (float)request.DefensePosition.Z);
        var angleMultiplier = CalculateAngleMultiplier(request.AttackAngle, request.DefenseAngle);
        var crossUpCheck = IsCrossUp(request.AttackPosition, request.DefensePosition);

        var result = new HitDetectionResult
        {
            AttackerId = attackerId,
            DefenderId = defenderId,
            Damage = (int)(request.BaseDamage * depthDamage * angleMultiplier),
            WasBlocked = request.DefenseAngle < -45,
            IsCrossUp = crossUpCheck,
            HitAngle = request.AttackAngle,
            DepthBonus = depthDamage,
            AngleBonus = angleMultiplier,
            ProcessedAt = _timeProvider.UtcNow
        };

        return Task.FromResult(result);
    }

    public Task<AxisPositioning> GetAxisPositioningAsync(string characterId, CancellationToken ct)
    {
        return Task.FromResult(new AxisPositioning
        {
            CharacterId = characterId,
            CurrentZPosition = 0.0f,
            OptimalAttackRange = new Vector3(50, 0, 20),
            CrossUpOpportunities = new[] { new Vector3(-30, 0, 15) },
            MeasuredAt = _timeProvider.UtcNow
        });
    }

    private float CalculateDepthDamage(float attackZ, float defenseZ)
    {
        var depthDifference = Math.Abs(attackZ - defenseZ);
        return 1.0f + (depthDifference / 50.0f) * 0.5f;
    }

    private float CalculateAngleMultiplier(float attackAngle, float defenseAngle)
    {
        var angleDifference = Math.Abs(attackAngle - defenseAngle);
        return 1.0f + (angleDifference / 180.0f) * 0.3f;
    }

    private bool IsCrossUp(Vector3 attackPos, Vector3 defensePos)
    {
        return Math.Sign(attackPos.X) != Math.Sign(defensePos.X) &&
               Math.Abs(attackPos.Z - defensePos.Z) > 10;
    }
}
