// Copyright (c) 2026 SaveStateReborn. All rights reserved.

using SaveState.Core.Common;

namespace SaveState.Core.SmartLauncher;

/// <summary>
/// Service for smart game launching with system optimization.
/// </summary>
public interface ISmartLauncherService
{
    /// <summary>
    /// Launches a game with optimizations.
    /// </summary>
    Task<LaunchResult> LaunchGameAsync(Guid gameId, Guid? profileId = null, CancellationToken ct = default);

    /// <summary>
    /// Gets available launch profiles.
    /// </summary>
    Task<IReadOnlyList<LaunchProfile>> GetProfilesAsync(Guid? gameId = null, CancellationToken ct = default);

    /// <summary>
    /// Creates a new launch profile.
    /// </summary>
    Task<Result<LaunchProfile>> CreateProfileAsync(LaunchProfile profile, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing launch profile.
    /// </summary>
    Task<Result> UpdateProfileAsync(LaunchProfile profile, CancellationToken ct = default);

    /// <summary>
    /// Deletes a launch profile.
    /// </summary>
    Task<Result> DeleteProfileAsync(Guid profileId, CancellationToken ct = default);

    /// <summary>
    /// Gets the default profile for a game.
    /// </summary>
    Task<Result<LaunchProfile>> GetDefaultProfileAsync(Guid gameId, CancellationToken ct = default);

    /// <summary>
    /// Sets the default profile for a game.
    /// </summary>
    Task<Result> SetDefaultProfileAsync(Guid gameId, Guid profileId, CancellationToken ct = default);

    /// <summary>
    /// Gets active launch session.
    /// </summary>
    Task<Result<LaunchSession>> GetActiveSessionAsync(CancellationToken ct = default);

    /// <summary>
    /// Ends an active gaming session and restores system state.
    /// </summary>
    Task<Result> EndSessionAsync(Guid sessionId, CancellationToken ct = default);

    /// <summary>
    /// Gets launch history for a game.
    /// </summary>
    Task<IReadOnlyList<LaunchSession>> GetLaunchHistoryAsync(Guid gameId, int count = 10, CancellationToken ct = default);

    /// <summary>
    /// Previews what optimizations will be applied.
    /// </summary>
    Task<IReadOnlyList<string>> PreviewOptimizationsAsync(Guid? profileId, CancellationToken ct = default);
}

/// <summary>
/// Service for system optimization during gameplay.
/// </summary>
public interface ISystemOptimizerService
{
    /// <summary>
    /// Applies optimizations before game launch.
    /// </summary>
    Task<SystemState> ApplyOptimizationsAsync(LaunchProfile profile, CancellationToken ct = default);

    /// <summary>
    /// Restores system state after gaming.
    /// </summary>
    Task RestoreSystemStateAsync(SystemState state, CancellationToken ct = default);

    /// <summary>
    /// Suspends specified processes.
    /// </summary>
    Task<List<ProcessInfo>> SuspendProcessesAsync(List<string> processNames, CancellationToken ct = default);

    /// <summary>
    /// Resumes suspended processes.
    /// </summary>
    Task ResumeProcessesAsync(List<ProcessInfo> processes, CancellationToken ct = default);

    /// <summary>
    /// Stops specified services.
    /// </summary>
    Task<List<ServiceInfo>> StopServicesAsync(List<string> serviceNames, CancellationToken ct = default);

    /// <summary>
    /// Starts services.
    /// </summary>
    Task StartServicesAsync(List<ServiceInfo> services, CancellationToken ct = default);

    /// <summary>
    /// Sets CPU priority for a process.
    /// </summary>
    Task SetProcessPriorityAsync(int processId, ProcessPriority priority, CancellationToken ct = default);

    /// <summary>
    /// Optimizes memory by clearing standby list.
    /// </summary>
    Task OptimizeMemoryAsync(CancellationToken ct = default);

    /// <summary>
    /// Disables Windows visual effects.
    /// </summary>
    Task DisableVisualEffectsAsync(CancellationToken ct = default);

    /// <summary>
    /// Enables Windows visual effects.
    /// </summary>
    Task EnableVisualEffectsAsync(CancellationToken ct = default);

