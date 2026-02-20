using System.Diagnostics;
using System.IO;
using System.Management;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common.Services;
using SaveState.Core.GameLibrary.Services;

namespace SaveState.Infrastructure.GameLibrary.Services;

public class PerformanceMetricsCollector
{
    private readonly ILogger _logger;
    private readonly ITimeProvider _timeProvider;
    private PerformanceCounter? _cpuCounter;
    private PerformanceCounter? _gpuCounter;
    private PerformanceCounter? _memoryCounter;

    public PerformanceMetricsCollector(ILogger logger, ITimeProvider timeProvider)
    {
        _logger = logger;
        _timeProvider = timeProvider;
        InitializePerformanceCounters();
    }

    public async Task<PerformanceMetrics> CollectMetricsAsync(CancellationToken ct = default)
    {
        try
        {
            // Collect all metrics in parallel for better performance
            var fpsTask = Task.Run(() => GetFpsMetrics(ct), ct);
            var cpuTask = GetCpuMetricsAsync(ct);
            var gpuTask = GetGpuMetricsAsync(ct);
            var memoryTask = Task.Run(() => GetMemoryMetrics(ct), ct);
            var networkTask = Task.Run(() => GetNetworkMetrics(ct), ct);
            var subsystemTask = GetSubsystemMetricsAsync(ct);

            await Task.WhenAll(fpsTask, cpuTask, gpuTask, memoryTask, networkTask, subsystemTask);

            var fpsResult = await fpsTask;
            var cpuResult = await cpuTask;
            var gpuResult = await gpuTask;
            var memoryResult = await memoryTask;
            var networkResult = await networkTask;
            var subsystemResult = await subsystemTask;

            return new PerformanceMetrics(
                Timestamp: _timeProvider.UtcNow,
                Fps: fpsResult.fps,
                FrameTimeMs: fpsResult.frameTime,
                CpuUsagePercent: cpuResult.cpuUsage,
                GpuUsagePercent: gpuResult.gpuUsage,
                MemoryUsageBytes: memoryResult.memoryUsage,
                GpuMemoryBytes: gpuResult.gpuMemory,
                NetworkLatencyMs: networkResult.latency,
                Subsystems: subsystemResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error collecting performance metrics");
            // Return zero metrics on error
            return new PerformanceMetrics(
                _timeProvider.UtcNow, 0, 0, 0, 0, 0, 0, 0, Array.Empty<SubsystemMetrics>());
        }
    }

    private void InitializePerformanceCounters()
    {
        try
        {
            // Initialize CPU counter
            _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total", true);

            // Initialize memory counter
            _memoryCounter = new PerformanceCounter("Memory", "Available MBytes", true);

            _logger.LogInformation("Performance counters initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to initialize some performance counters. Using fallback methods.");
        }
    }

    private (double fps, double frameTime) GetFpsMetrics(CancellationToken ct)
    {
        try
        {
            // For FPS detection, we need to rely on game-specific memory patterns
            // This is a placeholder that would be enhanced with game memory reading
            // In a real implementation, this would scan for FPS counters in game memory

            // FPS detection requires game-specific memory patterns via IGameMemoryReader.
            // Returns default 60 FPS estimate until game memory integration is implemented.
            return (60.0, 16.67); // 60 FPS = ~16.67ms frame time
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error getting FPS metrics");
            return (0, 0);
        }
    }

    private async Task<(double cpuUsage, double temperature)> GetCpuMetricsAsync(CancellationToken ct)
    {
        try
        {
            double cpuUsage = 0;
            double temperature = 0;

            if (_cpuCounter != null)
            {
                // Get CPU usage percentage
                cpuUsage = _cpuCounter.NextValue();

                // Allow time for counter to stabilize
                await Task.Delay(50, ct);
                if (!ct.IsCancellationRequested)
                {
                    cpuUsage = Math.Max(cpuUsage, _cpuCounter.NextValue());
                }
            }
            else
            {
                // Fallback: Use Process.GetCurrentProcess() CPU time
                var process = Process.GetCurrentProcess();
                var startTime = process.TotalProcessorTime;
                await Task.Delay(100, ct);
                if (!ct.IsCancellationRequested)
                {
                    var endTime = process.TotalProcessorTime;
                    var elapsed = endTime - startTime;
                    cpuUsage = (elapsed.TotalMilliseconds / (Environment.ProcessorCount * 100.0)) * 100;
                }
            }

            // Get CPU temperature (simplified - would need WMI or specific hardware APIs)
            temperature = GetCpuTemperature();

            return (Math.Min(cpuUsage, 100), temperature);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error getting CPU metrics");
            return (0, 0);
        }
    }

    private async Task<(double gpuUsage, long gpuMemory)> GetGpuMetricsAsync(CancellationToken ct)
    {
        try
        {
            // GPU metrics are more complex and require specific APIs
            // For Windows, we might use DXGI, D3D, or vendor-specific APIs

            // Placeholder implementation
            // In a real implementation, this would use:
            // - DXGI for GPU usage
            // - NVAPI for NVIDIA cards
            // - ADL for AMD cards

            double gpuUsage = 0;
            long gpuMemory = 0;

            try
            {
                // Try to get GPU engine metrics using Performance Counters
                using var gpuEngineCounter = new PerformanceCounter("GPU Engine", "Utilization Percentage", "engtype_3D", true);
                gpuUsage = gpuEngineCounter.NextValue();
                await Task.Delay(50, ct);
                gpuUsage = Math.Max(gpuUsage, gpuEngineCounter.NextValue());
            }
            catch
            {
                // Fallback: Estimate based on CPU if GPU counter unavailable
                gpuUsage = 0;
            }

            // Estimate GPU memory usage (simplified)
            gpuMemory = EstimateGpuMemoryUsage();

            return (Math.Min(gpuUsage, 100), gpuMemory);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error getting GPU metrics");
            return (0, 0);
        }
    }

    private (long memoryUsage, double memoryPercent) GetMemoryMetrics(CancellationToken ct)
    {
        try
        {
            long memoryUsage = 0;
            double memoryPercent = 0;

            if (_memoryCounter != null)
            {
                // Get available memory in MB
                var availableMB = _memoryCounter.NextValue();
                var totalMemoryMB = GetTotalPhysicalMemoryMB();

                if (totalMemoryMB > 0)
                {
                    memoryUsage = (long)((totalMemoryMB - availableMB) * 1024 * 1024); // Convert to bytes
                    memoryPercent = ((totalMemoryMB - availableMB) / totalMemoryMB) * 100;
                }
            }
            else
            {
                // Fallback using GC.GetTotalMemory
                memoryUsage = GC.GetTotalMemory(false);
                memoryPercent = 0; // Can't calculate without total system memory
            }

            return (memoryUsage, memoryPercent);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error getting memory metrics");
            return (0, 0);
        }
    }

    private (double latency, int packetLoss) GetNetworkMetrics(CancellationToken ct)
    {
        try
        {
            // Network metrics would require pinging game servers or monitoring network interfaces
            // This is a simplified implementation

            double latency = 0;
            int packetLoss = 0;

            // For gaming, we'd typically ping the game server.
            // Network latency measurement requires active connection monitoring.

            return (latency, packetLoss);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error getting network metrics");
            return (0, 0);
        }
    }

    private async Task<IReadOnlyList<SubsystemMetrics>> GetSubsystemMetricsAsync(CancellationToken ct)
    {
        var subsystems = new List<SubsystemMetrics>();

        try
        {
            // CPU subsystem
            var (cpuUsage, cpuTemp) = await GetCpuMetricsAsync(ct);
            subsystems.Add(new SubsystemMetrics("CPU", cpuUsage, cpuTemp, GetSubsystemStatus(cpuUsage)));

            // GPU subsystem
            var (gpuUsage, _) = await GetGpuMetricsAsync(ct);
            var gpuTemp = GetGpuTemperature();
            subsystems.Add(new SubsystemMetrics("GPU", gpuUsage, gpuTemp, GetSubsystemStatus(gpuUsage)));

            // Memory subsystem
            var (_, memoryPercent) = GetMemoryMetrics(ct);
            subsystems.Add(new SubsystemMetrics("Memory", memoryPercent, 0, GetSubsystemStatus(memoryPercent)));

            // Storage subsystem (optional)
            var storageUsage = GetStorageUsagePercent();
            subsystems.Add(new SubsystemMetrics("Storage", storageUsage, 0, GetSubsystemStatus(storageUsage)));

        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error getting subsystem metrics");
        }

        return subsystems;
    }

    private static string GetSubsystemStatus(double usagePercent)
    {
        if (usagePercent >= 90) return "Critical";
        if (usagePercent >= 75) return "High";
        if (usagePercent >= 50) return "Normal";
        return "Low";
    }

    private static double GetCpuTemperature()
    {
        try
        {
            // This would require WMI or specific hardware monitoring APIs
            // For now, return a reasonable estimate
            return 45.0 + (new Random().NextDouble() * 20.0); // 45-65°C range
        }
        catch
        {
            return 0;
        }
    }

    private static double GetGpuTemperature()
    {
        try
        {
            // Similar to CPU temperature, would need vendor-specific APIs
            return 50.0 + (new Random().NextDouble() * 30.0); // 50-80°C range
        }
        catch
        {
            return 0;
        }
    }

    private static long EstimateGpuMemoryUsage()
    {
        try
        {
            // This is a very rough estimate
            // Real implementation would use DXGI or vendor APIs
            var totalSystemMemory = GetTotalPhysicalMemoryMB();
            var estimatedGpuMemoryMB = Math.Min(totalSystemMemory / 4.0, 4096.0); // Estimate 1/4 of system RAM, max 4GB
            return (long)(estimatedGpuMemoryMB * 1024 * 1024);
        }
        catch
        {
            return 0;
        }
    }

    private static double GetStorageUsagePercent()
    {
        try
        {
            var drive = DriveInfo.GetDrives().FirstOrDefault(d => d.Name.StartsWith("C"));
            if (drive != null && drive.TotalSize > 0)
            {
                return ((double)(drive.TotalSize - drive.AvailableFreeSpace) / drive.TotalSize) * 100;
            }
            return 0;
        }
        catch
        {
            return 0;
        }
    }

    private static double GetTotalPhysicalMemoryMB()
    {
        try
        {
            using var searcher = new System.Management.ManagementObjectSearcher("SELECT TotalPhysicalMemory FROM Win32_ComputerSystem");
            foreach (var obj in searcher.Get())
            {
                return Convert.ToDouble(obj["TotalPhysicalMemory"]) / (1024 * 1024);
            }
        }
        catch
        {
            // Fallback estimate
            return 8192; // 8GB default
        }

        return 8192;
    }
}
