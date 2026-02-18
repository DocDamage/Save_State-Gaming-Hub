namespace SaveState.Application.Mugen.Services.EmergingTechnologies.Engines;

using Microsoft.Extensions.Logging;
using SaveState.Application.Mugen.Models.EmergingTech;
using SaveState.Core.Common.Services;

/// <summary>
/// Engine for processing biometric data including eye tracking and brainwaves.
/// </summary>
public class BiometricEngine
{
    private readonly ILogger<BiometricEngine> _logger;
    private readonly ITimeProvider _timeProvider;

    public BiometricEngine(ILogger<BiometricEngine> logger, ITimeProvider timeProvider)
    {
        _logger = logger;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Processes biometric input data.
    /// </summary>
    public Task<BiometricData> ProcessBiometricInputAsync(
        BiometricInput input,
        CancellationToken ct = default)
    {
        _logger.LogDebug("Processing biometric input for user {UserId}", input.UserId);

        var data = new BiometricData
        {
            UserId = input.UserId,
            Timestamp = _timeProvider.UtcNow,
            StressLevel = input.Metrics.GetValueOrDefault("stress", 0.5f),
            EngagementLevel = input.Metrics.GetValueOrDefault("engagement", 0.5f),
            FatigueLevel = input.Metrics.GetValueOrDefault("fatigue", 0.2f),
            ExcitementLevel = input.Metrics.GetValueOrDefault("excitement", 0.3f),
            FocusLevel = input.Metrics.GetValueOrDefault("focus", 0.6f)
        };

        return Task.FromResult(data);
    }

    /// <summary>
    /// Processes eye tracking data.
    /// </summary>
    public Task<EyeTrackingData> ProcessEyeTrackingAsync(
        EyeTrackingInput input,
        CancellationToken ct = default)
    {
        _logger.LogDebug("Processing eye tracking for user {UserId}", input.UserId);

        var data = new EyeTrackingData
        {
            UserId = input.UserId,
            Timestamp = _timeProvider.UtcNow,
            GazeX = (input.LeftEye.X + input.RightEye.X) / 2,
            GazeY = (input.LeftEye.Y + input.RightEye.Y) / 2,
            GazeZ = 0,
            FixationDuration = CalculateFixationDuration(input),
            SaccadeVelocity = 0,
            FocusedElement = ""
        };

        return Task.FromResult(data);
    }

    /// <summary>
    /// Processes brainwave data.
    /// </summary>
    public Task<BrainwaveData> ProcessBrainwavesAsync(
        BrainwaveInput input,
        CancellationToken ct = default)
    {
        _logger.LogDebug("Processing brainwave data for user {UserId}", input.UserId);

        var total = input.AlphaWaves + input.BetaWaves + input.ThetaWaves + input.GammaWaves;
        var dominantState = total > 0 ? DetermineDominantState(input, total) : "Unknown";

        var data = new BrainwaveData
        {
            UserId = input.UserId,
            Timestamp = _timeProvider.UtcNow,
            AttentionLevel = CalculateAttentionLevel(input),
            MeditationLevel = input.AlphaWaves / Math.Max(total, 1),
            MentalWorkload = input.BetaWaves / Math.Max(total, 1),
            CognitiveLoad = input.ThetaWaves / Math.Max(total, 1),
            DominantState = dominantState
        };

        return Task.FromResult(data);
    }

    private static float CalculateFixationDuration(EyeTrackingInput input)
    {
        // Simplified fixation calculation
        var leftStable = Math.Abs(input.LeftEye.X) < 0.1f && Math.Abs(input.LeftEye.Y) < 0.1f;
        var rightStable = Math.Abs(input.RightEye.X) < 0.1f && Math.Abs(input.RightEye.Y) < 0.1f;
        return leftStable && rightStable ? 300f : 100f; // milliseconds
    }

    private static float CalculateAttentionLevel(BrainwaveInput input)
    {
        var total = input.AlphaWaves + input.BetaWaves + input.ThetaWaves + input.GammaWaves;
        if (total == 0) return 0.5f;

        // Higher beta and gamma waves indicate attention
        return Math.Min((input.BetaWaves + input.GammaWaves) / total * 2, 1.0f);
    }

    private static string DetermineDominantState(BrainwaveInput input, float total)
    {
        var waves = new Dictionary<string, float>
        {
            ["Alpha"] = input.AlphaWaves,
            ["Beta"] = input.BetaWaves,
            ["Theta"] = input.ThetaWaves,
            ["Gamma"] = input.GammaWaves
        };

        var dominant = waves.OrderByDescending(w => w.Value).First();
        return dominant.Key switch
        {
            "Alpha" => "Relaxed",
            "Beta" => "Focused",
            "Theta" => "Drowsy",
            "Gamma" => "Peak Performance",
            _ => "Unknown"
        };
    }
}
