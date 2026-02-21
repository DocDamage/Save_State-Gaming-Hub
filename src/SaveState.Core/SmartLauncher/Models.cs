// Copyright (c) 2026 SaveStateReborn. All rights reserved.

using SaveState.Core.Common.Services;

namespace SaveState.Core.SmartLauncher;

/// <summary>
/// Represents a game launch profile with optimization settings.
/// </summary>
public class LaunchProfile
{
    /// <summary>
    /// Profile ID.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Profile name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Profile description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Associated game ID (null if global profile).
    /// </summary>
    public Guid? GameId { get; set; }

    /// <summary>
    /// CPU priority for the game process.
    /// </summary>
    public ProcessPriority Priority { get; set; } = ProcessPriority.High;

    /// <summary>
    /// Whether to disable Windows Game Mode.
    /// </summary>
    public bool DisableGameMode { get; set; } = false;

    /// <summary>
    /// Whether to disable fullscreen optimizations.
    /// </summary>
    public bool DisableFullscreenOptimizations { get; set; } = true;

    /// <summary>
    /// Whether to run the game as administrator.
    /// </summary>
    public bool RunAsAdministrator { get; set; } = false;

    /// <summary>
    /// Whether to disable Windows Defender during gameplay.
    /// </summary>
    public bool DisableWindowsDefender { get; set; } = false;

    /// <summary>
    /// List of processes to suspend during gameplay.
    /// </summary>
    public List<string> ProcessesToSuspend { get; set; } = new();

    /// <summary>
    /// List of services to stop during gameplay.
    /// </summary>
    public List<string> ServicesToStop { get; set; } = new();

    /// <summary>
    /// Display settings to apply.
    /// </summary>
    public DisplaySettings? DisplaySettings { get; set; }

    /// <summary>
    /// Performance settings.
    /// </summary>
    public PerformanceSettings PerformanceSettings { get; set; } = new();

    /// <summary>
    /// Power plan to use during gameplay.
    /// </summary>
    public string? PowerPlanGuid { get; set; }

    /// <summary>
    /// Whether this is the default profile.
    /// </summary>
    public bool IsDefault { get; set; } = false;

    /// <summary>
    /// Whether the profile is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// When the profile was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the profile was last modified.
    /// </summary>
    public DateTime? ModifiedAt { get; set; }

    /// <summary>
    /// Initializes a new instance of the LaunchProfile class.
    /// </summary>
    public LaunchProfile(ITimeProvider? timeProvider = null)
    {
        CreatedAt = (timeProvider ?? SystemTimeProvider.Instance).UtcNow;
    }

    /// <summary>
    /// Estimated performance gain percentage.
    /// </summary>
    public int? EstimatedPerformanceGain { get; set; }

    /// <summary>
    /// Predefined profile types.
    /// </summary>
    public static LaunchProfile CreatePerformanceProfile()
    {
        return new LaunchProfile
        {
            Name = "Maximum Performance",
            Description = "Optimizes system for maximum gaming performance",
            Priority = ProcessPriority.RealTime,
            DisableFullscreenOptimizations = true,
            PerformanceSettings = new PerformanceSettings
            {
                EnableMemoryOptimization = true,
                EnableCPUParking = false,
                DisableVisualEffects = true,
                ClearStandbyList = true
            },
            ProcessesToSuspend = new List<string>
            {
                "chrome", "firefox", "edge", "discord", "spotify",
                "steamwebhelper", "epicwebhelper", "originwebhelper"
            },
            EstimatedPerformanceGain = 15
        };
    }

    public static LaunchProfile CreateBalancedProfile()
    {
        return new LaunchProfile
        {
            Name = "Balanced",
            Description = "Balanced performance with minimal system changes",
            Priority = ProcessPriority.High,
            PerformanceSettings = new PerformanceSettings
            {
                EnableMemoryOptimization = true,
                EnableCPUParking = true,
                DisableVisualEffects = false,
                ClearStandbyList = false
            },
            ProcessesToSuspend = new List<string> { "chrome", "discord" },
            EstimatedPerformanceGain = 5
        };
    }

    public static LaunchProfile CreatePowerSaverProfile()
    {
        return new LaunchProfile
        {
            Name = "Power Saver",
            Description = "Optimizes for battery life on laptops",
            Priority = ProcessPriority.AboveNormal,
            PerformanceSettings = new PerformanceSettings
            {
                EnableMemoryOptimization = true,
                EnableCPUParking = true,
                DisableVisualEffects = true,
                ClearStandbyList = false,
                TargetFPS = 30
            },
            EstimatedPerformanceGain = -10
        };
    }
}

/// <summary>
/// Process priority levels.
/// </summary>
public enum ProcessPriority
{
    Low,
    BelowNormal,
    Normal,
    AboveNormal,
    High,
    RealTime
}

/// <summary>
/// Display settings for launch.
/// </summary>
public class DisplaySettings
{
    /// <summary>
    /// Target resolution width.
    /// </summary>
    public int? ResolutionWidth { get; set; }

    /// <summary>
    /// Target resolution height.
    /// </summary>
    public int? ResolutionHeight { get; set; }

    /// <summary>
    /// Target refresh rate.
    /// </summary>
    public int? RefreshRate { get; set; }

    /// <summary>
    /// Whether to enable HDR.
    /// </summary>
    public bool? EnableHDR { get; set; }

    /// <summary>
    /// Whether to disable fullscreen optimizations.
    /// </summary>
    public bool DisableFullscreenOptimizations { get; set; } = true;

    /// <summary>
    /// Override DPI scaling.
    /// </summary>
    public bool? OverrideDPIScaling { get; set; }
}

