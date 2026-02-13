using Microsoft.Extensions.Logging;
using SaveState.Core.Assistant.Services;
using SaveState.Core.Common;

namespace SaveState.Infrastructure.Assistant;

/// <summary>
/// Fallback eye-tracking monitor used on platforms without eye-tracking support.
/// </summary>
public sealed class NoOpEyeTrackingMonitor : IEyeTrackingMonitor
{
    private readonly ILogger<NoOpEyeTrackingMonitor> _logger;

    public NoOpEyeTrackingMonitor(
        ILogger<NoOpEyeTrackingMonitor> logger)
    {
        _logger = logger;
    }

    public bool IsAvailable => false;

    public bool IsMonitoring => false;

    public Task<Result> StartMonitoringAsync(CancellationToken ct = default)
    {
        _logger.LogDebug("Eye-tracking monitor is unavailable on this platform");
        return Task.FromResult(Result.Failure(
            "Eye tracking is unavailable on this platform.",
            ErrorType.NotImplemented));
    }

    public Task<Result> StopMonitoringAsync(CancellationToken ct = default)
    {
        return Task.FromResult(Result.Success());
    }

    public Task<Result<EyeTrackingSnapshot>> GetSnapshotAsync(CancellationToken ct = default)
    {
        return Task.FromResult(Result.Failure<EyeTrackingSnapshot>(
            "Eye tracking is unavailable on this platform.",
            ErrorType.NotImplemented));
    }
}
