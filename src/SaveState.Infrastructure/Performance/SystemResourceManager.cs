using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Performance.Services;
using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;

namespace SaveState.Infrastructure.Performance;

/// <summary>
/// Service for managing system resources and optimizing for gaming performance.
/// </summary>
public class SystemResourceManager : ISystemResourceManager
{
    private readonly ILogger<SystemResourceManager> _logger;
    private OptimizationState _currentState = OptimizationState.Normal;
    private OptimizationProfile? _appliedProfile;
    private readonly List<ProcessRestoreInfo> _closedProcesses = new();
    private readonly object _stateLock = new();

    // Well-known processes that are safe to terminate for gaming optimization
    private static readonly HashSet<string> SafeToTerminateProcesses = new(StringComparer.OrdinalIgnoreCase)
    {
        // Browsers
        "chrome", "firefox", "msedge", "opera", "brave", "vivaldi",
        // Communication
        "discord", "slack", "teams", "zoom", "skype",
        // Media
        "spotify", "vlc", "wmplayer", "groove",
        // Utilities
        "dropbox", "onedrive", "googledrivesync", "icloud",
        // Development (user may want these closed)
        "code", "devenv", "rider", "webstorm", "phpstorm",
        // Misc
        "steam", "epicgameslauncher", "gog", "origin", "uplay"
    };

    // System-critical processes that should never be terminated
    private static readonly HashSet<string> CriticalProcesses = new(StringComparer.OrdinalIgnoreCase)
    {
        "system", "svchost", "csrss", "wininit", "services", "lsass",
        "smss", "dwm", "explorer", "ctfmon", "taskhostw", "sihost",
        "fontdrvhost", "audiodg", "spoolsv", "searchindexer",
        "securityhealthservice", "windowsdefender", "msmpeng"
    };

    public event EventHandler<OptimizationStateChangedEventArgs>? StateChanged;

    public OptimizationState CurrentState
    {
        get
        {
            lock (_stateLock)
            {
                return _currentState;
            }
        }
    }

    public SystemResourceManager(ILogger<SystemResourceManager> logger)
    {
        _logger = logger;
    }