/// <summary>
/// Performance optimization settings.
/// </summary>
public class PerformanceSettings
{
    /// <summary>
    /// Whether to enable memory optimization.
    /// </summary>
    public bool EnableMemoryOptimization { get; set; } = true;

    /// <summary>
    /// Whether to enable CPU parking for unused cores.
    /// </summary>
    public bool EnableCPUParking { get; set; } = true;

    /// <summary>
    /// Whether to disable Windows visual effects.
    /// </summary>
    public bool DisableVisualEffects { get; set; } = false;

    /// <summary>
    /// Whether to clear standby list before launch.
    /// </summary>
    public bool ClearStandbyList { get; set; } = false;

    /// <summary>
    /// Target FPS for frame limiting (null = unlimited).
    /// </summary>
    public int? TargetFPS { get; set; }

    /// <summary>
    /// Whether to enable hardware-accelerated GPU scheduling.
    /// </summary>
    public bool EnableHardwareGPUScheduling { get; set; } = true;
}

/// <summary>
/// Game launch session tracking.
/// </summary>
public class LaunchSession
{
    /// <summary>
    /// Session ID.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Game ID.
    /// </summary>
    public Guid GameId { get; set; }

    /// <summary>
    /// Game name.
    /// </summary>
    public string GameName { get; set; } = string.Empty;

    /// <summary>
    /// Launch profile used.
    /// </summary>
    public Guid? ProfileId { get; set; }

    /// <summary>
    /// When the session started.
    /// </summary>
    public DateTime StartedAt { get; set; }

    /// <summary>
    /// Initializes a new instance of the LaunchSession class.
    /// </summary>
    public LaunchSession(ITimeProvider? timeProvider = null)
    {
        StartedAt = (timeProvider ?? SystemTimeProvider.Instance).UtcNow;
    }

    /// <summary>
    /// When the session ended.
    /// </summary>
    public DateTime? EndedAt { get; set; }

    /// <summary>
    /// Duration of the session.
    /// </summary>
    public TimeSpan? Duration => EndedAt.HasValue ? EndedAt.Value - StartedAt : null;

    /// <summary>
    /// Initial system state (for restoration).
    /// </summary>
    public SystemState? InitialSystemState { get; set; }

    /// <summary>
    /// Whether the session is active.
    /// </summary>
    public bool IsActive => !EndedAt.HasValue;

    /// <summary>
    /// Exit code if available.
    /// </summary>
    public int? ExitCode { get; set; }

    /// <summary>
    /// Performance metrics collected during session.
    /// </summary>
    public SessionPerformanceMetrics? PerformanceMetrics { get; set; }
}

/// <summary>
/// System state for backup/restore.
/// </summary>
public class SystemState
{
    /// <summary>
    /// Active power plan GUID.
    /// </summary>
    public string? PowerPlanGuid { get; set; }

    /// <summary>
    /// Running processes that were suspended.
    /// </summary>
    public List<ProcessInfo> SuspendedProcesses { get; set; } = new();

    /// <summary>
    /// Services that were stopped.
    /// </summary>
    public List<ServiceInfo> StoppedServices { get; set; } = new();

    /// <summary>
    /// Display settings before launch.
    /// </summary>
    public DisplaySettings? DisplaySettings { get; set; }

    /// <summary>
    /// Whether visual effects were enabled.
    /// </summary>
    public bool VisualEffectsEnabled { get; set; } = true;
}

/// <summary>
/// Process information.
/// </summary>
public class ProcessInfo
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ExecutablePath { get; set; }
}

/// <summary>
/// Service information.
/// </summary>
public class ServiceInfo
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string StartupType { get; set; } = string.Empty;
}

/// <summary>
/// Performance metrics for a gaming session.
/// </summary>
public class SessionPerformanceMetrics
{
    /// <summary>
    /// Average FPS.
    /// </summary>
    public double? AverageFPS { get; set; }

    /// <summary>
    /// Minimum FPS.
    /// </summary>
    public double? MinFPS { get; set; }

    /// <summary>
    /// Maximum FPS.
    /// </summary>
    public double? MaxFPS { get; set; }

    /// <summary>
    /// Average CPU usage.
    /// </summary>
    public double? AverageCPUUsage { get; set; }

    /// <summary>
    /// Average GPU usage.
    /// </summary>
    public double? AverageGPUUsage { get; set; }

    /// <summary>
    /// Peak memory usage in MB.
    /// </summary>
    public long? PeakMemoryMB { get; set; }

    /// <summary>
    /// Average temperature in Celsius.
    /// </summary>
    public double? AverageTemperature { get; set; }
}

/// <summary>
/// Launch result.
/// </summary>
public class LaunchResult
{
    /// <summary>
    /// Whether the launch was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Error message if failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Process ID of the launched game.
    /// </summary>
    public int? ProcessId { get; set; }

    /// <summary>
    /// Session ID for tracking.
    /// </summary>
    public Guid? SessionId { get; set; }

    /// <summary>
    /// Optimizations that were applied.
    /// </summary>
    public List<string> AppliedOptimizations { get; set; } = new();

    /// <summary>
    /// Estimated performance improvement.
    /// </summary>
    public int? EstimatedPerformanceGain { get; set; }

    public static LaunchResult Successful(int processId, Guid sessionId, List<string> optimizations, int? performanceGain)
    {
        return new LaunchResult
        {
            Success = true,
            ProcessId = processId,
            SessionId = sessionId,
            AppliedOptimizations = optimizations,
            EstimatedPerformanceGain = performanceGain
        };
    }

    public static LaunchResult Failed(string error)
    {
        return new LaunchResult
        {
            Success = false,
            ErrorMessage = error
        };
    }
}
