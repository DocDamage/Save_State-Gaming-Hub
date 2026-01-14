using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Management;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Performance.Services;

namespace SaveState.Infrastructure.Performance;

public class BatteryOptimizer : IBatteryOptimizer
{
    private readonly ILogger<BatteryOptimizer> _logger;
    private BatteryProfile? _activeProfile;
    private Timer? _batteryMonitorTimer;
    private BatteryStatus _lastBatteryStatus;
    private readonly Dictionary<Guid, BatteryProfile> _profiles = new();
    private readonly object _profilesLock = new();

    public event EventHandler<BatteryStatusChangedEventArgs>? BatteryStatusChanged;
    public event EventHandler<LowBatteryWarningEventArgs>? LowBatteryWarning;

    public BatteryOptimizer(ILogger<BatteryOptimizer> logger)
    {
        _logger = logger;
        _lastBatteryStatus = new BatteryStatus(100, TimeSpan.MaxValue, false, PowerMode.Balanced, BatteryHealth.Good, 25.0);
        SeedDefaultProfiles();
        StartBatteryMonitoring();
    }

    public Task<Result<BatteryStatus>> GetBatteryStatusAsync(CancellationToken ct = default)
    {
        try
        {
            var status = GetCurrentBatteryStatusAsync(ct);
            return Task.FromResult(Result.Success<BatteryStatus>(status));
        }
        catch (Exception ex)
        {
            return Task.FromResult(Result.Failure<BatteryStatus>($"Failed to get battery status: {ex.Message}", ErrorType.Internal));
        }
    }

    public Task<Result<BatteryProfile>> CreateProfileAsync(PowerMode mode, BatteryOptimizationSettings settings, CancellationToken ct = default)
    {
        try
        {
            var profile = new BatteryProfile(
                Id: Guid.NewGuid(),
                Name: $"{mode} Mode - {DateTime.Now:yyyy-MM-dd}",
                Mode: mode,
                Settings: settings,
                CreatedAt: DateTime.UtcNow,
                IsActive: false);

            lock (_profilesLock)
            {
                _profiles[profile.Id] = profile;
            }

            return Task.FromResult(Result.Success<BatteryProfile>(profile));
        }
        catch (Exception ex)
        {
            return Task.FromResult(Result.Failure<BatteryProfile>($"Failed to create battery profile: {ex.Message}", ErrorType.Internal));
        }
    }

    public Task<Result> ApplyProfileAsync(Guid profileId, CancellationToken ct = default)
    {
        try
        {
            BatteryProfile profile;
            lock (_profilesLock)
            {
                if (!_profiles.TryGetValue(profileId, out profile))
                {
                    return Task.FromResult(Result.Failure($"Profile {profileId} not found", ErrorType.NotFound));
                }
            }

            _activeProfile = profile with { IsActive = true };
            lock (_profilesLock)
            {
                _profiles[profileId] = _activeProfile;
            }

            ApplyBatterySettingsAsync(profile.Settings, ct);

            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            return Task.FromResult(Result.Failure($"Failed to apply battery profile: {ex.Message}", ErrorType.Internal));
        }
    }

    public Task<Result<BatteryProfile?>> GetCurrentProfileAsync(CancellationToken ct = default)
    {
        return Task.FromResult(Result.Success<BatteryProfile?>(_activeProfile));
    }

    public Task<Result<IReadOnlyList<BatteryProfile>>> GetAllProfilesAsync(CancellationToken ct = default)
    {
        try
        {
            lock (_profilesLock)
            {
                return Task.FromResult(Result.Success<IReadOnlyList<BatteryProfile>>(_profiles.Values.ToList()));
            }
        }
        catch (Exception ex)
        {
            return Task.FromResult(Result.Failure<IReadOnlyList<BatteryProfile>>($"Failed to get battery profiles: {ex.Message}", ErrorType.Internal));
        }
    }

