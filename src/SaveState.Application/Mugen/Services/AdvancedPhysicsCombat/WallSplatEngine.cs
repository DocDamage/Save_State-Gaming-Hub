using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using Microsoft.Extensions.Logging;
using System.Numerics;

namespace SaveState.Application.Mugen.Services;

/// <summary>
/// Wall splat engine for wall collision mechanics.
/// </summary>
public class WallSplatEngine
{
    private readonly ILogger<WallSplatEngine> _logger;
    private readonly ITimeProvider _timeProvider;

    public WallSplatEngine(ILogger<WallSplatEngine> logger, ITimeProvider timeProvider)
    {
        _logger = logger;
        _timeProvider = timeProvider;
    }

    public Task<WallSplatResult> ProcessSplatAsync(string characterId, WallCollision collision, CancellationToken ct)
    {
        var impactForce = CalculateImpactForce(collision.Velocity, collision.Angle);
        var damage = (int)(impactForce * 0.5f);
        var bounceAngle = CalculateBounceAngle(collision.Angle);
        var bounceVelocity = (float)(Math.Sqrt(collision.Velocity.X * collision.Velocity.X +
                                         collision.Velocity.Y * collision.Velocity.Y +
                                         collision.Velocity.Z * collision.Velocity.Z) * 0.7);

        return Task.FromResult(new WallSplatResult
        {
            CharacterId = characterId,
            Damage = damage,
            BounceAngle = bounceAngle,
            BounceVelocity = bounceVelocity,
            StunDuration = TimeSpan.FromMilliseconds(damage * 10),
            RecoveryWindow = TimeSpan.FromSeconds(2),
            ComboExtensionPossible = damage < 50,
            ProcessedAt = _timeProvider.UtcNow
        });
    }

    public Task<WallCollisionMetrics> GetMetricsAsync(string characterId, CancellationToken ct)
    {
        return Task.FromResult(new WallCollisionMetrics
        {
            CharacterId = characterId,
            TotalSplats = 12,
            AverageDamage = 35.5f,
            BounceEfficiency = 0.75f,
            ComboExtensionRate = 0.6f,
            MeasuredAt = _timeProvider.UtcNow
        });
    }

    private float CalculateImpactForce(Vector3 velocity, float angle)
    {
        var speed = Math.Sqrt((double)velocity.X * velocity.X + (double)velocity.Y * velocity.Y + (double)velocity.Z * velocity.Z);
        var angleFactor = Math.Abs(Math.Sin((double)angle * Math.PI / 180.0));
        return (float)speed * (float)angleFactor;
    }

    private float CalculateBounceAngle(float incidentAngle)
    {
        return -incidentAngle + (float)(new Random().NextDouble() * 20 - 10);
    }
}
