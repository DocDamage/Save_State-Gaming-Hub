using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Monitoring;
using SaveState.Core.Performance.Services;
using System.Diagnostics;

namespace SaveState.Infrastructure.Performance;

public class PerformanceMonitor : IPerformanceMonitor
{
    private readonly ILogger<PerformanceMonitor> _logger;
    private readonly IApplicationMetrics _metrics;
    private readonly ICachePerformanceMonitor _cacheMonitor;

    private Process? _monitoredProcess;
    private Timer? _monitoringTimer;
    private Guid _currentSessionId;
    private readonly List<PerformanceSnapshot> _snapshots = new();
    private readonly object _snapshotsLock = new();

    public event EventHandler<PerformanceSnapshot>? SnapshotUpdated;

    public bool IsMonitoring => _monitoringTimer != null;

    public PerformanceMonitor(
        ILogger<PerformanceMonitor> logger,
        IApplicationMetrics metrics,
        ICachePerformanceMonitor cacheMonitor)
    {
        _logger = logger;
        _metrics = metrics;
        _cacheMonitor = cacheMonitor;
    }

    public async Task<Result> StartMonitoringAsync(int processId, CancellationToken ct = default)
    {
        try
        {
            if (IsMonitoring)
                return Result.Failure("Already monitoring a process", ErrorType.Validation);

            _monitoredProcess = Process.GetProcessById(processId);
            if (_monitoredProcess == null)
                return Result.Failure("Process not found", ErrorType.NotFound);

            _currentSessionId = Guid.NewGuid();
            _snapshots.Clear();

            // Start monitoring timer (every 100ms for real-time updates)
            _monitoringTimer = new Timer(
                callback: _ => Task.Run(() => TakeSnapshotAsync(ct)),
                state: null,
                dueTime: 0,
                period: 100);

            _logger.LogInformation("Started performance monitoring for process {ProcessId}, session {SessionId}",
                processId, _currentSessionId);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start monitoring process {ProcessId}", processId);
            return Result.Failure($"Failed to start monitoring: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result> StopMonitoringAsync(CancellationToken ct = default)
    {
        try
        {
            if (!IsMonitoring)
                return Result.Failure("Not currently monitoring", ErrorType.Validation);

            _monitoringTimer?.Dispose();
            _monitoringTimer = null;

            var sessionId = _currentSessionId;
            _currentSessionId = Guid.Empty;
            _monitoredProcess = null;

            _logger.LogInformation("Stopped performance monitoring for session {SessionId}", sessionId);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop monitoring");
            return Result.Failure($"Failed to stop monitoring: {ex.Message}", ErrorType.Internal);
        }
    }

    public PerformanceSnapshot? GetCurrentSnapshot()
    {
        lock (_snapshotsLock)
        {
            return _snapshots.LastOrDefault();
        }
    }

    public async Task<Result<PerformanceHistory>> GetSessionHistoryAsync(Guid sessionId, CancellationToken ct = default)
    {
        try
        {
            IReadOnlyList<PerformanceSnapshot> sessionSnapshots;

            lock (_snapshotsLock)
            {
                // If requesting current session, return current snapshots
                if (sessionId == _currentSessionId)
                {
                    sessionSnapshots = _snapshots.ToList();
                }
                else
                {
                    // In a real implementation, this would load historical data from storage
                    return Result<PerformanceHistory>.Failure("Historical session data not implemented", ErrorType.NotImplemented);
                }
            }

            if (!sessionSnapshots.Any())
                return Result<PerformanceHistory>.Failure("No snapshots available for session", ErrorType.NotFound);

            var history = CalculatePerformanceHistory(sessionId, sessionSnapshots);
            return Result<PerformanceHistory>.Success(history);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get session history for {SessionId}", sessionId);
            return Result<PerformanceHistory>.Failure($"Failed to get history: {ex.Message}", ErrorType.Internal);
        }
    }

    private async Task TakeSnapshotAsync(CancellationToken ct)
    {
        try
        {
            if (_monitoredProcess == null || _monitoredProcess.HasExited)
            {
                await StopMonitoringAsync(ct);
                return;
            }

            // Gather performance metrics
            var timestamp = DateTime.UtcNow;

            // FPS calculation (placeholder - would integrate with game overlay)
            var fps = await GetCurrentFpsAsync(ct);
            var frameTimeMs = fps > 0 ? 1000f / fps : 0f;

            // CPU and memory usage
            var cpuUsage = await GetCpuUsageAsync(ct);
            var ramUsageMb = await GetRamUsageAsync(ct);

            // GPU metrics (placeholder - would use LibreHardwareMonitor or similar)
            var gpuUsage = await GetGpuUsageAsync(ct);
            var gpuTemp = await GetGpuTemperatureAsync(ct);
            var cpuTemp = await GetCpuTemperatureAsync(ct);

            var snapshot = new PerformanceSnapshot(
                Timestamp: timestamp,
                Fps: fps,
                FrameTimeMs: frameTimeMs,
                CpuUsagePercent: cpuUsage,
                GpuUsagePercent: gpuUsage,
                RamUsageMb: ramUsageMb,
                GpuTempCelsius: gpuTemp,
                CpuTempCelsius: cpuTemp);

            lock (_snapshotsLock)
            {
                _snapshots.Add(snapshot);

                // Keep only last 1000 snapshots to prevent memory issues
                if (_snapshots.Count > 1000)
                {
                    _snapshots.RemoveAt(0);
                }
            }

            // Update application metrics
            _metrics.RecordPerformanceSnapshot(snapshot);

            // Notify subscribers
            SnapshotUpdated?.Invoke(this, snapshot);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to take performance snapshot");
        }
    }

    private static PerformanceHistory CalculatePerformanceHistory(Guid sessionId, IReadOnlyList<PerformanceSnapshot> snapshots)
    {
        if (!snapshots.Any())
        {
            return new PerformanceHistory(
                SessionId: sessionId,
                AverageFps: 0,
                MinFps: 0,
                MaxFps: 0,
                OnePercentLow: 0,
                PointOnePercentLow: 0,
                Snapshots: Array.Empty<PerformanceSnapshot>());
        }

        var fpsValues = snapshots.Select(s => s.Fps).Where(f => f > 0).ToList();

        if (!fpsValues.Any())
        {
            return new PerformanceHistory(
                SessionId: sessionId,
                AverageFps: 0,
                MinFps: 0,
                MaxFps: 0,
                OnePercentLow: 0,
                PointOnePercentLow: 0,
                Snapshots: snapshots);
        }

        var averageFps = fpsValues.Average();
        var minFps = fpsValues.Min();
        var maxFps = fpsValues.Max();

        // Calculate percentile lows (sorted in ascending order)
        var sortedFps = fpsValues.OrderBy(f => f).ToList();
        var onePercentIndex = (int)(sortedFps.Count * 0.01);
        var pointOnePercentIndex = (int)(sortedFps.Count * 0.001);

        var onePercentLow = sortedFps[Math.Max(0, sortedFps.Count - 1 - onePercentIndex)];
        var pointOnePercentLow = sortedFps[Math.Max(0, sortedFps.Count - 1 - pointOnePercentIndex)];

        return new PerformanceHistory(
            SessionId: sessionId,
            AverageFps: averageFps,
            MinFps: minFps,
            MaxFps: maxFps,
            OnePercentLow: onePercentLow,
            PointOnePercentLow: pointOnePercentLow,
            Snapshots: snapshots);
    }

    private Task<float> GetCurrentFpsAsync(CancellationToken ct)
    {
        // Placeholder implementation
        // In a real implementation, this would:
        // - Use PresentMon or similar to capture frame times
        // - Integrate with game overlay APIs
        // - Hook into DirectX/OpenGL frame presentation

        // Simulate FPS between 30-120
        return Task.FromResult((float)Random.Shared.Next(30, 120));
    }

    private async Task<float> GetCpuUsageAsync(CancellationToken ct)
    {
        try
        {
            if (_monitoredProcess == null) return 0;

            // Get CPU usage for the monitored process
            var startTime = DateTime.UtcNow;
            var startCpuUsage = Process.GetCurrentProcess().TotalProcessorTime;

            await Task.Delay(50, ct); // Small delay for measurement

            var endTime = DateTime.UtcNow;
            var endCpuUsage = Process.GetCurrentProcess().TotalProcessorTime;

            var cpuUsedMs = (endCpuUsage - startCpuUsage).TotalMilliseconds;
            var totalMsPassed = (endTime - startTime).TotalMilliseconds;

            var cpuUsageTotal = cpuUsedMs / (Environment.ProcessorCount * totalMsPassed) * 100;
            return (float)Math.Min(cpuUsageTotal, 100.0);
        }
        catch
        {
            return 0;
        }
    }

    private Task<long> GetRamUsageAsync(CancellationToken ct)
    {
        try
        {
            if (_monitoredProcess == null) return Task.FromResult(0L);

            _monitoredProcess.Refresh();
            return Task.FromResult(_monitoredProcess.WorkingSet64 / 1024 / 1024); // Convert to MB
        }
        catch
        {
            return Task.FromResult(0L);
        }
    }

    private Task<float> GetGpuUsageAsync(CancellationToken ct)
    {
        // Placeholder - would integrate with LibreHardwareMonitor or similar
        // For now, return simulated GPU usage
        return Task.FromResult((float)Random.Shared.Next(10, 90));
    }

    private Task<float?> GetGpuTemperatureAsync(CancellationToken ct)
    {
        // Placeholder - would integrate with LibreHardwareMonitor
        // Return simulated temperature between 40-80°C
        return Task.FromResult((float?)Random.Shared.Next(40, 80));
    }

    private Task<float?> GetCpuTemperatureAsync(CancellationToken ct)
    {
        // Placeholder - would integrate with LibreHardwareMonitor
        // Return simulated temperature between 35-70°C
        return Task.FromResult((float?)Random.Shared.Next(35, 70));
    }
}
