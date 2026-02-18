// Copyright (c) 2026 SaveStateReborn. All rights reserved.

using Microsoft.Extensions.Logging;

namespace SaveState.Core.SmartLauncher.Plugins;

/// <summary>
/// Interface for plugins that extend Smart Launcher functionality.
/// </summary>
public interface ISmartLauncherPlugin
{
    /// <summary>
    /// Plugin name.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Plugin version.
    /// </summary>
    string Version { get; }

    /// <summary>
    /// Plugin description.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Called when the Smart Launcher initializes.
    /// </summary>
    Task OnLauncherInitializeAsync(ISmartLauncherContext context, CancellationToken ct = default);

    /// <summary>
    /// Called before a game launches.
    /// </summary>
    Task OnBeforeLaunchAsync(GameLaunchContext context, CancellationToken ct = default);

    /// <summary>
    /// Called after a game launches successfully.
    /// </summary>
    Task OnAfterLaunchAsync(GameLaunchContext context, CancellationToken ct = default);

    /// <summary>
    /// Called when a game session ends.
    /// </summary>
    Task OnSessionEndAsync(SessionEndContext context, CancellationToken ct = default);

    /// <summary>
    /// Called when a profile is applied.
    /// </summary>
    Task OnProfileAppliedAsync(ProfileContext context, CancellationToken ct = default);
}

/// <summary>
/// Context provided to Smart Launcher plugins.
/// </summary>
public interface ISmartLauncherContext
{
    /// <summary>
    /// Service provider for dependency injection.
    /// </summary>
    IServiceProvider Services { get; }

    /// <summary>
    /// Logger factory for creating loggers.
    /// </summary>
    ILoggerFactory LoggerFactory { get; }

    /// <summary>
    /// Registers a custom optimization step.
    /// </summary>
    void RegisterOptimizationStep(IOptimizationStep step);

    /// <summary>
    /// Registers a custom launch profile provider.
    /// </summary>
    void RegisterProfileProvider(IProfileProvider provider);
}

/// <summary>
/// Context for game launch events.
/// </summary>
public class GameLaunchContext
{
    public Guid GameId { get; set; }
    public string GameName { get; set; } = string.Empty;
    public Guid? ProfileId { get; set; }
    public LaunchProfile? Profile { get; set; }
    public Guid SessionId { get; set; }
    public int? ProcessId { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Context for session end events.
/// </summary>
public class SessionEndContext
{
    public Guid SessionId { get; set; }
    public Guid GameId { get; set; }
    public string GameName { get; set; } = string.Empty;
    public TimeSpan? Duration { get; set; }
    public int? ExitCode { get; set; }
    public SessionPerformanceMetrics? PerformanceMetrics { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Context for profile events.
/// </summary>
public class ProfileContext
{
    public Guid ProfileId { get; set; }
    public string ProfileName { get; set; } = string.Empty;
    public Guid? GameId { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Interface for custom optimization steps.
/// </summary>
public interface IOptimizationStep
{
    /// <summary>
    /// Step name.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Step description.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Priority (lower = earlier execution).
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// Applies the optimization.
    /// </summary>
    Task<OptimizationResult> ApplyAsync(IOptimizationContext context, CancellationToken ct = default);

    /// <summary>
    /// Reverses the optimization.
    /// </summary>
    Task<OptimizationResult> RevertAsync(IOptimizationContext context, CancellationToken ct = default);
}

/// <summary>
/// Context for optimization steps.
/// </summary>
public interface IOptimizationContext
{
    LaunchProfile Profile { get; }
    ILogger Logger { get; }
    Dictionary<string, object> State { get; }
}

/// <summary>
/// Result of an optimization step.
/// </summary>
public class OptimizationResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public Dictionary<string, object> Data { get; set; } = new();

    public static OptimizationResult Successful(string? message = null) =>
        new() { Success = true, Message = message };

    public static OptimizationResult Failed(string message) =>
        new() { Success = false, Message = message };
}

/// <summary>
/// Interface for custom profile providers.
/// </summary>
public interface IProfileProvider
{
    /// <summary>
    /// Provider name.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets profiles for a game.
    /// </summary>
    Task<IReadOnlyList<LaunchProfile>> GetProfilesForGameAsync(Guid gameId, CancellationToken ct = default);

    /// <summary>
    /// Gets the recommended profile for a game.
    /// </summary>
    Task<LaunchProfile?> GetRecommendedProfileAsync(Guid gameId, CancellationToken ct = default);
}