    /// <summary>
    /// Sets the active power plan.
    /// </summary>
    Task SetPowerPlanAsync(string powerPlanGuid, CancellationToken ct = default);

    /// <summary>
    /// Gets the current power plan.
    /// </summary>
    Task<string> GetCurrentPowerPlanAsync(CancellationToken ct = default);

    /// <summary>
    /// Checks if all optimizations can be applied.
    /// </summary>
    Task<OptimizationCheckResult> CanApplyOptimizationsAsync(LaunchProfile profile, CancellationToken ct = default);
}

/// <summary>
/// Result of optimization capability check.
/// </summary>
public class OptimizationCheckResult
{
    public bool CanApply { get; set; }
    public List<string> Warnings { get; set; } = new();
    public List<string> Requirements { get; set; } = new();
}

/// <summary>
/// Repository for launch profile persistence.
/// </summary>
public interface ILaunchProfileRepository
{
    /// <summary>
    /// Gets all profiles for a game.
    /// </summary>
    Task<IReadOnlyList<LaunchProfile>> GetProfilesAsync(Guid? gameId = null, CancellationToken ct = default);

    /// <summary>
    /// Gets a specific profile.
    /// </summary>
    Task<Result<LaunchProfile>> GetProfileAsync(Guid profileId, CancellationToken ct = default);

    /// <summary>
    /// Gets the default profile for a game.
    /// </summary>
    Task<Result<LaunchProfile>> GetDefaultProfileAsync(Guid gameId, CancellationToken ct = default);

    /// <summary>
    /// Saves a profile.
    /// </summary>
    Task SaveProfileAsync(LaunchProfile profile, CancellationToken ct = default);

    /// <summary>
    /// Deletes a profile.
    /// </summary>
    Task DeleteProfileAsync(Guid profileId, CancellationToken ct = default);

    /// <summary>
    /// Sets the default profile for a game.
    /// </summary>
    Task SetDefaultProfileAsync(Guid gameId, Guid? profileId, CancellationToken ct = default);
}

/// <summary>
/// Repository for launch session tracking.
/// </summary>
public interface ILaunchSessionRepository
{
    /// <summary>
    /// Creates a new session.
    /// </summary>
    Task CreateSessionAsync(LaunchSession session, CancellationToken ct = default);

    /// <summary>
    /// Updates a session.
    /// </summary>
    Task UpdateSessionAsync(LaunchSession session, CancellationToken ct = default);

    /// <summary>
    /// Gets the active session.
    /// </summary>
    Task<Result<LaunchSession>> GetActiveSessionAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets session by ID.
    /// </summary>
    Task<Result<LaunchSession>> GetSessionAsync(Guid sessionId, CancellationToken ct = default);

    /// <summary>
    /// Gets launch history for a game.
    /// </summary>
    Task<IReadOnlyList<LaunchSession>> GetLaunchHistoryAsync(Guid gameId, int count, CancellationToken ct = default);

    /// <summary>
    /// Ends a session.
    /// </summary>
    Task EndSessionAsync(Guid sessionId, int? exitCode, SessionPerformanceMetrics? metrics, CancellationToken ct = default);
}

/// <summary>
/// Monitors game process and collects performance metrics.
/// </summary>
public interface IGameProcessMonitor
{
    /// <summary>
    /// Starts monitoring a game process.
    /// </summary>
    Task StartMonitoringAsync(int processId, Guid sessionId, CancellationToken ct = default);

    /// <summary>
    /// Stops monitoring.
    /// </summary>
    Task<SessionPerformanceMetrics> StopMonitoringAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets current performance metrics.
    /// </summary>
    Task<SessionPerformanceMetrics> GetCurrentMetricsAsync(CancellationToken ct = default);

    /// <summary>
    /// Event raised when process exits.
    /// </summary>
    event EventHandler<GameProcessExitedEventArgs>? ProcessExited;
}

/// <summary>
/// Event args for game process exit.
/// </summary>
public class GameProcessExitedEventArgs : EventArgs
{
    public Guid SessionId { get; set; }
    public int ProcessId { get; set; }
    public int? ExitCode { get; set; }
    public DateTime ExitTime { get; set; }
}