    public async Task<Result<SystemAnalysis>> AnalyzeSystemAsync(CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Analyzing system resources...");

            var processes = await GetBackgroundProcessesAsync(ct);
            if (!processes.IsSuccess || processes.Value is null)
            {
                return Result.Failure<SystemAnalysis>(processes.Error ?? "Unknown error", processes.ErrorType);
            }

            var terminableProcesses = processes.Value
                .Where(p => p.IsSafeToTerminate)
                .ToList();

            // Get system memory info
            var memoryInfo = GetMemoryInfo();
            var cpuInfo = await GetCpuInfoAsync(ct);

            // Determine recommended optimization level
            var recommendedLevel = DetermineRecommendedLevel(
                memoryInfo.AvailableMb,
                memoryInfo.TotalMb,
                cpuInfo.UsagePercent);

            var recommendations = GenerateRecommendations(terminableProcesses, memoryInfo, cpuInfo);

            var analysis = new SystemAnalysis(
                TerminableProcesses: terminableProcesses,
                AvailableRamMb: memoryInfo.AvailableMb,
                TotalRamMb: memoryInfo.TotalMb,
                CpuUsagePercent: cpuInfo.UsagePercent,
                CpuHeadroom: 100 - cpuInfo.UsagePercent,
                GpuUsagePercent: await GetGpuUsageAsync(ct),
                RecommendedLevel: recommendedLevel,
                Recommendations: recommendations);

            _logger.LogInformation(
                "System analysis complete: {AvailableRam}MB RAM available, {CpuHeadroom}% CPU headroom, {ProcessCount} terminable processes",
                analysis.AvailableRamMb, analysis.CpuHeadroom, terminableProcesses.Count);

            return Result.Success<SystemAnalysis>(analysis);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to analyze system");
            return Result.Failure<SystemAnalysis>($"Failed to analyze system: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result> ApplyOptimizationAsync(OptimizationProfile profile, CancellationToken ct = default)
    {
        try
        {
            lock (_stateLock)
            {
                if (_currentState == OptimizationState.Optimized)
                {
                    return Result.Failure("System is already optimized. Call RestoreSystemAsync first.", ErrorType.Validation);
                }

                SetState(OptimizationState.Optimizing, null);
            }

            _logger.LogInformation("Applying optimization profile: {Name} (Level: {Level})",
                profile.Name, profile.Level);

            _closedProcesses.Clear();

            // Close specified processes
            foreach (var processName in profile.ProcessesToClose)
            {
                await CloseProcessAsync(processName, ct);
            }

            // Close processes based on optimization level
            if (profile.Level >= OptimizationLevel.Standard)
            {
                await CloseProcessesByCategoryAsync(ProcessCategory.Browser, ct);
                await CloseProcessesByCategoryAsync(ProcessCategory.Media, ct);
            }

            if (profile.Level >= OptimizationLevel.Aggressive)
            {
                await CloseProcessesByCategoryAsync(ProcessCategory.Communication, ct);
                await CloseProcessesByCategoryAsync(ProcessCategory.Utility, ct);
            }

            // Set high performance power plan if requested
            if (profile.SetHighPerformancePowerPlan)
            {
                await SetHighPerformancePowerPlanAsync(ct);
            }

            // Disable fullscreen optimizations if requested
            if (profile.DisableFullscreenOptimizations)
            {
                _logger.LogDebug("Fullscreen optimizations setting noted (game-specific setting)");
            }

            lock (_stateLock)
            {
                _appliedProfile = profile;
                SetState(OptimizationState.Optimized, profile);
            }

            _logger.LogInformation("Optimization applied successfully. Closed {Count} processes.",
                _closedProcesses.Count);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply optimization");
            lock (_stateLock)
            {
                SetState(OptimizationState.Normal, null);
            }
            return Result.Failure($"Failed to apply optimization: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result> RestoreSystemAsync(CancellationToken ct = default)
    {
        try
        {
            lock (_stateLock)
            {
                if (_currentState != OptimizationState.Optimized)
                {
                    return Result.Failure("System is not currently optimized", ErrorType.Validation);
                }

                SetState(OptimizationState.Restoring, _appliedProfile);
            }

            _logger.LogInformation("Restoring system to normal state...");

            // Restart previously closed processes
            var restoreErrors = new List<string>();
            foreach (var processInfo in _closedProcesses)
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(processInfo.ExecutablePath) &&
                        File.Exists(processInfo.ExecutablePath))
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = processInfo.ExecutablePath,
                            UseShellExecute = true
                        });

                        _logger.LogDebug("Restarted process: {Name}", processInfo.Name);
                    }
                }
                catch (Exception ex)
                {
                    restoreErrors.Add($"{processInfo.Name}: {ex.Message}");
                    _logger.LogWarning(ex, "Failed to restart process: {Name}", processInfo.Name);
                }
            }

            _closedProcesses.Clear();

            lock (_stateLock)
            {
                _appliedProfile = null;
                SetState(OptimizationState.Normal, null);
            }

            if (restoreErrors.Any())
            {
                _logger.LogWarning("System restored with {ErrorCount} errors", restoreErrors.Count);
            }
            else
            {
                _logger.LogInformation("System restored to normal state");
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restore system");
            return Result.Failure($"Failed to restore system: {ex.Message}", ErrorType.Internal);
        }
    }

    public Task<Result<IReadOnlyList<BackgroundProcess>>> GetBackgroundProcessesAsync(CancellationToken ct = default)
    {
        try
        {
            var processes = new List<BackgroundProcess>();
            var allProcesses = Process.GetProcesses();

            foreach (var proc in allProcesses)
            {
                try
                {
                    if (proc.Id == Environment.ProcessId) continue;
                    if (CriticalProcesses.Contains(proc.ProcessName)) continue;

                    var category = CategorizeProcess(proc.ProcessName);
                    var isSafe = SafeToTerminateProcesses.Contains(proc.ProcessName) ||
                                 (category != ProcessCategory.System && category != ProcessCategory.Security);

                    string? description = null;
                    try
                    {
                        description = proc.MainModule?.FileVersionInfo.FileDescription;
                    }
                    catch
                    {
                        // Can't access some process info
                    }

                    processes.Add(new BackgroundProcess(
                        ProcessId: proc.Id,
                        Name: proc.ProcessName,
                        Description: description,
                        MemoryUsageMb: proc.WorkingSet64 / 1024 / 1024,
                        CpuUsagePercent: 0, // Would need sampling over time
                        Category: category,
                        IsSafeToTerminate: isSafe));
                }
                catch
                {
                    // Skip processes we can't access
                }
            }

            return Task.FromResult(Result.Success<IReadOnlyList<BackgroundProcess>>(
                (IReadOnlyList<BackgroundProcess>)processes.OrderByDescending(p => p.MemoryUsageMb).ToList()));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get background processes");
            return Task.FromResult(Result.Failure<IReadOnlyList<BackgroundProcess>>(
                $"Failed to get processes: {ex.Message}", ErrorType.Internal));
        }
    }