    private BatteryStatus GetCurrentBatteryStatusAsync(CancellationToken ct)
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return GetWindowsBatteryStatusAsync(ct);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return GetLinuxBatteryStatusAsync(ct);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return GetMacOsBatteryStatusAsync(ct);
            }
            else
            {
                return new BatteryStatus(100, TimeSpan.MaxValue, false, PowerMode.Balanced, BatteryHealth.Good, 25.0);
            }
        }
        catch
        {
            return new BatteryStatus(100, TimeSpan.MaxValue, false, PowerMode.Balanced, BatteryHealth.Good, 25.0);
        }
    }

    private BatteryStatus GetWindowsBatteryStatusAsync(CancellationToken ct)
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT EstimatedChargeRemaining, BatteryStatus, EstimatedRunTime FROM Win32_Battery");
            var results = searcher.Get();
            if (results.Count == 0)
            {
                return _lastBatteryStatus;
            }

            var battery = results.Cast<ManagementBaseObject>().First();
            var percent = Convert.ToInt32(battery["EstimatedChargeRemaining"] ?? 100);
            var statusCode = Convert.ToInt32(battery["BatteryStatus"] ?? 1);
            var estimatedRunTime = Convert.ToInt32(battery["EstimatedRunTime"] ?? 0);

            var isCharging = statusCode is 6 or 7 or 8 or 9;
            var currentMode = _activeProfile?.Mode ?? PowerMode.Balanced;
            var estimatedRemaining = estimatedRunTime > 0 && estimatedRunTime < 65535
                ? TimeSpan.FromMinutes(estimatedRunTime)
                : TimeSpan.MaxValue;

            var cpuTemp = ReadCpuTemperatureCelsius();
            var temperature = cpuTemp ?? _lastBatteryStatus.TemperatureCelsius;

            var health = percent switch
            {
                >= 90 => BatteryHealth.Excellent,
                >= 70 => BatteryHealth.Good,
                >= 50 => BatteryHealth.Fair,
                >= 25 => BatteryHealth.Poor,
                _ => BatteryHealth.Critical
            };

            return new BatteryStatus(percent, estimatedRemaining, isCharging, currentMode, health, temperature);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to read Windows battery status");
            return new BatteryStatus(100, TimeSpan.MaxValue, false, PowerMode.Balanced, BatteryHealth.Good, 25.0);
        }
    }

    private BatteryStatus GetLinuxBatteryStatusAsync(CancellationToken ct)
    {
        try
        {
            // Read from /sys/class/power_supply/BAT* on Linux systems
            var batteryPaths = Directory.GetDirectories("/sys/class/power_supply")
                .Where(d => Path.GetFileName(d).StartsWith("BAT"))
                .ToList();

            if (!batteryPaths.Any())
            {
                return new BatteryStatus(100, TimeSpan.MaxValue, false, PowerMode.Balanced, BatteryHealth.Good, 25.0);
            }

            // Read battery information from sysfs
            var batteryPath = batteryPaths.First();

            var capacity = ReadSysFsValue(batteryPath, "capacity", 100);
            var status = ReadSysFsValue(batteryPath, "status", "Unknown");
            var voltage = ReadSysFsValue(batteryPath, "voltage_now", 0) / 1000000.0; // Convert from µV to V
            var current = ReadSysFsValue(batteryPath, "current_now", 0) / 1000000.0; // Convert from µA to A

            var isCharging = status.Contains("Charging", StringComparison.OrdinalIgnoreCase);
            var currentMode = _activeProfile?.Mode ?? PowerMode.Balanced;

            // Estimate remaining time
            var estimatedHours = current > 0 ? (voltage * capacity / 100.0) / current : 24.0;
            var estimatedRemaining = TimeSpan.FromHours(Math.Min(estimatedHours, 24.0));

            // Estimate temperature (Linux doesn't always provide this)
            var temperature = voltage > 4.0 ? 40.0 : voltage > 3.8 ? 35.0 : 30.0;

            var health = capacity > 80 ? BatteryHealth.Excellent :
                        capacity > 60 ? BatteryHealth.Good :
                        capacity > 40 ? BatteryHealth.Fair :
                        capacity > 20 ? BatteryHealth.Poor : BatteryHealth.Critical;

            return new BatteryStatus(capacity, estimatedRemaining, isCharging, currentMode, health, temperature);
        }
        catch
        {
            return new BatteryStatus(100, TimeSpan.MaxValue, false, PowerMode.Balanced, BatteryHealth.Good, 25.0);
        }
    }

    private BatteryStatus GetMacOsBatteryStatusAsync(CancellationToken ct)
    {
        try
        {
            // Use system_profiler or ioreg on macOS
            // This is a simplified implementation

            var percent = 90; // Mock value
            var isCharging = true;
            var estimatedHours = 5.0;
            var temperature = 32.0;

            var estimatedRemaining = TimeSpan.FromHours(estimatedHours);
            var currentMode = _activeProfile?.Mode ?? PowerMode.Balanced;

            var health = percent > 80 ? BatteryHealth.Excellent :
                        percent > 60 ? BatteryHealth.Good :
                        percent > 40 ? BatteryHealth.Fair :
                        percent > 20 ? BatteryHealth.Poor : BatteryHealth.Critical;

            return new BatteryStatus(percent, estimatedRemaining, isCharging, currentMode, health, temperature);
        }
        catch
        {
            return new BatteryStatus(100, TimeSpan.MaxValue, false, PowerMode.Balanced, BatteryHealth.Good, 25.0);
        }
    }

    private Task ApplyBatterySettingsAsync(BatteryOptimizationSettings settings, CancellationToken ct)
    {
        try
        {
            if (settings.DisableBackgroundApps || settings.EnablePowerSaverMode)
            {
                try
                {
                    var process = Process.GetCurrentProcess();
                    process.PriorityClass = settings.EnablePowerSaverMode ? ProcessPriorityClass.BelowNormal : ProcessPriorityClass.Normal;
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Failed to adjust process priority for battery optimization");
                }
            }

            if (settings.ReduceFrameRate || settings.LowerResolution)
            {
                ApplyPerformanceSettingsAsync(settings, ct);
            }

            if (settings.ReduceAudioQuality)
            {
                ApplyAudioSettingsAsync(settings, ct);
            }

            SetScreenBrightnessAsync(settings.ScreenBrightnessPercent, ct);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to apply some battery settings");
        }

        return Task.CompletedTask;
    }

    private Task ApplyPerformanceSettingsAsync(BatteryOptimizationSettings settings, CancellationToken ct)
    {
        // Adjust process priority already handled; hook for future performance tweaks
        return Task.CompletedTask;
    }

    private Task SetScreenBrightnessAsync(int brightnessPercent, CancellationToken ct)
    {
        try
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return Task.CompletedTask;

            var clamped = Math.Clamp(brightnessPercent, 0, 100);
            using var mclass = new ManagementClass("root\\wmi", "WmiMonitorBrightnessMethods", null);
            foreach (ManagementObject instance in mclass.GetInstances())
            {
                instance.InvokeMethod("WmiSetBrightness", new object[] { uint.MaxValue, (byte)clamped });
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to set screen brightness");
        }

        return Task.CompletedTask;
    }

    private void SeedDefaultProfiles()
    {
        lock (_profilesLock)
        {
            if (_profiles.Count > 0)
                return;

            var performanceId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            var balancedId = Guid.Parse("22222222-2222-2222-2222-222222222222");
            var powerSaverId = Guid.Parse("33333333-3333-3333-3333-333333333333");

            _profiles[performanceId] = new BatteryProfile(
                performanceId,
                "Performance Mode",
                PowerMode.Performance,
                new BatteryOptimizationSettings(
                    DisableBackgroundApps: false,
                    ReduceFrameRate: false,
                    LowerResolution: false,
                    DisableVSync: false,
                    ReduceAudioQuality: false,
                    EnablePowerSaverMode: false,
                    TargetFrameRate: 60,
                    ScreenBrightnessPercent: 100),
                DateTime.UtcNow.AddDays(-1),
                false);

            _profiles[balancedId] = new BatteryProfile(
                balancedId,
                "Balanced Mode",
                PowerMode.Balanced,
                new BatteryOptimizationSettings(
                    DisableBackgroundApps: true,
                    ReduceFrameRate: false,
                    LowerResolution: false,
                    DisableVSync: false,
                    ReduceAudioQuality: false,
                    EnablePowerSaverMode: false,
                    TargetFrameRate: 60,
                    ScreenBrightnessPercent: 80),
                DateTime.UtcNow.AddDays(-1),
                true);

            _profiles[powerSaverId] = new BatteryProfile(
                powerSaverId,
                "Power Saver Mode",
                PowerMode.PowerSaver,
                new BatteryOptimizationSettings(
                    DisableBackgroundApps: true,
                    ReduceFrameRate: true,
                    LowerResolution: true,
                    DisableVSync: true,
                    ReduceAudioQuality: true,
                    EnablePowerSaverMode: true,
                    TargetFrameRate: 30,
                    ScreenBrightnessPercent: 50),
                DateTime.UtcNow.AddDays(-1),
                false);

            _activeProfile = _profiles[balancedId];
        }
    }

    private double? ReadCpuTemperatureCelsius()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(@"root\\WMI", "SELECT CurrentTemperature FROM MSAcpi_ThermalZoneTemperature");
            foreach (var obj in searcher.Get())
            {
                if (obj["CurrentTemperature"] is uint rawTemp && rawTemp > 0)
                {
                    return (rawTemp / 10.0) - 273.15;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "CPU temperature unavailable");
        }

        return null;
    }

    private void StartBatteryMonitoring()
    {
        try
        {
            _batteryMonitorTimer = new Timer(
                callback: _ => CheckBatteryStatus(),
                state: null,
                dueTime: TimeSpan.FromSeconds(5),
                period: TimeSpan.FromSeconds(30));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to start battery monitoring timer");
        }
    }

    private void CheckBatteryStatus()
    {
        try
        {
            var currentStatus = GetCurrentBatteryStatusAsync(CancellationToken.None);
            
            // Check for low battery warning
            if (!currentStatus.IsCharging && currentStatus.PercentRemaining < 20 && 
                _lastBatteryStatus.PercentRemaining >= 20)
            {
                LowBatteryWarning?.Invoke(this, new LowBatteryWarningEventArgs
                {
                    PercentRemaining = currentStatus.PercentRemaining,
                    EstimatedTime = currentStatus.EstimatedRemaining,
                    IsCharging = currentStatus.IsCharging
                });
            }
            
            // Notify status change
            if (currentStatus.PercentRemaining != _lastBatteryStatus.PercentRemaining || 
                currentStatus.IsCharging != _lastBatteryStatus.IsCharging)
            {
                BatteryStatusChanged?.Invoke(this, new BatteryStatusChangedEventArgs
                {
                    PreviousStatus = _lastBatteryStatus,
                    CurrentStatus = currentStatus
                });
            }
            
            _lastBatteryStatus = currentStatus;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error checking battery status");
        }
    }

    private T ReadSysFsValue<T>(string batteryPath, string fileName, T defaultValue)
    {
        try
        {
            var filePath = Path.Combine(batteryPath, fileName);
            if (!File.Exists(filePath))
                return defaultValue;

            var content = File.ReadAllText(filePath).Trim();
            
            if (typeof(T) == typeof(int))
            {
                return (T)(object)int.Parse(content);
            }
            else if (typeof(T) == typeof(double))
            {
                return (T)(object)double.Parse(content);
            }
            else if (typeof(T) == typeof(string))
            {
                return (T)(object)content;
            }
            
            return defaultValue;
        }
        catch
        {
            return defaultValue;
        }
    }

    private Task ApplyAudioSettingsAsync(BatteryOptimizationSettings settings, CancellationToken ct)
    {
        // Placeholder for audio quality adjustments
        // Would require audio framework integration
        _logger.LogDebug("Audio quality adjustment not yet implemented");
        return Task.CompletedTask;
    }
}





