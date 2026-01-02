using Microsoft.Extensions.Logging;
using SaveState.Core.Analytics.DTOs;
using SaveState.Core.Analytics.Services;
using SaveState.Core.Common;
using SaveState.Core.GameLibrary;
using SaveState.Core.GameLibrary.Entities;

namespace SaveState.Infrastructure.Analytics;

/// <summary>
/// Service for analyzing gaming sessions and providing insights.
/// Generates heatmaps, trends, and playtime distribution analytics.
/// </summary>
public class AnalyticsService : IAnalyticsService
{
    private readonly IGameSessionRepository _sessionRepository;
    private readonly IGameRepository _gameRepository;
    private readonly ICacheService _cache;
    private readonly ILogger<AnalyticsService> _logger;

    public AnalyticsService(
        IGameSessionRepository sessionRepository,
        IGameRepository gameRepository,
        ICacheService cache,
        ILogger<AnalyticsService> logger)
    {
        _sessionRepository = sessionRepository;
        _gameRepository = gameRepository;
        _cache = cache;
        _logger = logger;
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

                // Get all sessions for the year
                var startDate = new DateTime(year, 1, 1);
                var endDate = new DateTime(year + 1, 1, 1);

                var allSessions = await GetAllSessionsInDateRangeAsync(startDate, endDate, ct);

                // Group sessions by date and calculate daily activities
                var activities = new Dictionary<DateOnly, DailyActivity>();
                var totalPlaytime = TimeSpan.Zero;

                foreach (var sessionGroup in allSessions.GroupBy(s => DateOnly.FromDateTime(s.StartedAt)))
                {
                    var date = sessionGroup.Key;
                    var sessions = sessionGroup.ToList();
                    var dailyPlaytime = TimeSpan.FromTicks(sessions.Sum(s => s.Duration.Ticks));

                    var gamesPlayed = sessions
                        .Select(s => s.Game.Title)
                        .Distinct()
                        .ToList();

                    var activityLevel = GetActivityLevel(dailyPlaytime);

                    activities[date] = new DailyActivity(
                        Date: date,
                        TotalPlaytime: dailyPlaytime,
                        SessionCount: sessions.Count,
                        GamesPlayed: gamesPlayed,
                        Level: activityLevel);

                    totalPlaytime += dailyPlaytime;
                }

                // Calculate streaks and statistics
                var activeDays = activities.Count;
                var totalDays = (endDate - startDate).Days;
                var currentStreak = CalculateCurrentStreak(activities, DateOnly.FromDateTime(DateTime.Now));
                var longestStreak = CalculateLongestStreak(activities);

                return new GamingHeatmapData(
                    Activities: activities,
                    TotalDays: totalDays,
                    ActiveDays: activeDays,
                    CurrentStreak: currentStreak,
                    LongestStreak: longestStreak,
                    TotalPlaytime: totalPlaytime);

            }).ConfigureAwait(false);

            return Result<GamingHeatmapData>.Success(heatmapData);

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate heatmap for year {Year}", year);
            return Result<GamingHeatmapData>.Failure($"Failed to generate heatmap: {ex.Message}", ErrorType.Internal);
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

                var endDate = DateTime.Now;
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

                    var weekPlaytime = TimeSpan.FromTicks(weekSessions.Sum(s => s.Duration.Ticks));
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

            return Result<IReadOnlyList<WeeklyTrend>>.Success(trends);

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get weekly trends for {Weeks} weeks", weeks);
            return Result<IReadOnlyList<WeeklyTrend>>.Failure($"Failed to get weekly trends: {ex.Message}", ErrorType.Internal);
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
                        g => TimeSpan.FromTicks(g.Sum(s => s.Duration.Ticks)));

                // Group by hour of day
                var byHour = allSessions
                    .GroupBy(s => s.StartedAt.Hour)
                    .ToDictionary(
                        g => g.Key,
                        g => TimeSpan.FromTicks(g.Sum(s => s.Duration.Ticks)));

                return new TimeDistribution(byDayOfWeek, byHour);

            }).ConfigureAwait(false);

            return Result<TimeDistribution>.Success(distribution);

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get playtime distribution");
            return Result<TimeDistribution>.Failure($"Failed to get playtime distribution: {ex.Message}", ErrorType.Internal);
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
                    ? await GetAllSessionsInDateRangeAsync(since.Value.ToDateTime(TimeOnly.MinValue), DateTime.Now, ct)
                    : await GetAllSessionsAsync(ct);

                var gameStats = allSessions
                    .GroupBy(s => s.GameId)
                    .Select(g => new
                    {
                        GameId = g.Key,
                        GameTitle = g.First().Game.Title, // Assuming first session has the title
                        TotalPlaytime = TimeSpan.FromTicks(g.Sum(s => s.Duration.Ticks)),
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

            return Result<IReadOnlyList<TopGame>>.Success(topGames);

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get top {Count} games", count);
            return Result<IReadOnlyList<TopGame>>.Failure($"Failed to get top games: {ex.Message}", ErrorType.Internal);
        }
    }

    private Task<IReadOnlyList<GameSession>> GetAllSessionsAsync(CancellationToken ct = default)
    {
        // This is a simplified implementation. In reality, we'd need to implement
        // a method to get all sessions across all games, possibly with pagination
        // For now, return empty list as this would require extending the repository
        _logger.LogWarning("GetAllSessionsAsync not fully implemented - returning empty list");
        return Task.FromResult((IReadOnlyList<GameSession>)Array.Empty<GameSession>());
    }

    private Task<IReadOnlyList<GameSession>> GetAllSessionsInDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken ct = default)
    {
        // This is a simplified implementation. In reality, we'd need to implement
        // a method to get all sessions in a date range across all games
        // For now, return empty list as this would require extending the repository
        _logger.LogWarning("GetAllSessionsInDateRangeAsync not fully implemented - returning empty list");
        return Task.FromResult((IReadOnlyList<GameSession>)Array.Empty<GameSession>());
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

    private static int CalculateCurrentStreak(IReadOnlyDictionary<DateOnly, DailyActivity> activities, DateOnly today)
    {
        var streak = 0;
        var currentDate = today;

        while (activities.ContainsKey(currentDate) && activities[currentDate].Level != ActivityLevel.None)
        {
            streak++;
            currentDate = currentDate.AddDays(-1);
        }

        return streak;
    }

    private static int CalculateLongestStreak(IReadOnlyDictionary<DateOnly, DailyActivity> activities)
    {
        var longestStreak = 0;
        var currentStreak = 0;

        foreach (var date in activities.Keys.OrderBy(d => d))
        {
            if (activities[date].Level != ActivityLevel.None)
            {
                currentStreak++;
                longestStreak = Math.Max(longestStreak, currentStreak);
            }
            else
            {
                currentStreak = 0;
            }
        }

        return longestStreak;
    }
}
