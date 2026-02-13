using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Monitoring;
using SaveState.Core.Performance.Services;
using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;

namespace SaveState.Infrastructure.Performance;

public class PerformanceMonitor : IPerformanceMonitor
{
    private readonly ILogger<PerformanceMonitor> _logger;
    private readonly IApplicationMetrics _metrics;
    private readonly ICachePerformanceMonitor _cacheMonitor;

    private Process? _monitoredProcess;
    private Timer? _monitoringTimer;
    private Guid? _currentSessionId;
    private readonly List<PerformanceSnapshot> _snapshots = new();
    private readonly object _snapshotsLock = new();
    private readonly Dictionary<Guid, List<PerformanceSnapshot>> _sessionHistory = new();
    private DateTime? _lastCpuSampleTime;
    private TimeSpan? _lastCpuTotalProcessorTime;

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
            List<PerformanceSnapshot> sessionSnapshots;
            lock (_snapshotsLock)
            {
                sessionSnapshots = _snapshots.ToList();
            }

            if (sessionId.HasValue && sessionSnapshots.Count > 0)
            {
                lock (_snapshotsLock)
                {
                    _sessionHistory[sessionId.Value] = sessionSnapshots;
                }
            }

            _currentSessionId = null;
            _monitoredProcess = null;
            _lastCpuSampleTime = null;
            _lastCpuTotalProcessorTime = null;

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
                if (sessionId == _currentSessionId)
                {
                    sessionSnapshots = _snapshots.ToList();
                }
                else if (_sessionHistory.TryGetValue(sessionId, out var historical))
                {
                    sessionSnapshots = historical.ToList();
                }
                else
                {
                    return Result.Failure<PerformanceHistory>("No snapshots available for session", ErrorType.NotFound);
                }
            }

            if (!sessionSnapshots.Any())
                return Result.Failure<PerformanceHistory>("No snapshots available for session", ErrorType.NotFound);

            var history = CalculatePerformanceHistory(sessionId, sessionSnapshots);
            return Result.Success<PerformanceHistory>(history);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get session history for {SessionId}", sessionId);
            return Result.Failure<PerformanceHistory>($"Failed to get history: {ex.Message}", ErrorType.Internal);
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
            if (_monitoredProcess == null || _monitoredProcess.HasExited) return 0;

            _monitoredProcess.Refresh();
            var now = DateTime.UtcNow;
            var totalProcessorTime = _monitoredProcess.TotalProcessorTime;

            if (_lastCpuSampleTime == null || _lastCpuTotalProcessorTime == null)
            {
                _lastCpuSampleTime = now;
                _lastCpuTotalProcessorTime = totalProcessorTime;
                return 0;
            }

            var cpuUsedMs = (totalProcessorTime - _lastCpuTotalProcessorTime.Value).TotalMilliseconds;
            var totalMsPassed = (now - _lastCpuSampleTime.Value).TotalMilliseconds;

            _lastCpuSampleTime = now;
            _lastCpuTotalProcessorTime = totalProcessorTime;

            if (totalMsPassed <= 0)
                return 0;

            var cpuUsageTotal = cpuUsedMs / (Environment.ProcessorCount * totalMsPassed) * 100;
            return (float)Math.Clamp(cpuUsageTotal, 0.0, 100.0);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to calculate CPU usage. Returning 0 as fallback.");
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
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve RAM usage metrics. Returning 0 as fallback.");
            return Task.FromResult(0L);
        }
    }

    private async Task<float?> GetGpuUsageAsync(CancellationToken ct)
    {
        try
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || _monitoredProcess == null)
                return null;

            return await Task.Run(async () =>
            {
                try
                {
                    var category = new PerformanceCounterCategory("GPU Engine");
                    var instanceNames = category.GetInstanceNames()
                        .Where(name => name.Contains($"pid_{_monitoredProcess.Id}", StringComparison.OrdinalIgnoreCase) &&
                                       name.Contains("engtype_3D", StringComparison.OrdinalIgnoreCase))
                        .ToArray();

                    if (instanceNames.Length == 0)
                        return (float?)null;

                    float total = 0;
                    foreach (var instance in instanceNames)
                    {
                        using var counter = new PerformanceCounter("GPU Engine", "Utilization Percentage", instance);
                        _ = counter.NextValue(); // Prime counter
                        await Task.Delay(20, ct);
                        total += counter.NextValue();
                    }

                    return (float?)total;
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "GPU usage counters unavailable");
                    _metrics.IncrementCounter("performance.gpu_usage.unavailable");
                    return null;
                }
            }, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve GPU usage metrics.");
            _metrics.RecordException("PerformanceMonitor.GetGpuUsageAsync", ex.GetType().Name, ex.Message);
            _metrics.IncrementCounter("performance.gpu_usage.failure");
            return null;
        }
    }

    private Task<float?> GetGpuTemperatureAsync(CancellationToken ct)
    {
        // GPU temperature retrieval requires vendor-specific APIs; return null when unavailable.
        return Task.FromResult<float?>(null);
    }

    private Task<float?> GetCpuTemperatureAsync(CancellationToken ct)
    {
        try
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return Task.FromResult<float?>(null);

            using var searcher = new ManagementObjectSearcher(@"root\\WMI", "SELECT CurrentTemperature FROM MSAcpi_ThermalZoneTemperature");
            foreach (var obj in searcher.Get())
            {
                if (obj["CurrentTemperature"] is uint rawTemp && rawTemp > 0)
                {
                    // Value is in tenths of Kelvin
                    var celsius = (rawTemp / 10f) - 273.15f;
                    return Task.FromResult<float?>(celsius);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "CPU temperature unavailable");
        }

        return Task.FromResult<float?>(null);
    }
}
