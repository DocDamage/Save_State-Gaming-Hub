using SaveState.Core.Common;

namespace SaveState.Core.Performance.Services;

public interface IBatteryOptimizer
{
    Task<Result<BatteryStatus>> GetBatteryStatusAsync(CancellationToken ct = default);
    Task<Result<BatteryProfile>> CreateProfileAsync(PowerMode mode, BatteryOptimizationSettings settings, CancellationToken ct = default);
    Task<Result> ApplyProfileAsync(Guid profileId, CancellationToken ct = default);
    Task<Result<BatteryProfile?>> GetCurrentProfileAsync(CancellationToken ct = default);
    Task<Result<IReadOnlyList<BatteryProfile>>> GetAllProfilesAsync(CancellationToken ct = default);
    event EventHandler<BatteryStatusChangedEventArgs>? BatteryStatusChanged;
    event EventHandler<LowBatteryWarningEventArgs>? LowBatteryWarning;
}

public sealed record BatteryStatus(
    int PercentRemaining,
    TimeSpan EstimatedRemaining,
    bool IsCharging,
    PowerMode CurrentMode,
    BatteryHealth Health,
    double TemperatureCelsius);

public sealed record BatteryProfile(
    Guid Id,
    string Name,
    PowerMode Mode,
    BatteryOptimizationSettings Settings,
    DateTime CreatedAt,
    bool IsActive);

public sealed record BatteryOptimizationSettings(
    bool DisableBackgroundApps,
    bool ReduceFrameRate,
    bool LowerResolution,
    bool DisableVSync,
    bool ReduceAudioQuality,
    bool EnablePowerSaverMode,
    int TargetFrameRate,
    int ScreenBrightnessPercent);

public enum PowerMode { Performance, Balanced, PowerSaver, UltraPowerSaver, Custom }

public enum BatteryHealth { Excellent, Good, Fair, Poor, Critical }

public sealed class BatteryStatusChangedEventArgs : EventArgs
{
    public BatteryStatus PreviousStatus { get; init; } = default!;
    public BatteryStatus CurrentStatus { get; init; } = default!;
}

public sealed class LowBatteryWarningEventArgs : EventArgs
{
    public int PercentRemaining { get; init; }
    public TimeSpan EstimatedTime { get; init; }
    public bool IsCharging { get; init; }
}