    private Task CloseProcessAsync(string processName, CancellationToken ct)
    {
        try
        {
            var processes = Process.GetProcessesByName(processName);
            foreach (var proc in processes)
            {
                try
                {
                    string? exePath = null;
                    try
                    {
                        exePath = proc.MainModule?.FileName;
                    }
                    catch
                    {
                        // Can't get path
                    }

                    _closedProcesses.Add(new ProcessRestoreInfo(proc.ProcessName, exePath));

                    proc.CloseMainWindow();
                    if (!proc.WaitForExit(3000))
                    {
                        proc.Kill();
                    }

                    _logger.LogDebug("Closed process: {Name}", processName);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to close process: {Name}", processName);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error closing process: {Name}", processName);
        }

        return Task.CompletedTask;
    }

    private async Task CloseProcessesByCategoryAsync(ProcessCategory category, CancellationToken ct)
    {
        var processesResult = await GetBackgroundProcessesAsync(ct);
        if (!processesResult.IsSuccess || processesResult.Value is null) return;

        var toClose = processesResult.Value
            .Where(p => p.Category == category && p.IsSafeToTerminate)
            .Select(p => p.Name)
            .Distinct();

        foreach (var name in toClose)
        {
            await CloseProcessAsync(name, ct);
        }
    }

    private static readonly Dictionary<string, ProcessCategory> ProcessCategoryKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        // Browser keywords
        { "chrome", ProcessCategory.Browser },
        { "firefox", ProcessCategory.Browser },
        { "edge", ProcessCategory.Browser },
        { "opera", ProcessCategory.Browser },
        { "brave", ProcessCategory.Browser },
        // Communication keywords
        { "discord", ProcessCategory.Communication },
        { "slack", ProcessCategory.Communication },
        { "teams", ProcessCategory.Communication },
        { "zoom", ProcessCategory.Communication },
        { "skype", ProcessCategory.Communication },
        // Media keywords
        { "spotify", ProcessCategory.Media },
        { "vlc", ProcessCategory.Media },
        { "music", ProcessCategory.Media },
        { "media", ProcessCategory.Media },
        // Gaming keywords
        { "steam", ProcessCategory.Gaming },
        { "epic", ProcessCategory.Gaming },
        { "gog", ProcessCategory.Gaming },
        { "origin", ProcessCategory.Gaming },
        { "uplay", ProcessCategory.Gaming },
        // Development keywords
        { "code", ProcessCategory.Development },
        { "devenv", ProcessCategory.Development },
        { "rider", ProcessCategory.Development },
        { "studio", ProcessCategory.Development },
        // System keywords
        { "svc", ProcessCategory.System },
        { "service", ProcessCategory.System },
        { "host", ProcessCategory.System },
        // Security keywords
        { "defender", ProcessCategory.Security },
        { "security", ProcessCategory.Security },
        { "antivirus", ProcessCategory.Security },
        // Utility keywords
        { "dropbox", ProcessCategory.Utility },
        { "onedrive", ProcessCategory.Utility },
        { "backup", ProcessCategory.Utility }
    };

    private static ProcessCategory CategorizeProcess(string processName)
    {
        var name = processName.ToLowerInvariant();

        // Check for exact matches first for better performance
        if (ProcessCategoryKeywords.TryGetValue(name, out var category))
        {
            return category;
        }

        // Fall back to substring matching for partial matches
        foreach (var kvp in ProcessCategoryKeywords)
        {
            if (name.Contains(kvp.Key))
            {
                return kvp.Value;
            }
        }

        return ProcessCategory.Other;
    }

