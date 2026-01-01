using SaveState.Core.Common;

namespace SaveState.Core.Performance.Services;

/// <summary>
/// Service interface for managing system resources and optimizing for gaming performance.
/// </summary>
public interface ISystemResourceManager
{
    /// <summary>
    /// Analyzes the current system state and provides optimization recommendations.
    /// </summary>
    Task<Result<SystemAnalysis>> AnalyzeSystemAsync(CancellationToken ct = default);

    /// <summary>
    /// Applies an optimization profile to the system.
    /// </summary>
    Task<Result> ApplyOptimizationAsync(OptimizationProfile profile, CancellationToken ct = default);

    /// <summary>
    /// Restores the system to its pre-optimization state.
    /// </summary>
    Task<Result> RestoreSystemAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets the list of background processes that can be safely terminated.
    /// </summary>
    Task<Result<IReadOnlyList<BackgroundProcess>>> GetBackgroundProcessesAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets the current optimization state.
    /// </summary>
    OptimizationState CurrentState { get; }

    /// <summary>
    /// Event raised when optimization state changes.
    /// </summary>
    event EventHandler<OptimizationStateChangedEventArgs>? StateChanged;
}

/// <summary>
/// Represents the result of a system analysis.
/// </summary>
public sealed record SystemAnalysis(
    IReadOnlyList<BackgroundProcess> TerminableProcesses,
    long AvailableRamMb,
    long TotalRamMb,
    float CpuUsagePercent,
    float CpuHeadroom,
    float GpuUsagePercent,
    OptimizationLevel RecommendedLevel,
    IReadOnlyList<string> Recommendations);

/// <summary>
/// Represents a background process that can be managed.
/// </summary>
public sealed record BackgroundProcess(
    int ProcessId,
    string Name,
    string? Description,
    long MemoryUsageMb,
    float CpuUsagePercent,
    ProcessCategory Category,
    bool IsSafeToTerminate);

/// <summary>
/// Category of a background process.
/// </summary>
public enum ProcessCategory
{
    System,
    Security,
    Utility,
    Communication,
    Media,
    Gaming,
    Browser,
    Development,
    Other
}

/// <summary>
/// Defines an optimization profile with settings for system tuning.
/// </summary>
public sealed record OptimizationProfile(
    string Name,
    OptimizationLevel Level,
    IReadOnlyList<string> ProcessesToClose,
    bool SetGamePriority,
    bool DisableOverlays,
    bool DisableWindowsGameMode,
    bool SetHighPerformancePowerPlan,
    bool DisableFullscreenOptimizations);

/// <summary>
/// Levels of system optimization aggressiveness.
/// </summary>
public enum OptimizationLevel
{
    /// <summary>Close basic apps only (browsers, media players).</summary>
    Minimal,

    /// <summary>Balanced optimization with moderate resource recovery.</summary>
    Standard,

    /// <summary>Close most background apps for maximum performance.</summary>
    Aggressive,

    /// <summary>Maximum performance mode - closes everything non-essential.</summary>
    Extreme
}

/// <summary>
/// Current state of system optimization.
/// </summary>
public enum OptimizationState
{
    /// <summary>No optimization applied.</summary>
    Normal,

    /// <summary>Optimization is currently being applied.</summary>
    Optimizing,

    /// <summary>System is optimized for gaming.</summary>
    Optimized,

    /// <summary>System is being restored to normal state.</summary>
    Restoring
}

/// <summary>
/// Event args for optimization state changes.
/// </summary>
public sealed class OptimizationStateChangedEventArgs : EventArgs
{
    public OptimizationState PreviousState { get; init; }
    public OptimizationState NewState { get; init; }
    public OptimizationProfile? AppliedProfile { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
