using SaveState.Core.Common;

namespace SaveState.Core.Assistant.Services;

/// <summary>
/// Monitors eye-tracking state for Smart Pause and accessibility scenarios.
/// </summary>
public interface IEyeTrackingMonitor
{
    /// <summary>
    /// Gets whether eye tracking is available on the current machine.
    /// </summary>
    bool IsAvailable { get; }

    /// <summary>
    /// Gets whether monitoring is currently active.
    /// </summary>
    bool IsMonitoring { get; }

    /// <summary>
    /// Starts eye-tracking monitoring.
    /// </summary>
    Task<Result> StartMonitoringAsync(CancellationToken ct = default);

    /// <summary>
    /// Stops eye-tracking monitoring.
    /// </summary>
    Task<Result> StopMonitoringAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets the most recent eye-tracking snapshot.
    /// </summary>
    Task<Result<EyeTrackingSnapshot>> GetSnapshotAsync(CancellationToken ct = default);
}

/// <summary>
/// Represents a sampled eye-tracking state for Smart Pause analysis.
/// </summary>
public sealed record EyeTrackingSnapshot(
    DateTime CapturedAtUtc,
    bool IsLookingAtScreen,
    int LookAwayDurationSeconds,
    float Confidence,
    string Source);
