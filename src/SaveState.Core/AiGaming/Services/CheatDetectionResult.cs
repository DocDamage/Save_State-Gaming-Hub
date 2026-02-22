using SaveState.Core.Common.Services;

namespace SaveState.Core.AiGaming.Services;

public class CheatDetectionResult
{
    public bool IsCheating { get; }
    public double Confidence { get; } // 0.0 to 1.0
    public string? DetectionMethod { get; }
    public IReadOnlyList<long> FlaggedAddresses { get; }
    public string? Reason { get; }
    public DateTime DetectedAt { get; }

    private CheatDetectionResult(bool isCheating, double confidence, string? detectionMethod, IReadOnlyList<long> flaggedAddresses, string? reason, DateTime detectedAt)
    {
        IsCheating = isCheating;
        Confidence = confidence;
        DetectionMethod = detectionMethod;
        FlaggedAddresses = flaggedAddresses;
        Reason = reason;
        DetectedAt = detectedAt;
    }

    public static CheatDetectionResult NoCheating(ITimeProvider timeProvider)
    {
        return new CheatDetectionResult(false, 0.0, null, Array.Empty<long>(), null, timeProvider.UtcNow);
    }

    public static CheatDetectionResult CheatingDetected(double confidence, string detectionMethod, IReadOnlyList<long> flaggedAddresses, string reason, ITimeProvider timeProvider)
    {
        return new CheatDetectionResult(true, confidence, detectionMethod, flaggedAddresses, reason, timeProvider.UtcNow);
    }
}
