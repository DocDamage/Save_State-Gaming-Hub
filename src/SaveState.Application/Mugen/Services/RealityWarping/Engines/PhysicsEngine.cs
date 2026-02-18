namespace SaveState.Application.Mugen.Services.RealityWarping.Engines;

using Microsoft.Extensions.Logging;
using SaveState.Application.Mugen.Models.RealityWarping;

public class PhysicsEngine
{
    private readonly ILogger<PhysicsEngine> _logger;

    public PhysicsEngine(ILogger<PhysicsEngine> logger) => _logger = logger;

    /// <summary>
    /// Calculates the gravitational pull based on strength and radius.
    /// </summary>
    public float CalculateGravitationalPull(float strength, float radius)
    {
        // G-force calculation: stronger strength = more pull, larger radius = less concentrated pull
        float pull = strength / Math.Max(1.0f, radius * 0.5f);
        float normalizedPull = Math.Min(1.0f, pull / 100.0f);

        _logger.LogDebug("Calculated gravitational pull: {Pull:F2} (strength: {Strength}, radius: {Radius})",
            normalizedPull, strength, radius);

        return normalizedPull;
    }

    /// <summary>
    /// Analyzes physics distortion across all active effects.
    /// </summary>
    public Task<PhysicsDistortionMetrics> AnalyzePhysicsDistortionAsync(
        Dictionary<string, GravityWell> gravityWells,
        Dictionary<string, TimeDilationZone> timeZones,
        int activeWarpCount,
        TimeSpan period,
        CancellationToken ct = default)
    {
        int activeWells = gravityWells.Count(gw => gw.Value.Active);
        float avgGravitationalPull = gravityWells.Any(gw => gw.Value.Active)
            ? gravityWells.Where(gw => gw.Value.Active).Average(gw => gw.Value.GravitationalPull)
            : 0f;

        int activeTimeZones = timeZones.Count(tz => tz.Value.Active);
        float avgTimeDilation = timeZones.Any(tz => tz.Value.Active)
            ? timeZones.Where(tz => tz.Value.Active).Average(tz => tz.Value.TimeScale)
            : 1f;

        int anomalies = activeWarpCount + (activeWells > 5 ? 1 : 0) + (activeTimeZones > 3 ? 1 : 0);

        var metrics = new PhysicsDistortionMetrics
        {
            GravityWellsActive = activeWells,
            AverageGravitationalPull = avgGravitationalPull,
            TimeZonesActive = activeTimeZones,
            AverageTimeDilation = avgTimeDilation,
            PhysicsAnomalies = anomalies
        };

        _logger.LogDebug("Physics distortion metrics: {ActiveWells} wells, {ActiveZones} zones, {Anomalies} anomalies",
            activeWells, activeTimeZones, anomalies);

        return Task.FromResult(metrics);
    }

    /// <summary>
    /// Calculates dimensional integrity based on rift counts.
    /// </summary>
    public float CalculateDimensionalIntegrity(int totalRifts, int unstableRifts)
    {
        if (totalRifts == 0)
            return 1.0f;

        float stabilityRatio = (float)(totalRifts - unstableRifts) / totalRifts;
        float integrity = 0.5f + (stabilityRatio * 0.5f);

        _logger.LogDebug("Calculated dimensional integrity: {Integrity:F2} (total: {Total}, unstable: {Unstable})",
            integrity, totalRifts, unstableRifts);

        return Math.Min(1.0f, integrity);
    }
}
