namespace SaveState.Application.Mugen.Services.RealityWarping.Engines;

using Microsoft.Extensions.Logging;

public class EnvironmentalEngine
{
    private readonly ILogger<EnvironmentalEngine> _logger;

    public EnvironmentalEngine(ILogger<EnvironmentalEngine> logger) => _logger = logger;

    /// <summary>
    /// Calculates the stability of a dimensional rift based on size and duration.
    /// </summary>
    public float CalculateRiftStability(float size, TimeSpan duration)
    {
        // Larger rifts are less stable
        float sizeFactor = 1.0f - (size / 100.0f * 0.5f);

        // Longer duration rifts tend to become more unstable over time
        float durationFactor = 1.0f - (float)(duration.TotalMinutes / 60.0 * 0.3f);

        float stability = Math.Max(0.0f, Math.Min(1.0f, sizeFactor * durationFactor));

        // Add some randomness for realism
        stability *= (0.9f + Random.Shared.NextSingle() * 0.1f);

        _logger.LogDebug("Calculated rift stability: {Stability:F2} (size: {Size}, duration: {Duration})",
            stability, size, duration);

        return stability;
    }
}