    private static (long AvailableMb, long TotalMb) GetMemoryInfo()
    {
        try
        {
            var gcMemoryInfo = GC.GetGCMemoryInfo();
            var totalMemory = gcMemoryInfo.TotalAvailableMemoryBytes / 1024 / 1024;
            var usedMemory = Process.GetCurrentProcess().WorkingSet64 / 1024 / 1024;

            // Get actual system memory (Windows-specific)
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_OperatingSystem");
                foreach (ManagementObject obj in searcher.Get())
                {
                    var freeMemory = Convert.ToInt64(obj["FreePhysicalMemory"]) / 1024;
                    var totalMem = Convert.ToInt64(obj["TotalVisibleMemorySize"]) / 1024;
                    return (freeMemory, totalMem);
                }
            }

            return (totalMemory - usedMemory, totalMemory);
        }
        catch
        {
            return (0, 0);
        }
    }

    private async Task<(float UsagePercent, int Cores)> GetCpuInfoAsync(CancellationToken ct)
    {
        try
        {
            var startTime = DateTime.UtcNow;
            var startCpu = Process.GetCurrentProcess().TotalProcessorTime;

            await Task.Delay(100, ct);

            var endTime = DateTime.UtcNow;
            var endCpu = Process.GetCurrentProcess().TotalProcessorTime;

            var cpuUsed = (endCpu - startCpu).TotalMilliseconds;
            var elapsed = (endTime - startTime).TotalMilliseconds;
            var usage = (float)(cpuUsed / (Environment.ProcessorCount * elapsed) * 100);

            return (Math.Min(usage, 100), Environment.ProcessorCount);
        }
        catch
        {
            return (0, Environment.ProcessorCount);
        }
    }

    private Task<float> GetGpuUsageAsync(CancellationToken ct)
    {
        // Placeholder - would integrate with LibreHardwareMonitor
        return Task.FromResult(0f);
    }

    private async Task SetHighPerformancePowerPlanAsync(CancellationToken ct)
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // High Performance GUID
                var highPerfGuid = "8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c";
                var startInfo = new ProcessStartInfo
                {
                    FileName = "powercfg",
                    Arguments = $"/setactive {highPerfGuid}",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(startInfo);
                if (process is null)
                {
                    _logger.LogWarning("Failed to start powercfg process");
                    return;
                }

                await process.WaitForExitAsync(ct);

                _logger.LogInformation("Set power plan to High Performance");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to set high performance power plan");
        }
    }

    private static OptimizationLevel DetermineRecommendedLevel(long availableRamMb, long totalRamMb, float cpuUsage)
    {
        var ramPercentAvailable = totalRamMb > 0 ? (float)availableRamMb / totalRamMb * 100 : 100;

        if (ramPercentAvailable > 50 && cpuUsage < 30)
            return OptimizationLevel.Minimal;

        if (ramPercentAvailable > 30 && cpuUsage < 50)
            return OptimizationLevel.Standard;

        if (ramPercentAvailable > 15 && cpuUsage < 70)
            return OptimizationLevel.Aggressive;

        return OptimizationLevel.Extreme;
    }

    private static List<string> GenerateRecommendations(
        List<BackgroundProcess> terminable,
        (long AvailableMb, long TotalMb) memory,
        (float UsagePercent, int Cores) cpu)
    {
        var recommendations = new List<string>();

        var memoryPercent = memory.TotalMb > 0 ? (float)memory.AvailableMb / memory.TotalMb * 100 : 100;

        if (memoryPercent < 30)
            recommendations.Add($"Low available memory ({memoryPercent:F0}%). Consider closing memory-heavy applications.");

        if (cpu.UsagePercent > 60)
            recommendations.Add($"High CPU usage ({cpu.UsagePercent:F0}%). Closing background apps may improve game performance.");

        var browserProcesses = terminable.Where(p => p.Category == ProcessCategory.Browser).ToList();
        if (browserProcesses.Any())
        {
            var browserMemory = browserProcesses.Sum(p => p.MemoryUsageMb);
            recommendations.Add($"Browsers using {browserMemory}MB RAM. Consider closing for extra memory.");
        }

        var communicationProcesses = terminable.Where(p => p.Category == ProcessCategory.Communication).ToList();
        if (communicationProcesses.Any())
        {
            recommendations.Add("Communication apps running. These may cause notifications during gameplay.");
        }

        if (!recommendations.Any())
            recommendations.Add("System is well-optimized for gaming.");

        return recommendations;
    }

    private void SetState(OptimizationState newState, OptimizationProfile? profile)
    {
        var previousState = _currentState;
        _currentState = newState;

        StateChanged?.Invoke(this, new OptimizationStateChangedEventArgs
        {
            PreviousState = previousState,
            NewState = newState,
            AppliedProfile = profile
        });
    }

    private sealed record ProcessRestoreInfo(string Name, string? ExecutablePath);
}

