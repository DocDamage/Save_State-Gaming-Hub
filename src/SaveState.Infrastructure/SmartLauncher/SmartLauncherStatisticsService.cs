// Copyright (c) 2026 SaveStateReborn. All rights reserved.

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SaveState.Core.SmartLauncher;
using SaveState.Infrastructure.Persistence;

namespace SaveState.Infrastructure.SmartLauncher;

/// <summary>
/// Implementation of Smart Launcher statistics service.
/// </summary>
public sealed class SmartLauncherStatisticsService : ISmartLauncherStatisticsService
{
    private readonly SaveStateDbContext _dbContext;
    private readonly ILogger<SmartLauncherStatisticsService> _logger;

    public SmartLauncherStatisticsService(
        SaveStateDbContext dbContext,
        ILogger<SmartLauncherStatisticsService> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<SmartLauncherStatistics> GetOverallStatisticsAsync(CancellationToken ct = default)
    {
        try
        {
            var sessions = await _dbContext.LaunchSessions
                .AsNoTracking()
                .ToListAsync(ct);

            var profiles = await _dbContext.LaunchProfiles
                .AsNoTracking()
                .CountAsync(ct);

            var optimizedSessions = sessions
                .Where(s => s.ProfileId.HasValue)
                .ToList();

            var totalDuration = sessions
                .Where(s => s.Duration.HasValue)
                .Sum(s => s.Duration!.Value.TotalMinutes);

            var uniqueGames = sessions
                .Select(s => s.GameId)
                .Distinct()
                .Count();

            return new SmartLauncherStatistics
            {
                TotalSessions = sessions.Count,
                TotalGamingTime = TimeSpan.FromMinutes(totalDuration),
                TotalGamesLaunched = uniqueGames,
                TotalProfilesCreated = profiles,
                AverageSessionDurationMinutes = sessions.Count > 0 
                    ? totalDuration / sessions.Count 
                    : 0,
                OptimizedLaunches = optimizedSessions.Count,
                NonOptimizedLaunches = sessions.Count - optimizedSessions.Count,
                FirstSessionDate = sessions.MinBy(s => s.StartedAt)?.StartedAt,
                LastSessionDate = sessions.MaxBy(s => s.StartedAt)?.StartedAt,
                TotalProcessesSuspended = await CalculateTotalProcessesSuspendedAsync(ct),
                TotalTimeSaved = CalculateTimeSaved(optimizedSessions)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get overall statistics");
            return new SmartLauncherStatistics();
        }
    }

    /// <inheritdoc />
    public async Task<GameLaunchStatistics> GetGameStatisticsAsync(Guid gameId, CancellationToken ct = default)
    {
        try
        {
            var sessions = await _dbContext.LaunchSessions
                .AsNoTracking()
                .Where(s => s.GameId == gameId)
                .ToListAsync(ct);

            if (!sessions.Any())
            {
                return new GameLaunchStatistics { GameId = gameId };
            }

            var gameName = sessions.First().GameName;
            var totalDuration = sessions
                .Where(s => s.Duration.HasValue)
                .Sum(s => s.Duration!.Value.TotalMinutes);

            var optimizedCount = sessions.Count(s => s.ProfileId.HasValue);

            // Calculate profile usage
            var profileUsage = sessions
                .Where(s => s.ProfileId.HasValue)
                .GroupBy(s => s.ProfileId!.Value)
                .Select(g => new ProfileUsageCount
                {
                    ProfileId = g.Key,
                    ProfileName = "Unknown", // Would need to join with profiles
                    UseCount = g.Count(),
                    TotalDuration = TimeSpan.FromMinutes(g.Sum(s => s.Duration?.TotalMinutes ?? 0))
                })
                .ToList();

            // Calculate performance metrics
            var sessionsWithMetrics = sessions
                .Where(s => s.PerformanceMetrics != null)
                .ToList();

            SessionPerformanceMetrics? bestPerformance = null;
            SessionPerformanceMetrics? avgPerformance = null;

            if (sessionsWithMetrics.Any())
            {
                bestPerformance = new SessionPerformanceMetrics
                {
                    AverageFPS = sessionsWithMetrics.Max(s => s.PerformanceMetrics!.AverageFPS),
                    PeakMemoryMB = sessionsWithMetrics.Max(s => s.PerformanceMetrics!.PeakMemoryMB)
                };

                avgPerformance = new SessionPerformanceMetrics
                {
                    AverageFPS = sessionsWithMetrics.Average(s => s.PerformanceMetrics!.AverageFPS ?? 0),
                    AverageCPUUsage = sessionsWithMetrics.Average(s => s.PerformanceMetrics!.AverageCPUUsage ?? 0),
                    PeakMemoryMB = (long?)sessionsWithMetrics.Average(s => s.PerformanceMetrics!.PeakMemoryMB ?? 0)
                };
            }

            return new GameLaunchStatistics
            {
                GameId = gameId,
                GameName = gameName,
                TotalSessions = sessions.Count,
                TotalPlayTime = TimeSpan.FromMinutes(totalDuration),
                FirstPlayed = sessions.MinBy(s => s.StartedAt)?.StartedAt,
                LastPlayed = sessions.MaxBy(s => s.StartedAt)?.StartedAt,
                AverageSessionDurationMinutes = sessions.Count > 0 
                    ? totalDuration / sessions.Count 
                    : 0,
                TimesLaunchedWithOptimization = optimizedCount,
                ProfileUsage = profileUsage,
                BestPerformance = bestPerformance,
                AveragePerformance = avgPerformance
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get game statistics for {GameId}", gameId);
            return new GameLaunchStatistics { GameId = gameId };
        }
    }

    /// <inheritdoc />
    public async Task<ProfileUsageStatistics> GetProfileStatisticsAsync(Guid profileId, CancellationToken ct = default)
    {
        try
        {
            var profile = await _dbContext.LaunchProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == profileId, ct);

            var sessions = await _dbContext.LaunchSessions
                .AsNoTracking()
                .Where(s => s.ProfileId == profileId)
                .ToListAsync(ct);

            if (profile == null || !sessions.Any())
            {
                return new ProfileUsageStatistics { ProfileId = profileId };
            }

            var totalDuration = sessions
                .Where(s => s.Duration.HasValue)
                .Sum(s => s.Duration!.Value.TotalMinutes);

            var gameUsage = sessions
                .GroupBy(s => s.GameId)
                .Select(g => new GameUsageCount
                {
                    GameId = g.Key,
                    GameName = g.First().GameName,
                    LaunchCount = g.Count()
                })
                .ToList();

            return new ProfileUsageStatistics
            {
                ProfileId = profileId,
                ProfileName = profile.Name,
                TotalUses = sessions.Count,
                TotalDuration = TimeSpan.FromMinutes(totalDuration),
                AverageSessionDurationMinutes = sessions.Count > 0 
                    ? totalDuration / sessions.Count 
                    : 0,
                GameUsage = gameUsage,
                AveragePerformanceGain = profile.EstimatedPerformanceGain ?? 0
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get profile statistics for {ProfileId}", profileId);
            return new ProfileUsageStatistics { ProfileId = profileId };
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<MostPlayedGame>> GetMostPlayedGamesAsync(int count = 10, CancellationToken ct = default)
    {
        try
        {
            var games = await _dbContext.LaunchSessions
                .AsNoTracking()
                .GroupBy(s => new { s.GameId, s.GameName })
                .Select(g => new MostPlayedGame
                {
                    GameId = g.Key.GameId,
                    GameName = g.Key.GameName,
                    SessionCount = g.Count(),
                    TotalPlayTime = TimeSpan.FromMinutes(
                        g.Where(s => s.Duration.HasValue)
                         .Sum(s => s.Duration!.Value.TotalMinutes)),
                    LastPlayed = g.Max(s => s.StartedAt)
                })
                .OrderByDescending(g => g.TotalPlayTime)
                .Take(count)
                .ToListAsync(ct);

            return games;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get most played games");
            return new List<MostPlayedGame>();
        }
    }

    /// <inheritdoc />
    public async Task<PerformanceComparison> GetPerformanceComparisonAsync(CancellationToken ct = default)
    {
        try
        {
            var optimizedSessions = await _dbContext.LaunchSessions
                .AsNoTracking()
                .Where(s => s.ProfileId.HasValue && s.PerformanceMetrics != null)
                .ToListAsync(ct);

            var nonOptimizedSessions = await _dbContext.LaunchSessions
                .AsNoTracking()
                .Where(s => !s.ProfileId.HasValue && s.PerformanceMetrics != null)
                .ToListAsync(ct);

            var optimizedStats = CalculatePerformanceStats(optimizedSessions);
            var nonOptimizedStats = CalculatePerformanceStats(nonOptimizedSessions);

            double improvementPercentage = 0;
            if (optimizedStats.AverageFPS.HasValue && nonOptimizedStats.AverageFPS.HasValue 
                && nonOptimizedStats.AverageFPS.Value > 0)
            {
                improvementPercentage = 
                    (optimizedStats.AverageFPS.Value - nonOptimizedStats.AverageFPS.Value) 
                    / nonOptimizedStats.AverageFPS.Value * 100;
            }

            return new PerformanceComparison
            {
                Optimized = optimizedStats,
                NonOptimized = nonOptimizedStats,
                PerformanceImprovementPercentage = improvementPercentage
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get performance comparison");
            return new PerformanceComparison();
        }
    }

    /// <inheritdoc />
    public Task RecordLaunchAsync(LaunchSession session, LaunchProfile profile, CancellationToken ct = default)
    {
        // Statistics are calculated on-demand from session data
        // This method exists for future real-time analytics
        _logger.LogDebug("Recorded launch: {GameName} with profile {ProfileName}", 
            session.GameName, profile.Name);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task RecordSessionCompleteAsync(LaunchSession session, CancellationToken ct = default)
    {
        // Statistics are calculated on-demand from session data
        // This method exists for future real-time analytics
        _logger.LogDebug("Recorded session completion: {SessionId}, Duration: {Duration}", 
            session.Id, session.Duration);
        return Task.CompletedTask;
    }

    private static OptimizedPerformanceStats CalculatePerformanceStats(List<LaunchSession> sessions)
    {
        if (!sessions.Any())
        {
            return new OptimizedPerformanceStats();
        }

        var metrics = sessions
            .Where(s => s.PerformanceMetrics != null)
            .Select(s => s.PerformanceMetrics!)
            .ToList();

        if (!metrics.Any())
        {
            return new OptimizedPerformanceStats { SampleCount = sessions.Count };
        }

        return new OptimizedPerformanceStats
        {
            SampleCount = sessions.Count,
            AverageFPS = metrics.Average(m => m.AverageFPS ?? 0) > 0 
                ? metrics.Average(m => m.AverageFPS ?? 0) 
                : null,
            AverageCPUUsage = metrics.Average(m => m.AverageCPUUsage ?? 0) > 0 
                ? metrics.Average(m => m.AverageCPUUsage ?? 0) 
                : null,
            AverageMemoryUsageMB = metrics.Average(m => m.PeakMemoryMB ?? 0) > 0 
                ? metrics.Average(m => m.PeakMemoryMB ?? 0) 
                : null,
            AverageSessionDurationMinutes = sessions.Average(s => s.Duration?.TotalMinutes ?? 0)
        };
    }

    private async Task<long> CalculateTotalProcessesSuspendedAsync(CancellationToken ct)
    {
        // This would require storing suspended process counts in sessions
        // For now, return an estimate based on profile usage
        var profiles = await _dbContext.LaunchProfiles
            .AsNoTracking()
            .ToListAsync(ct);

        var sessions = await _dbContext.LaunchSessions
            .AsNoTracking()
            .Where(s => s.ProfileId.HasValue)
            .CountAsync(ct);

        var avgProcessesPerProfile = profiles.Any() 
            ? profiles.Average(p => p.ProcessesToSuspend.Count) 
            : 0;

        return (long)(sessions * avgProcessesPerProfile);
    }

    private static TimeSpan CalculateTimeSaved(List<LaunchSession> optimizedSessions)
    {
        // Estimate time saved based on performance gains
        // This is a rough estimate: 5% time saved per session on average
        var totalDuration = optimizedSessions
            .Where(s => s.Duration.HasValue)
            .Sum(s => s.Duration!.Value.TotalMinutes);

        return TimeSpan.FromMinutes(totalDuration * 0.05);
    }
}
