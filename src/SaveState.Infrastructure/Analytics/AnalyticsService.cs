using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using SaveState.Core.Analytics.DTOs;
using SaveState.Core.Analytics.Services;
using SaveState.Core.Common;
using SaveState.Core.Common.ValueObjects;
using SaveState.Core.GameLibrary;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Core.Common.Services;

namespace SaveState.Infrastructure.Analytics;

/// <summary>
/// Service for analyzing gaming sessions and providing insights.
/// Generates heatmaps, trends, and playtime distribution analytics.
/// </summary>
public class AnalyticsService : IAnalyticsService
{
    private readonly IGameSessionRepository _sessionRepository;
    private readonly IGameRepository _gameRepository;
    private readonly IBacklogRepository _backlogRepository;
    private readonly ICompletionPredictionService _completionPredictionService;
    private readonly IStreakCalculator _streakCalculator;
    private readonly ICacheService _cache;
    private readonly ILogger<AnalyticsService> _logger;
    private readonly ITimeProvider _timeProvider;

    public AnalyticsService(
        IGameSessionRepository sessionRepository,
        IGameRepository gameRepository,
        IBacklogRepository backlogRepository,
        ICompletionPredictionService completionPredictionService,
        IStreakCalculator streakCalculator,
        ICacheService cache,
        ILogger<AnalyticsService> logger,
        ITimeProvider? timeProvider = null)
    {
        _sessionRepository = sessionRepository;
        _gameRepository = gameRepository;
        _backlogRepository = backlogRepository;
        _completionPredictionService = completionPredictionService;
        _streakCalculator = streakCalculator;
        _cache = cache;
        _logger = logger;
        _timeProvider = timeProvider ?? SystemTimeProvider.Instance;
    }

