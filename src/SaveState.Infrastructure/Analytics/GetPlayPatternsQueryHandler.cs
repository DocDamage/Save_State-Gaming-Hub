using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SaveState.Application.Analytics.Queries;
using SaveState.Core.Common;
using SaveState.Core.GameLibrary;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Core.GameLibrary.Enums;
using SaveState.Infrastructure.Persistence;

namespace SaveState.Infrastructure.Analytics;

public class GetPlayPatternsQueryHandler : IRequestHandler<GetPlayPatternsQuery, Result<PlayPatternAnalytics>>
{
    private readonly SaveStateDbContext _dbContext;
    private readonly ILogger<GetPlayPatternsQueryHandler> _logger;

    public GetPlayPatternsQueryHandler(
        SaveStateDbContext dbContext,
        ILogger<GetPlayPatternsQueryHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Result<PlayPatternAnalytics>> Handle(GetPlayPatternsQuery request, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Analyzing play patterns...");

            var sessions = await _dbContext.GameSessions
                .Where(s => s.EndedAt.HasValue)
                .OrderBy(s => s.StartedAt)
                .ToListAsync(ct);

            var games = await _dbContext.Games
                .ToListAsync(ct);

            // Fetch BacklogEntries for completion tracking
            var backlogEntries = await _dbContext.BacklogEntries
                .Include(b => b.Game)
                .ToListAsync(ct);

            if (!sessions.Any() && !games.Any())
            {
                return Result.Success<PlayPatternAnalytics>(new PlayPatternAnalytics(
                    new Dictionary<DayOfWeek, double>(),
                    new Dictionary<int, double>(),
                    new Dictionary<string, int>(),
                    new List<PlayStreakData>(),
                    new AverageSessionData(TimeSpan.Zero, 0, 0, "N/A"),
                    new List<CompletionPrediction>(),
                    new List<BacklogDataPoint>(),
                    new List<CompletionRateDataPoint>()));
            }

            // Day of week distribution
            var dayOfWeekDist = sessions.Any() ? sessions
                .GroupBy(s => s.StartedAt.DayOfWeek)
                .ToDictionary(
                    g => g.Key,
                    g => (double)g.Count() / sessions.Count * 100)
                : new Dictionary<DayOfWeek, double>();

            // Hour of day distribution
            var hourOfDayDist = sessions.Any() ? sessions
                .GroupBy(s => s.StartedAt.Hour)
                .ToDictionary(
                    g => g.Key,
                    g => (double)g.Count() / sessions.Count * 100)
                : new Dictionary<int, double>();

            // Genre playtime
            var genrePlaytime = await _dbContext.GameSessions
                .Include(s => s.Game)
                .ThenInclude(g => g.Platform)
                .Where(s => s.EndedAt.HasValue && s.Game != null && s.Game.Platform != null)
                .GroupBy(s => s.Game!.Platform!.Name)
                .Select(g => new
                {
                    Genre = g.Key,
                    TotalMinutes = g.Sum(s => (int)(s.EndedAt!.Value - s.StartedAt).TotalMinutes)
                })
                .ToDictionaryAsync(x => x.Genre.Value, x => x.TotalMinutes, ct);

            // Calculate play streaks
            var playStreaks = CalculatePlayStreaks(sessions);

            // Average session data
            AverageSessionData avgSession;
            if (sessions.Any())
            {
                var avgDuration = TimeSpan.FromMinutes(
                    sessions.Average(s => (s.EndedAt!.Value - s.StartedAt).TotalMinutes));

                var totalDays = (sessions.Max(s => s.StartedAt) - sessions.Min(s => s.StartedAt)).TotalDays;
                var sessionsPerDay = totalDays > 0 ? sessions.Count / totalDays : 0;
                var sessionsPerWeek = sessionsPerDay * 7;

                var mostPlayedHour = hourOfDayDist.OrderByDescending(x => x.Value).FirstOrDefault();
                var mostPlayedTime = mostPlayedHour.Key >= 0
                    ? $"{mostPlayedHour.Key:D2}:00 - {(mostPlayedHour.Key + 1):D2}:00"
                    : "N/A";

                avgSession = new AverageSessionData(
                    avgDuration,
                    sessionsPerDay,
                    sessionsPerWeek,
                    mostPlayedTime);
            }
            else
            {
                avgSession = new AverageSessionData(TimeSpan.Zero, 0, 0, "N/A");
            }

            // --- New Phase 1 Finalization Calculations ---

            // 1. Completion Predictions
            var predictions = games
                // Use 'Running' (2) or 'Installed' (1) as active states.
                .Where(g => g.Status == GameStatus.Running || g.Status == GameStatus.Installed)
                .Where(g => g.TotalPlayTime.TotalHours > 0)
                .Select(g => {
                    var avgTime = 15.0;
                    var remaining = Math.Max(0, avgTime - g.TotalPlayTime.TotalHours);
                    var confidence = Math.Min(1.0, g.TotalPlayTime.TotalHours / 5.0) * 100;
                    return new CompletionPrediction(g.Title, TimeSpan.FromHours(remaining), confidence);
                })
                .OrderBy(p => p.EstimatedTimeRemaining)
                .Take(5)
                .ToList();

            // 2. Backlog & Completion Rates (Last 6 Months)
            var backlogPoints = new List<BacklogDataPoint>();
            var completionRates = new List<CompletionRateDataPoint>();
            var startDate = DateTime.Now.AddMonths(-6);

            for (int i = 0; i <= 6; i++)
            {
                var date = startDate.AddMonths(i);

                // Get cumulative Backlog state at 'date'
                // Count entries that existed at 'date'
                var entriesAtTime = backlogEntries.Where(b => b.AddedAt <= date).ToList();

                // Count completed entries at 'date'
                // We use LastPlayedAt as a proxy for completion date if exact date is unknown
                var completedAtTime = entriesAtTime
                    .Count(b => b.Status == BacklogStatus.Completed &&
                                (b.Game.LastPlayedAt.HasValue && b.Game.LastPlayedAt.Value <= date));

                // Active Backlog = Total Relevant Entries - Completed Entries
                var activeBacklog = entriesAtTime.Count - completedAtTime;

                backlogPoints.Add(new BacklogDataPoint(date, activeBacklog, completedAtTime));

                var total = activeBacklog + completedAtTime;
                var rate = total > 0 ? (double)completedAtTime / total * 100 : 0;

                completionRates.Add(new CompletionRateDataPoint(date, rate));
            }

            var analytics = new PlayPatternAnalytics(
                dayOfWeekDist,
                hourOfDayDist,
                genrePlaytime,
                playStreaks,
                avgSession,
                predictions,
                backlogPoints,
                completionRates);

            _logger.LogInformation("Play pattern analysis complete. Sessions: {SessionCount}, Games: {GameCount}", sessions.Count, games.Count);
            return Result.Success<PlayPatternAnalytics>(analytics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to analyze play patterns");
            return Result.Failure<PlayPatternAnalytics>($"Failed to analyze play patterns: {ex.Message}");
        }
    }

    private List<PlayStreakData> CalculatePlayStreaks(List<Core.GameLibrary.Entities.GameSession> sessions)
    {
        var streaks = new List<PlayStreakData>();
        if (!sessions.Any()) return streaks;

        var sessionsByDate = sessions
            .GroupBy(s => s.StartedAt.Date)
            .OrderBy(g => g.Key)
            .ToList();

        DateTime? streakStart = null;
        DateTime? lastDate = null;
        int streakSessions = 0;

        foreach (var dateGroup in sessionsByDate)
        {
            var currentDate = dateGroup.Key;

            if (lastDate == null || (currentDate - lastDate.Value).TotalDays == 1)
            {
                // Continue or start streak
                if (streakStart == null)
                    streakStart = currentDate;

                lastDate = currentDate;
                streakSessions += dateGroup.Count();
            }
            else
            {
                // Streak broken, save if >= 3 days
                if (streakStart != null && lastDate != null)
                {
                    var days = (int)(lastDate.Value - streakStart.Value).TotalDays + 1;
                    if (days >= 3)
                    {
                        streaks.Add(new PlayStreakData(
                            streakStart.Value,
                            lastDate.Value,
                            days,
                            streakSessions));
                    }
                }

                // Start new streak
                streakStart = currentDate;
                lastDate = currentDate;
                streakSessions = dateGroup.Count();
            }
        }

        // Add final streak if exists
        if (streakStart != null && lastDate != null)
        {
            var days = (int)(lastDate.Value - streakStart.Value).TotalDays + 1;
            if (days >= 3)
            {
                streaks.Add(new PlayStreakData(
                    streakStart.Value,
                    lastDate.Value,
                    days,
                    streakSessions));
            }
        }

        return streaks.OrderByDescending(s => s.DaysCount).Take(10).ToList();
    }
}

