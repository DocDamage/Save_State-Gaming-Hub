using SaveState.Core.Common;

namespace SaveState.Core.Performance.Services;

public interface IPerformanceMonitor
{
    Task<Result> StartMonitoringAsync(int processId, CancellationToken ct = default);
    Task<Result> StopMonitoringAsync(CancellationToken ct = default);
    bool IsMonitoring { get; }
    PerformanceSnapshot? GetCurrentSnapshot();
    Task<Result<PerformanceHistory>> GetSessionHistoryAsync(Guid sessionId, CancellationToken ct = default);
    event EventHandler<PerformanceSnapshot>? SnapshotUpdated;
}

public sealed record PerformanceSnapshot(
    DateTime Timestamp,
    float Fps,
    float FrameTimeMs,
    float CpuUsagePercent,
    float? GpuUsagePercent,
    long RamUsageMb,
    float? GpuTempCelsius,
    float? CpuTempCelsius);

public sealed record PerformanceHistory(
    Guid SessionId,
    float AverageFps,
    float MinFps,
    float MaxFps,
    float OnePercentLow,
    float PointOnePercentLow,
    IReadOnlyList<PerformanceSnapshot> Snapshots);
