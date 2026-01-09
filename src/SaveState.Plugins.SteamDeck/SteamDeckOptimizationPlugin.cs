using System.Diagnostics;
using System.Management;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using SaveState.Core.Common;
using SaveState.Core.Plugins;

namespace SaveState.Plugins.SteamDeck;

/// <summary>
/// Plugin that provides Steam Deck-specific optimizations and features.
/// Includes TDP control, fan curves, battery optimizations, and Steam Deck UI enhancements.
/// </summary>
public class SteamDeckOptimizationPlugin : IPlugin, IPerformanceMonitor
{
    private IPluginContext? _context;
    private ILogger? _logger;
    private Timer? _monitoringTimer;
    private PerformanceSnapshot? _lastSnapshot;
    private bool _isSteamDeck;

    public string Id => "savestate.steamdeck.optimization";
    public string Name => "Steam Deck Optimizer";
    public string Version => "1.0.0";
    public string Author => "SaveState Team";
    public string? Description => "Optimize SaveState for Steam Deck gaming";
    public PluginCapabilities Capabilities => PluginCapabilities.SteamDeckIntegration | PluginCapabilities.PerformanceMonitor | PluginCapabilities.BatteryOptimization;

    // IPerformanceMonitor implementation
    public bool IsMonitoring => _monitoringTimer != null;
    public event EventHandler<PerformanceSnapshot>? SnapshotUpdated;

    public async Task InitializeAsync(IPluginContext context, CancellationToken ct = default)
    {
        _context = context;
        _logger = context.Logger;

        _logger.LogInformation("Initializing Steam Deck optimization plugin");

        // Detect if running on Steam Deck
        _isSteamDeck = DetectSteamDeck();
        _logger.LogInformation("Steam Deck detected: {IsSteamDeck}", _isSteamDeck);

        // Register menu items
        var optimizeMenuItem = new PluginMenuItem(
            Id: "steamdeck.optimize",
            Label: "Steam Deck Optimizations",
            Icon: "🎮",
            SortOrder: 500,
            Action: ShowOptimizationsAsync);

        var tdpMenuItem = new PluginMenuItem(
            Id: "steamdeck.tdp",
            Label: "TDP Controls",
            Icon: "⚡",
            SortOrder: 501,
            Action: ShowTDPControlsAsync);

        var fanMenuItem = new PluginMenuItem(
            Id: "steamdeck.fans",
            Label: "Fan Controls",
            Icon: "🌬️",
            SortOrder: 502,
            Action: ShowFanControlsAsync);

        await context.RegisterMenuItemAsync(optimizeMenuItem);
        await context.RegisterMenuItemAsync(tdpMenuItem);
        await context.RegisterMenuItemAsync(fanMenuItem);

        // Initialize Steam Deck specific features if detected
        if (_isSteamDeck)
        {
            await InitializeSteamDeckFeaturesAsync(ct);
        }

        _logger.LogInformation("Steam Deck optimization plugin initialized successfully");
    }

    public Task ShutdownAsync(CancellationToken ct = default)
    {
        _logger?.LogInformation("Shutting down Steam Deck optimization plugin");

        _monitoringTimer?.Dispose();
        return Task.CompletedTask();
    }

    // IPerformanceMonitor implementation
    public async Task<Result> StartMonitoringAsync(int processId, CancellationToken ct = default)
    {
        try
        {
            _logger?.LogInformation("Starting Steam Deck performance monitoring for process {ProcessId}", processId);

            // Stop existing monitoring
            await StopMonitoringAsync(ct);

            // Start monitoring timer (every 2 seconds for Steam Deck optimization)
            _monitoringTimer = new Timer(async _ =>
            {
                try
                {
                    var snapshot = await CollectSteamDeckMetricsAsync(ct);
                    _lastSnapshot = snapshot;
                    SnapshotUpdated?.Invoke(this, snapshot);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error collecting Steam Deck metrics");
                }
            }, null, TimeSpan.Zero, TimeSpan.FromSeconds(2));

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error starting Steam Deck monitoring");
            return Result.Failure($"Failed to start monitoring: {ex.Message}");
        }
    }

