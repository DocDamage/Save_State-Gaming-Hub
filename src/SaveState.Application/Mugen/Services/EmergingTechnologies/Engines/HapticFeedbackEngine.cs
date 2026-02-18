namespace SaveState.Application.Mugen.Services.EmergingTechnologies.Engines;

using Microsoft.Extensions.Logging;
using SaveState.Application.Mugen.Models.EmergingTech;

/// <summary>
/// Engine for haptic feedback control.
/// </summary>
public class HapticFeedbackEngine
{
    private readonly ILogger<HapticFeedbackEngine> _logger;

    public HapticFeedbackEngine(ILogger<HapticFeedbackEngine> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Sends haptic feedback to a device.
    /// </summary>
    public Task<bool> SendFeedbackAsync(
        HapticDevice device,
        HapticFeedbackRequest request,
        CancellationToken ct = default)
    {
        _logger.LogDebug("Sending haptic feedback to device {DeviceId}, intensity: {Intensity}",
            device.DeviceId, request.Intensity);

        // Find the actuator
        var actuator = device.Actuators.FirstOrDefault(a => a.ActuatorId.ToString() == request.ActuatorId);
        if (actuator == null)
        {
            _logger.LogWarning("Actuator {ActuatorId} not found on device {DeviceId}", request.ActuatorId, device.DeviceId);
            return Task.FromResult(false);
        }

        // Simulate sending feedback (always succeed for now)
        return Task.FromResult(true);
    }

    /// <summary>
    /// Plays a haptic pattern on a device.
    /// </summary>
    public Task<bool> PlayPatternAsync(
        HapticDevice device,
        HapticPattern pattern,
        float scale,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Playing haptic pattern {PatternId} on device {DeviceId}, scale: {Scale}",
            pattern.PatternId, device.DeviceId, scale);

        if (pattern.Steps.Count == 0)
        {
            _logger.LogWarning("Pattern {PatternId} has no steps", pattern.PatternId);
            return Task.FromResult(false);
        }

        // Simulate playing pattern
        return Task.FromResult(true);
    }
}
