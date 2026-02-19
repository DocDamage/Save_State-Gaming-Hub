// Copyright (c) 2026 SaveStateReborn. All rights reserved.

namespace SaveState.Core.SmartLauncher;

using SaveState.Core.Common;

/// <summary>
/// Service for tracking and analyzing Smart Launcher usage statistics.
/// </summary>
public interface ISmartLauncherStatisticsService
{
    /// <summary>
    /// Gets overall usage statistics.
    /// </summary>
    Task<Result<SmartLauncherStatistics>> GetOverallStatisticsAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets statistics for a specific game.
    /// </summary>
    Task<Result<GameLaunchStatistics>> GetGameStatisticsAsync(Guid gameId, CancellationToken ct = default);

    /// <summary>
    /// Gets statistics for a specific profile.
    /// </summary>
    Task<Result<ProfileUsageStatistics>> GetProfileStatisticsAsync(Guid profileId, CancellationToken ct = default);

    /// <summary>
    /// Gets the most played games.
    /// </summary>
    Task<Result<IReadOnlyList<MostPlayedGame>>> GetMostPlayedGamesAsync(int count = 10, CancellationToken ct = default);

    /// <summary>
    /// Gets performance comparison between optimized and non-optimized launches.
    /// </summary>
    Task<Result<PerformanceComparison>> GetPerformanceComparisonAsync(CancellationToken ct = default);

    /// <summary>
    /// Records a launch event for analytics.
    /// </summary>
    Task RecordLaunchAsync(LaunchSession session, LaunchProfile profile, CancellationToken ct = default);

    /// <summary>
    /// Records session completion with performance metrics.
    /// </summary>
    Task RecordSessionCompleteAsync(LaunchSession session, CancellationToken ct = default);
}

/// <summary>
/// Overall Smart Launcher statistics.
/// </summary>
public class SmartLauncherStatistics
{
    public int TotalSessions { get; set; }
    public TimeSpan TotalGamingTime { get; set; }
    public int TotalGamesLaunched { get; set; }
    public int TotalProfilesCreated { get; set; }
    public double AverageSessionDurationMinutes { get; set; }
    public int OptimizedLaunches { get; set; }
    public int NonOptimizedLaunches { get; set; }
    public double OptimizationAdoptionRate => TotalSessions > 0 
        ? (double)OptimizedLaunches / TotalSessions * 100 
        : 0;
    public DateTime? FirstSessionDate { get; set; }
    public DateTime? LastSessionDate { get; set; }
    public long TotalProcessesSuspended { get; set; }
    public TimeSpan TotalTimeSaved { get; set; }
}

/// <summary>
/// Statistics for a specific game.
/// </summary>
public class GameLaunchStatistics
{
    public Guid GameId { get; set; }
    public string GameName { get; set; } = string.Empty;
    public int TotalSessions { get; set; }
    public TimeSpan TotalPlayTime { get; set; }
    public DateTime? FirstPlayed { get; set; }
    public DateTime? LastPlayed { get; set; }
    public double AverageSessionDurationMinutes { get; set; }
    public int TimesLaunchedWithOptimization { get; set; }
    public List<ProfileUsageCount> ProfileUsage { get; set; } = new();
    public SessionPerformanceMetrics? BestPerformance { get; set; }
    public SessionPerformanceMetrics? AveragePerformance { get; set; }
}

/// <summary>
/// Profile usage count.
/// </summary>
public class ProfileUsageCount
{
    public Guid ProfileId { get; set; }
    public string ProfileName { get; set; } = string.Empty;
    public int UseCount { get; set; }
    public TimeSpan TotalDuration { get; set; }
}

/// <summary>
/// Statistics for a specific profile.
/// </summary>
public class ProfileUsageStatistics
{
    public Guid ProfileId { get; set; }
    public string ProfileName { get; set; } = string.Empty;
    public int TotalUses { get; set; }
    public TimeSpan TotalDuration { get; set; }
    public double AverageSessionDurationMinutes { get; set; }
    public List<GameUsageCount> GameUsage { get; set; } = new();
    public double AveragePerformanceGain { get; set; }
}

/// <summary>
/// Game usage count for a profile.
/// </summary>
public class GameUsageCount
{
    public Guid GameId { get; set; }
    public string GameName { get; set; } = string.Empty;
    public int LaunchCount { get; set; }
}

/// <summary>
/// Most played game entry.
/// </summary>
public class MostPlayedGame
{
    public Guid GameId { get; set; }
    public string GameName { get; set; } = string.Empty;
    public int SessionCount { get; set; }
    public TimeSpan TotalPlayTime { get; set; }
    public DateTime? LastPlayed { get; set; }
}

/// <summary>
/// Performance comparison between optimized and non-optimized launches.
/// </summary>
public class PerformanceComparison
{
    public OptimizedPerformanceStats Optimized { get; set; } = new();
    public OptimizedPerformanceStats NonOptimized { get; set; } = new();
    public double PerformanceImprovementPercentage { get; set; }
}

/// <summary>
/// Performance statistics for optimized/non-optimized launches.
/// </summary>
public class OptimizedPerformanceStats
{
    public int SampleCount { get; set; }
    public double? AverageFPS { get; set; }
    public double? AverageCPUUsage { get; set; }
    public double? AverageMemoryUsageMB { get; set; }
    public double? AverageSessionDurationMinutes { get; set; }
}