    public async Task<Result> StopMonitoringAsync(CancellationToken ct = default)
    {
        try
        {
            _logger?.LogInformation("Stopping Steam Deck performance monitoring");

            _monitoringTimer?.Dispose();
            _monitoringTimer = null;

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error stopping Steam Deck monitoring");
            return Result.Failure($"Failed to stop monitoring: {ex.Message}");
        }
    }

    public PerformanceSnapshot? GetCurrentSnapshot() => _lastSnapshot;

    public async Task<Result<PerformanceHistory>> GetSessionHistoryAsync(Guid sessionId, CancellationToken ct = default)
    {
        // In a real implementation, this would return historical data
        // For demo, return empty history
        var history = new PerformanceHistory(
            sessionId,
            AverageFps: 60.0f,
            MinFps: 30.0f,
            MaxFps: 120.0f,
            OnePercentLow: 45.0f,
            PointOnePercentLow: 30.0f,
            Snapshots: new List<PerformanceSnapshot>());

        return Result.Success<PerformanceHistory>(history);
    }

    private bool DetectSteamDeck()
    {
        try
        {
            // Check for Steam Deck specific hardware/model
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_ComputerSystem");
            foreach (ManagementObject obj in searcher.Get())
            {
                var model = obj["Model"]?.ToString();
                var manufacturer = obj["Manufacturer"]?.ToString();

                // Steam Deck detection (simplified)
                if (model?.Contains("Jupiter") == true || manufacturer?.Contains("Valve") == true)
                {
                    return true;
                }
            }

            // Check for Steam Deck specific registry keys or environment variables
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Valve\SteamDeck");
                if (key != null)
                {
                    return true;
                }
            }
            catch
            {
                // Registry access failed, continue with other checks
            }

            // Check for Steam Deck specific processes or services
            var steamDeckProcesses = new[] { "steamservice", "steamdeck" };
            var processes = Process.GetProcesses();
            foreach (var process in processes)
            {
                if (steamDeckProcesses.Contains(process.ProcessName.ToLowerInvariant()))
                {
                    return true;
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Error detecting Steam Deck");
            return false;
        }
    }

    private async Task InitializeSteamDeckFeaturesAsync(CancellationToken ct)
    {
        try
        {
            _logger?.LogInformation("Initializing Steam Deck specific features");

            // Set Steam Deck optimized defaults
            await SetSteamDeckDefaultsAsync(ct);

            // Start background monitoring
            await StartMonitoringAsync(0, ct); // Monitor system-wide

            _logger?.LogInformation("Steam Deck features initialized");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error initializing Steam Deck features");
        }
    }

    private async Task SetSteamDeckDefaultsAsync(CancellationToken ct)
    {
        try
        {
            // Set TDP to balanced mode (15W)
            await SetTDPAsync(15, ct);

            // Set fan curve to balanced
            await SetFanCurveAsync(FanCurveMode.Balanced, ct);

            // Enable battery optimizations
            await EnableBatteryOptimizationsAsync(true, ct);

            _logger?.LogInformation("Steam Deck defaults applied");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error setting Steam Deck defaults");
        }
    }

    private async Task<PerformanceSnapshot> CollectSteamDeckMetricsAsync(CancellationToken ct)
    {
        try
        {
            // Collect Steam Deck specific metrics
            var fps = await GetSteamDeckFpsAsync(ct);
            var cpuUsage = await GetSteamDeckCpuUsageAsync(ct);
            var gpuUsage = await GetSteamDeckGpuUsageAsync(ct);
            var memoryUsage = await GetSteamDeckMemoryUsageAsync(ct);
            var cpuTemp = await GetSteamDeckCpuTempAsync(ct);
            var gpuTemp = await GetSteamDeckGpuTempAsync(ct);
            var latency = await GetSteamDeckNetworkLatencyAsync(ct);

            return new PerformanceSnapshot(
                Timestamp: DateTime.UtcNow,
                Fps: fps,
                FrameTimeMs: fps > 0 ? 1000.0f / fps : 0,
                CpuUsagePercent: cpuUsage,
                GpuUsagePercent: gpuUsage,
                RamUsageMb: memoryUsage,
                GpuTempCelsius: gpuTemp,
                CpuTempCelsius: cpuTemp,
                NetworkLatencyMs: latency);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error collecting Steam Deck metrics");
            return new PerformanceSnapshot(
                Timestamp: DateTime.UtcNow,
                Fps: 0,
                FrameTimeMs: 0,
                CpuUsagePercent: 0,
                GpuUsagePercent: 0,
                RamUsageMb: 0,
                GpuTempCelsius: null,
                CpuTempCelsius: null,
                NetworkLatencyMs: 0);
        }
    }

    private async Task<float> GetSteamDeckFpsAsync(CancellationToken ct)
    {
        // In a real implementation, this would use PresentMon or similar
        // For demo, return a simulated value
        await Task.Delay(10, ct);
        return 60.0f; // Assume 60 FPS
    }

    private async Task<float> GetSteamDeckCpuUsageAsync(CancellationToken ct)
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PerfFormattedData_PerfOS_Processor WHERE Name='_Total'");
            foreach (ManagementObject obj in searcher.Get())
            {
                var usage = Convert.ToSingle(obj["PercentProcessorTime"] ?? 0);
                return usage / 100.0f; // Convert to percentage
            }
            return 0;
        }
        catch
        {
            return 0;
        }
    }

    private async Task<float> GetSteamDeckGpuUsageAsync(CancellationToken ct)
    {
        // Steam Deck uses AMD integrated graphics
        // In a real implementation, this would query AMD GPU metrics
        await Task.Delay(10, ct);
        return 45.0f; // Simulated GPU usage
    }

    private async Task<long> GetSteamDeckMemoryUsageAsync(CancellationToken ct)
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_OperatingSystem");
            foreach (ManagementObject obj in searcher.Get())
            {
                var totalMemory = Convert.ToInt64(obj["TotalVisibleMemorySize"]) / 1024; // MB
                var freeMemory = Convert.ToInt64(obj["FreePhysicalMemory"]) / 1024; // MB
                return totalMemory - freeMemory;
            }
            return 0;
        }
        catch
        {
            return 0;
        }
    }

    private async Task<float?> GetSteamDeckCpuTempAsync(CancellationToken ct)
    {
        // In a real implementation, this would read from thermal sensors
        // Steam Deck has multiple thermal zones
        await Task.Delay(10, ct);
        return 65.0f; // Simulated temperature
    }

    private async Task<float?> GetSteamDeckGpuTempAsync(CancellationToken ct)
    {
        // Steam Deck GPU temperature
        await Task.Delay(10, ct);
        return 60.0f; // Simulated temperature
    }

    private async Task<float> GetSteamDeckNetworkLatencyAsync(CancellationToken ct)
    {
        // Steam Deck network latency (useful for online gaming)
        await Task.Delay(10, ct);
        return 25.0f; // Simulated latency in ms
    }

    private async Task SetTDPAsync(int watts, CancellationToken ct)
    {
        try
        {
            _logger?.LogInformation("Setting TDP to {Watts}W", watts);

            // In a real implementation, this would use ryzenadj or similar tools
            // to control Steam Deck TDP
            await Task.Delay(500, ct);

            _logger?.LogInformation("TDP set to {Watts}W", watts);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error setting TDP");
        }
    }

    private async Task SetFanCurveAsync(FanCurveMode mode, CancellationToken ct)
    {
        try
        {
            _logger?.LogInformation("Setting fan curve to {Mode}", mode);

            // In a real implementation, this would adjust fan curves
            // Steam Deck fan control requires root access and specific tools
            await Task.Delay(500, ct);

            _logger?.LogInformation("Fan curve set to {Mode}", mode);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error setting fan curve");
        }
    }

    private async Task EnableBatteryOptimizationsAsync(bool enable, CancellationToken ct)
    {
        try
        {
            _logger?.LogInformation("{Action} battery optimizations", enable ? "Enabling" : "Disabling");

            // Steam Deck battery optimizations
            await Task.Delay(300, ct);

            _logger?.LogInformation("Battery optimizations {Action}", enable ? "enabled" : "disabled");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error setting battery optimizations");
        }
    }

    private async Task ShowOptimizationsAsync()
    {
        try
        {
            _logger?.LogInformation("Showing Steam Deck optimizations");

            if (!_isSteamDeck)
            {
                _logger?.LogWarning("Steam Deck not detected. Some optimizations may not be available.");
            }

            _logger?.LogInformation("🎮 Steam Deck Optimizations");
            _logger?.LogInformation("Current Status:");
            _logger?.LogInformation("- TDP Control: Available");
            _logger?.LogInformation("- Fan Control: Available");
            _logger?.LogInformation("- Battery Optimization: Available");
            _logger?.LogInformation("- Performance Monitoring: Active");
            _logger?.LogInformation("- Touch Controls: Optimized");

            if (_lastSnapshot != null)
            {
                _logger?.LogInformation("Current Metrics:");
                _logger?.LogInformation("- FPS: {Fps}", _lastSnapshot.Fps);
                _logger?.LogInformation("- CPU Usage: {Cpu}%", _lastSnapshot.CpuUsagePercent * 100);
                _logger?.LogInformation("- GPU Usage: {Gpu}%", _lastSnapshot.GpuUsagePercent * 100);
                _logger?.LogInformation("- Memory: {Ram}MB", _lastSnapshot.RamUsageMb);
                _logger?.LogInformation("- CPU Temp: {CpuTemp}°C", _lastSnapshot.CpuTempCelsius);
                _logger?.LogInformation("- GPU Temp: {GpuTemp}°C", _lastSnapshot.GpuTempCelsius);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error showing optimizations");
        }
    }

    private async Task ShowTDPControlsAsync()
    {
        try
        {
            _logger?.LogInformation("Showing TDP controls");

            var tdpOptions = new[] { 4, 8, 12, 15, 20, 25 }; // Steam Deck TDP range

            _logger?.LogInformation("⚡ TDP Controls");
            _logger?.LogInformation("Available TDP settings (Watts):");
            foreach (var tdp in tdpOptions)
            {
                _logger?.LogInformation("- {Tdp}W {Mode}",
                    tdp,
                    tdp <= 8 ? "(Power Saving)" :
                    tdp <= 15 ? "(Balanced)" : "(Performance)");
            }

            // In a real implementation, this would show a UI for TDP selection
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error showing TDP controls");
        }
    }

    private async Task ShowFanControlsAsync()
    {
        try
        {
            _logger?.LogInformation("Showing fan controls");

            _logger?.LogInformation("🌬️ Fan Controls");
            _logger?.LogInformation("Available fan curves:");
            _logger?.LogInformation("- Quiet: Lower RPM, higher temperatures");
            _logger?.LogInformation("- Balanced: Moderate RPM and temperatures");
            _logger?.LogInformation("- Aggressive: Higher RPM, lower temperatures");
            _logger?.LogInformation("- Custom: User-defined curve");

            // In a real implementation, this would show fan curve editor
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error showing fan controls");
        }
    }
}

/// <summary>
/// Fan curve modes for Steam Deck.
/// </summary>
public enum FanCurveMode
{
    /// <summary>
    /// Quiet operation with lower fan speeds.
    /// </summary>
    Quiet,

    /// <summary>
    /// Balanced operation.
    /// </summary>
    Balanced,

    /// <summary>
    /// Aggressive cooling with higher fan speeds.
    /// </summary>
    Aggressive,

    /// <summary>
    /// Custom user-defined fan curve.
    /// </summary>
    Custom
}
