namespace SaveState.Application.Mugen.Services.RealityWarping.Engines;

using Microsoft.Extensions.Logging;
using SaveState.Application.Mugen.Models.RealityWarping;
using SaveState.Core.Common.Services;

public class RealityEngine
{
    private readonly ILogger<RealityEngine> _logger;
    private readonly ITimeProvider _timeProvider;

    public RealityEngine(ILogger<RealityEngine> logger, ITimeProvider timeProvider)
    {
        _logger = logger;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Calculates the reality distortion level for a warp.
    /// </summary>
    public Task<float> CalculateRealityDistortionAsync(
        WarpType warpType, float intensity, CancellationToken ct = default)
    {
        float baseDistortion = warpType switch
        {
            WarpType.GravityShift => intensity * 0.3f,
            WarpType.TimeWarp => intensity * 0.5f,
            WarpType.DimensionalShift => intensity * 0.7f,
            WarpType.RealityFracture => intensity * 1.0f,
            _ => intensity * 0.5f
        };

        _logger.LogDebug("Calculated reality distortion: {Distortion:F2} for warp type {WarpType}", baseDistortion, warpType);
        return Task.FromResult(baseDistortion);
    }

    /// <summary>
    /// Calculates the stability of a warp based on intensity and duration.
    /// </summary>
    public Task<float> CalculateWarpStabilityAsync(
        float intensity, TimeSpan duration, CancellationToken ct = default)
    {
        // Higher intensity = lower stability
        float intensityFactor = 1.0f - (intensity * 0.5f);

        // Longer duration = slightly lower stability
        float durationFactor = 1.0f - (float)(duration.TotalMinutes / 60.0 * 0.2f);

        float stability = Math.Max(0.0f, Math.Min(1.0f, intensityFactor * durationFactor));

        _logger.LogDebug("Calculated warp stability: {Stability:F2}", stability);
        return Task.FromResult(stability);
    }

    /// <summary>
    /// Gets the current reality state for an area.
    /// </summary>
    public Task<RealityState> GetRealityStateAsync(string areaId, CancellationToken ct = default)
    {
        var state = new RealityState
        {
            AreaId = areaId,
            DistortionLevel = Random.Shared.NextSingle() * 0.5f,
            StabilityIndex = 0.7f + (Random.Shared.NextSingle() * 0.3f),
            ActiveWarps = 0,
            Anomalies = new List<string>(),
            MeasuredAt = _timeProvider.UtcNow
        };

        _logger.LogDebug("Retrieved reality state for area {AreaId}: distortion {Distortion:F2}", areaId, state.DistortionLevel);
        return Task.FromResult(state);
    }

    /// <summary>
    /// Collapses a reality warp.
    /// </summary>
    public Task CollapseWarpAsync(RealityWarp warp, CancellationToken ct = default)
    {
        _logger.LogInformation("Collapsing warp {WarpId} of type {WarpType}", warp.WarpId, warp.WarpType);

        // Simulate collapse effects
        warp.Active = false;

        return Task.CompletedTask;
    }

    /// <summary>
    /// Calculates overall reality stability based on active warps and unstable rifts.
    /// </summary>
    public float CalculateOverallStability(int activeWarps, int unstableRifts)
    {
        float baseStability = 1.0f;

        // Each active warp reduces stability
        float warpPenalty = activeWarps * 0.05f;

        // Each unstable rift significantly reduces stability
        float riftPenalty = unstableRifts * 0.15f;

        float stability = Math.Max(0.0f, baseStability - warpPenalty - riftPenalty);

        _logger.LogDebug("Calculated overall stability: {Stability:F2} (warps: {Warps}, unstable rifts: {UnstableRifts})",
            stability, activeWarps, unstableRifts);

        return stability;
    }
}