    /// <summary>
    /// Generates a gaming heatmap showing playtime distribution across days and hours for a given year.
    /// </summary>
    /// <param name="year">The year to generate the heatmap for.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result containing the heatmap data or an error.</returns>
    public async Task<Result<GamingHeatmapData>> GetHeatmapAsync(int year, CancellationToken ct = default)
    {
        try
        {
            var cacheKey = $"analytics:heatmap:{year}";

            var heatmapData = await _cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1); // Cache for 1 hour

                var startDate = new DateTime(year, 1, 1);
                var endDate = new DateTime(year + 1, 1, 1);

                var allSessions = await GetAllSessionsInDateRangeAsync(startDate, endDate, ct);
                var activities = BuildDailyActivities(allSessions);
                var totalPlaytime = TimeSpan.FromTicks(allSessions.Sum(s => s.GetDuration(_timeProvider.Now).Ticks));

                var streakResult = _streakCalculator.Calculate(
                    activities,
                    DateOnly.FromDateTime(_timeProvider.Now));

                return new GamingHeatmapData(
                    Activities: activities,
                    TotalDays: (endDate - startDate).Days,
                    ActiveDays: activities.Count,
                    CurrentStreak: streakResult.CurrentStreak,
                    LongestStreak: streakResult.LongestStreak,
                    TotalPlaytime: totalPlaytime);
            }).ConfigureAwait(false);

            return Result.Success<GamingHeatmapData>(heatmapData);

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate heatmap for year {Year}", year);
            return Result.Failure<GamingHeatmapData>($"Failed to generate heatmap: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <summary>
    /// Retrieves weekly gaming trends showing playtime patterns over time.
    /// </summary>
    /// <param name="weeks">Number of weeks to look back (default: 12).</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result containing the weekly trends data or an error.</returns>
    public async Task<Result<IReadOnlyList<WeeklyTrend>>> GetWeeklyTrendsAsync(int weeks = 12, CancellationToken ct = default)
    {
        try
        {
            var cacheKey = $"analytics:weekly-trends:{weeks}";

            var trends = await _cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);

                var endDate = _timeProvider.Now;
                var startDate = endDate.AddDays(-weeks * 7);

                var allSessions = await GetAllSessionsInDateRangeAsync(startDate, endDate, ct);

                // Group by weeks and calculate trends
                var weeklyTrends = new List<WeeklyTrend>();
                var previousPlaytime = TimeSpan.Zero;

                for (int week = weeks - 1; week >= 0; week--)
                {
                    var weekStart = endDate.AddDays(-week * 7).Date;
                    var weekEnd = weekStart.AddDays(7);

                    var weekSessions = allSessions.Where(s =>
                        s.StartedAt >= weekStart && s.StartedAt < weekEnd).ToList();

                    var weekPlaytime = TimeSpan.FromTicks(weekSessions.Sum(s => s.GetDuration(_timeProvider.Now).Ticks));
                    var changePercent = previousPlaytime.TotalHours > 0
                        ? (float)((weekPlaytime.TotalHours - previousPlaytime.TotalHours) / previousPlaytime.TotalHours * 100)
                        : 0;

                    weeklyTrends.Add(new WeeklyTrend(
                        WeekNumber: week + 1,
                        TotalPlaytime: weekPlaytime,
                        SessionCount: weekSessions.Count,
                        ChangePercent: changePercent));

                    previousPlaytime = weekPlaytime;
                }

                return weeklyTrends.AsReadOnly();

            }).ConfigureAwait(false);

            return Result.Success<IReadOnlyList<WeeklyTrend>>(trends);

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get weekly trends for {Weeks} weeks", weeks);
            return Result.Failure<IReadOnlyList<WeeklyTrend>>($"Failed to get weekly trends: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <summary>
    /// Analyzes playtime distribution across different time periods and categories.
    /// </summary>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result containing the playtime distribution data or an error.</returns>
    public async Task<Result<TimeDistribution>> GetPlaytimeDistributionAsync(CancellationToken ct = default)
    {
        try
        {
            var cacheKey = "analytics:time-distribution";

            var distribution = await _cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(6); // Cache for 6 hours

                var allSessions = await GetAllSessionsAsync(ct);

                // Group by day of week
                var byDayOfWeek = allSessions
                    .GroupBy(s => s.StartedAt.DayOfWeek)
                    .ToDictionary(
                        g => g.Key,
                        g => TimeSpan.FromTicks(g.Sum(s => s.GetDuration(_timeProvider.Now).Ticks)));

                // Group by hour of day
                var byHour = allSessions
                    .GroupBy(s => s.StartedAt.Hour)
                    .ToDictionary(
                        g => g.Key,
                        g => TimeSpan.FromTicks(g.Sum(s => s.GetDuration(_timeProvider.Now).Ticks)));

                return new TimeDistribution(byDayOfWeek, byHour);

            }).ConfigureAwait(false);

            return Result.Success<TimeDistribution>(distribution);

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get playtime distribution");
            return Result.Failure<TimeDistribution>($"Failed to get playtime distribution: {ex.Message}", ErrorType.Internal);
        }
    }

    /// <summary>
    /// Retrieves the top games by playtime within an optional date range.
    /// </summary>
    /// <param name="count">Number of top games to return (default: 10).</param>
    /// <param name="since">Optional start date to filter games played since this date.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result containing the list of top games or an error.</returns>
    public async Task<Result<IReadOnlyList<TopGame>>> GetTopGamesAsync(int count = 10, DateOnly? since = null, CancellationToken ct = default)
    {
        try
        {
            var cacheKey = $"analytics:top-games:{count}:{since}";

            var topGames = await _cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(2); // Cache for 2 hours

                var allSessions = since.HasValue
                    ? await GetAllSessionsInDateRangeAsync(since.Value.ToDateTime(TimeOnly.MinValue), _timeProvider.Now, ct)
                    : await GetAllSessionsAsync(ct);

                var gameStats = allSessions
                    .GroupBy(s => s.GameId)
                    .Select(g => new
                    {
                        GameId = g.Key,
                        GameTitle = g.First().Game.Title, // Assuming first session has the title
                        TotalPlaytime = TimeSpan.FromTicks(g.Sum(s => s.GetDuration(_timeProvider.Now).Ticks)),
                        SessionCount = g.Count()
                    })
                    .OrderByDescending(x => x.TotalPlaytime)
                    .Take(count)
                    .Select(x => new TopGame(
                        GameId: x.GameId,
                        Title: x.GameTitle,
                        TotalPlaytime: x.TotalPlaytime,
                        SessionCount: x.SessionCount))
                    .ToList();

                return gameStats.AsReadOnly();

            }).ConfigureAwait(false);

            return Result.Success<IReadOnlyList<TopGame>>(topGames);

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get top {Count} games", count);
            return Result.Failure<IReadOnlyList<TopGame>>($"Failed to get top games: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result<AnalyticsExportData>> GetExportDataAsync(CancellationToken ct = default)
    {
        try
        {
            var allSessions = await GetAllSessionsAsync(ct);
            var games = await _gameRepository.GetAllAsync(ct);

            var totalPlayTime = TimeSpan.FromTicks(allSessions.Sum(s => s.GetDuration(_timeProvider.Now).Ticks));
            var totalGames = games.Count;
            var totalSessions = allSessions.Count;
            var avgSession = totalSessions > 0 ? totalPlayTime / totalSessions : TimeSpan.Zero;

            var sessionExports = allSessions.Select(s => new SessionExportData(
                s.Game.Title,
                s.StartedAt,
                s.GetDuration(_timeProvider.Now),
                s.Game.Platform?.Name ?? "Unknown")).ToList();

            var genreExport = BuildGenreExport(games, totalPlayTime);

            var activities = BuildDailyActivities(allSessions);
            var streakResult = _streakCalculator.Calculate(
                activities,
                DateOnly.FromDateTime(_timeProvider.Now));

            var predictions = await BuildPredictionsAsync(ct);

            var exportData = new AnalyticsExportData(
                _timeProvider.Now,
                $"{totalPlayTime.TotalHours:F1} hours",
                totalGames,
                totalSessions,
                $"{avgSession.TotalMinutes:F0} mins",
                sessionExports,
                genreExport,
                streakResult.Streaks,
                predictions);

            return Result.Success(exportData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get export data");
            return Result.Failure<AnalyticsExportData>($"Failed to get export data: {ex.Message}", ErrorType.Internal);
        }
    }

    private async Task<IReadOnlyList<GameSession>> GetAllSessionsAsync(CancellationToken ct = default)
    {
        return await _sessionRepository.GetAllAsync(ct).ConfigureAwait(false);
    }

    private async Task<IReadOnlyList<GameSession>> GetAllSessionsInDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken ct = default)
    {
        return await _sessionRepository.GetByDateRangeAsync(startDate, endDate, ct).ConfigureAwait(false);
    }

    private static ActivityLevel GetActivityLevel(TimeSpan playtime)
    {
        var hours = playtime.TotalHours;
        return hours switch
        {
            < 0.5 => ActivityLevel.Low,      // < 30 min
            < 2 => ActivityLevel.Medium,     // 30 min - 2 hours
            < 4 => ActivityLevel.High,       // 2 - 4 hours
            _ => ActivityLevel.VeryHigh      // > 4 hours
        };
    }

    private static Dictionary<DateOnly, DailyActivity> BuildDailyActivities(IReadOnlyList<GameSession> sessions)
    {
        var activities = new Dictionary<DateOnly, DailyActivity>();

        foreach (var sessionGroup in sessions.GroupBy(s => DateOnly.FromDateTime(s.StartedAt)))
        {
            var date = sessionGroup.Key;
            var sessionList = sessionGroup.ToList();
            var dailyPlaytime = TimeSpan.FromTicks(sessionList.Sum(s => s.GetDuration(DateTime.UtcNow).Ticks));
            var gamesPlayed = sessionList
                .Select(s => s.Game.Title)
                .Where(title => !string.IsNullOrWhiteSpace(title))
                .Distinct()
                .ToList();
            var activityLevel = GetActivityLevel(dailyPlaytime);

            activities[date] = new DailyActivity(
                Date: date,
                TotalPlaytime: dailyPlaytime,
                SessionCount: sessionList.Count,
                GamesPlayed: gamesPlayed,
                Level: activityLevel);
        }

        return activities;
    }

    private static IReadOnlyList<GenreExportData> BuildGenreExport(IReadOnlyList<Game> games, TimeSpan totalPlayTime)
    {
        var totalHours = totalPlayTime.TotalHours;

        return games
            .SelectMany(game => game.Genres.Select(genre => new
            {
                Genre = genre.Name,
                Hours = game.TotalPlayTime.TotalHours
            }))
            .GroupBy(entry => entry.Genre)
            .Select(group =>
            {
                var hours = group.Sum(item => item.Hours);
                var percentage = totalHours > 0
                    ? Math.Round(hours / totalHours * 100, 1)
                    : 0;

                return new GenreExportData(group.Key, hours, percentage);
            })
            .ToList();
    }

    private async Task<IReadOnlyList<PredictionExportData>> BuildPredictionsAsync(CancellationToken ct)
    {
        try
        {
            var backlog = await _backlogRepository.GetBacklogAsync(
                status: BacklogStatus.InProgress,
                pageSize: 100,
                ct: ct);

            if (!backlog.Items.Any())
            {
                return Array.Empty<PredictionExportData>();
            }

            var predictionTasks = backlog.Items.Select(async entry =>
            {
                var predictionResult = await _completionPredictionService.GetPredictionForGameAsync(
                    GameId.From(entry.GameId),
                    ct);

                if (predictionResult.IsSuccess && predictionResult.Value != null)
                {
                    return new PredictionExportData(
                        predictionResult.Value.GameName,
                        FormatPredictionDuration(predictionResult.Value.EstimatedTimeRemaining),
                        Math.Round(predictionResult.Value.ConfidenceScore, 1));
                }

                return null;
            });

            var predictions = await Task.WhenAll(predictionTasks);
            return predictions
                .Where(p => p != null)
                .Cast<PredictionExportData>()
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to include completion predictions in export");
            return Array.Empty<PredictionExportData>();
        }
    }

    private static string FormatPredictionDuration(TimeSpan duration)
    {
        if (duration.TotalHours >= 1)
        {
            return $"{duration.TotalHours:F1}h";
        }

        if (duration.TotalMinutes >= 1)
        {
            return $"{duration.TotalMinutes:F0}m";
        }

        return $"{duration.TotalSeconds:F0}s";
    }
}
