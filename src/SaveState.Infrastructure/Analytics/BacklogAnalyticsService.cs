using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SaveState.Core.Analytics.Services;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Infrastructure.Persistence;

namespace SaveState.Infrastructure.Analytics;

/// <summary>
/// Advanced backlog analytics service with burn-down analysis and trend detection.
/// </summary>
public class BacklogAnalyticsService : IBacklogAnalyticsService
{
    private readonly SaveStateDbContext _dbContext;
    private readonly ILogger<BacklogAnalyticsService> _logger;
    private readonly ITimeProvider _timeProvider;

    public BacklogAnalyticsService(
        SaveStateDbContext dbContext,
        ILogger<BacklogAnalyticsService> logger,
        ITimeProvider? timeProvider = null)
    {
        _dbContext = dbContext;
        _logger = logger;
        _timeProvider = timeProvider ?? SystemTimeProvider.Instance;
    }

    public async Task<Result<BacklogBurndownAnalysis>> GetBurndownAnalysisAsync(
        int monthsBack = 6,
        BacklogGranularity granularity = BacklogGranularity.Weekly,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation(
                "Calculating backlog burn-down with {Granularity} granularity over {Months} months",
                granularity, monthsBack);

            var backlogEntries = await _dbContext.BacklogEntries
                .Include(b => b.Game)
                .ToListAsync(ct);

            var startDate = _timeProvider.Now.AddMonths(-monthsBack);
            var dataPoints = new List<BacklogDataPoint>();

            // Calculate data points based on granularity
            var periods = granularity switch
            {
                BacklogGranularity.Daily => GenerateDailyPeriods(startDate, _timeProvider.Now),
                BacklogGranularity.Weekly => GenerateWeeklyPeriods(startDate, _timeProvider.Now),
                BacklogGranularity.Monthly => GenerateMonthlyPeriods(startDate, _timeProvider.Now),
                _ => GenerateWeeklyPeriods(startDate, _timeProvider.Now)
            };

            int previousBacklog = 0;

            foreach (var periodStart in periods)
            {
                var periodEnd = GetPeriodEnd(periodStart, granularity);

                // Count entries that existed at this period
                var entriesAtTime = backlogEntries.Where(b => b.AddedAt <= periodEnd).ToList();

                // Count completed entries
                var completedAtTime = entriesAtTime.Count(b =>
                    b.Status == BacklogStatus.Completed &&
                    b.Game.LastPlayedAt.HasValue &&
                    b.Game.LastPlayedAt.Value <= periodEnd);

                // Games added in this period
                var addedInPeriod = backlogEntries.Count(b =>
                    b.AddedAt >= periodStart && b.AddedAt <= periodEnd);

                var activeBacklog = entriesAtTime.Count - completedAtTime;

                // Calculate velocity (change from previous period)
                var velocity = previousBacklog > 0
                    ? (double)(previousBacklog - activeBacklog) / GetPeriodDays(granularity)
                    : 0;

                dataPoints.Add(new BacklogDataPoint(
                    periodEnd,
                    activeBacklog,
                    completedAtTime,
                    addedInPeriod,
                    velocity));

                previousBacklog = activeBacklog;
            }

            // Calculate current metrics
            var currentBacklog = dataPoints.LastOrDefault()?.ActiveBacklog ?? 0;
            var completionVelocity = CalculateAverageVelocity(dataPoints);
            var estimatedCompletionDate = CalculateEstimatedCompletionDate(
                currentBacklog, completionVelocity);

            // Determine trend
            var (direction, strength) = CalculateTrend(dataPoints);

            var analysis = new BacklogBurndownAnalysis(
                dataPoints,
                currentBacklog,
                completionVelocity,
                estimatedCompletionDate,
                direction,
                strength);

            _logger.LogInformation(
                "Burn-down analysis complete: Current backlog={Backlog}, Velocity={Velocity:F2}/week, Trend={Trend}",
                currentBacklog, completionVelocity, direction);

            return Result.Success(analysis);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to calculate backlog burn-down analysis");
            return Result.Failure<BacklogBurndownAnalysis>(
                $"Failed to calculate burn-down: {ex.Message}");
        }
    }

    public async Task<Result<CompletionRateTrends>> GetCompletionRateTrendsAsync(
        int monthsBack = 6,
        bool includeForecasting = true,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Calculating completion rate trends over {Months} months", monthsBack);

            var backlogEntries = await _dbContext.BacklogEntries
                .Include(b => b.Game)
                .ToListAsync(ct);

            var startDate = _timeProvider.Now.AddMonths(-monthsBack);
            var historicalRates = new List<CompletionRateDataPoint>();

            // Calculate monthly completion rates
            var periods = GenerateMonthlyPeriods(startDate, _timeProvider.Now);

            foreach (var periodStart in periods)
            {
                var periodEnd = GetPeriodEnd(periodStart, BacklogGranularity.Monthly);

                var entriesAtTime = backlogEntries.Where(b => b.AddedAt <= periodEnd).ToList();
                var completedAtTime = entriesAtTime.Count(b =>
                    b.Status == BacklogStatus.Completed &&
                    b.Game.LastPlayedAt.HasValue &&
                    b.Game.LastPlayedAt.Value <= periodEnd);

                var total = entriesAtTime.Count;
                var rate = total > 0 ? (double)completedAtTime / total * 100 : 0;

                historicalRates.Add(new CompletionRateDataPoint(periodEnd, rate, false));
            }

            var currentRate = historicalRates.LastOrDefault()?.CompletionRate ?? 0;
            var averageRate = historicalRates.Any()
                ? historicalRates.Average(r => r.CompletionRate)
                : 0;

            var (trendDirection, _) = CalculateCompletionRateTrend(historicalRates);

            // Generate forecast if requested
            List<CompletionRateDataPoint>? forecastedRates = null;
            if (includeForecasting && historicalRates.Count >= 3)
            {
                forecastedRates = ForecastCompletionRates(historicalRates, 3);
            }

            var trends = new CompletionRateTrends(
                historicalRates,
                currentRate,
                averageRate,
                trendDirection,
                forecastedRates);

            _logger.LogInformation(
                "Completion rate trends calculated: Current={Current:F1}%, Average={Avg:F1}%, Trend={Trend}",
                currentRate, averageRate, trendDirection);

            return Result.Success(trends);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to calculate completion rate trends");
            return Result.Failure<CompletionRateTrends>(
                $"Failed to calculate trends: {ex.Message}");
        }
    }

    public async Task<Result<BacklogVelocity>> GetVelocityAsync(
        int weeks = 4,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Calculating backlog velocity over {Weeks} weeks", weeks);

            var cutoffDate = _timeProvider.Now.AddDays(-weeks * 7);

            var backlogEntries = await _dbContext.BacklogEntries
                .Include(b => b.Game)
                .ToListAsync(ct);

            var completed = backlogEntries.Count(b =>
                b.Status == BacklogStatus.Completed &&
                b.Game.LastPlayedAt.HasValue &&
                b.Game.LastPlayedAt.Value >= cutoffDate);

            var added = backlogEntries.Count(b => b.AddedAt >= cutoffDate);

            var completedPerWeek = (double)completed / weeks;
            var addedPerWeek = (double)added / weeks;
            var netVelocity = completedPerWeek - addedPerWeek;
            var isGrowing = netVelocity < 0;

            var currentBacklog = backlogEntries.Count(b =>
                b.Status != BacklogStatus.Completed &&
                b.Status != BacklogStatus.Abandoned);

            int? estimatedWeeks = completedPerWeek > 0
                ? (int)Math.Ceiling(currentBacklog / completedPerWeek)
                : null;

            var velocity = new BacklogVelocity(
                completedPerWeek,
                addedPerWeek,
                netVelocity,
                isGrowing,
                estimatedWeeks);

            _logger.LogInformation(
                "Velocity calculated: {Completed:F1} completed/week, {Added:F1} added/week, Net={Net:F1}",
                completedPerWeek, addedPerWeek, netVelocity);

            return Result.Success(velocity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to calculate backlog velocity");
            return Result.Failure<BacklogVelocity>($"Failed to calculate velocity: {ex.Message}");
        }
    }

    private List<DateTime> GenerateDailyPeriods(DateTime start, DateTime end)
    {
        var periods = new List<DateTime>();
        for (var date = start.Date; date <= end.Date; date = date.AddDays(1))
        {
            periods.Add(date);
        }
        return periods;
    }

    private List<DateTime> GenerateWeeklyPeriods(DateTime start, DateTime end)
    {
        var periods = new List<DateTime>();
        var current = start.Date;

        // Start from the beginning of the week
        while (current.DayOfWeek != DayOfWeek.Monday)
        {
            current = current.AddDays(-1);
        }

        while (current <= end)
        {
            periods.Add(current);
            current = current.AddDays(7);
        }

        return periods;
    }

    private List<DateTime> GenerateMonthlyPeriods(DateTime start, DateTime end)
    {
        var periods = new List<DateTime>();
        var current = new DateTime(start.Year, start.Month, 1);

        while (current <= end)
        {
            periods.Add(current);
            current = current.AddMonths(1);
        }

        return periods;
    }

    private DateTime GetPeriodEnd(DateTime periodStart, BacklogGranularity granularity)
    {
        return granularity switch
        {
            BacklogGranularity.Daily => periodStart.AddDays(1).AddSeconds(-1),
            BacklogGranularity.Weekly => periodStart.AddDays(7).AddSeconds(-1),
            BacklogGranularity.Monthly => periodStart.AddMonths(1).AddSeconds(-1),
            _ => periodStart.AddDays(7).AddSeconds(-1)
        };
    }

    private int GetPeriodDays(BacklogGranularity granularity)
    {
        return granularity switch
        {
            BacklogGranularity.Daily => 1,
            BacklogGranularity.Weekly => 7,
            BacklogGranularity.Monthly => 30,
            _ => 7
        };
    }

    private double CalculateAverageVelocity(List<BacklogDataPoint> dataPoints)
    {
        if (dataPoints.Count < 2) return 0;

        var velocities = dataPoints.Where(dp => dp.Velocity != 0).Select(dp => dp.Velocity).ToList();
        return velocities.Any() ? velocities.Average() : 0;
    }

    private DateTime? CalculateEstimatedCompletionDate(int currentBacklog, double velocityPerWeek)
    {
        if (velocityPerWeek <= 0 || currentBacklog <= 0) return null;

        var weeksToComplete = currentBacklog / velocityPerWeek;
        return _timeProvider.Now.AddDays(weeksToComplete * 7);
    }

    private (TrendDirection direction, double strength) CalculateTrend(List<BacklogDataPoint> dataPoints)
    {
        if (dataPoints.Count < 3)
        {
            return (TrendDirection.Stable, 0);
        }

        // Simple linear regression to determine trend
        var xValues = Enumerable.Range(0, dataPoints.Count).Select(i => (double)i).ToList();
        var yValues = dataPoints.Select(dp => (double)dp.ActiveBacklog).ToList();

        var slope = CalculateSlope(xValues, yValues);
        var strength = Math.Abs(slope) * 10; // Scale for readability

        var direction = slope < -0.5 ? TrendDirection.Decreasing
            : slope > 0.5 ? TrendDirection.Increasing
            : TrendDirection.Stable;

        return (direction, Math.Min(100, strength));
    }

    private (TrendDirection direction, double strength) CalculateCompletionRateTrend(
        List<CompletionRateDataPoint> dataPoints)
    {
        if (dataPoints.Count < 3)
        {
            return (TrendDirection.Stable, 0);
        }

        var xValues = Enumerable.Range(0, dataPoints.Count).Select(i => (double)i).ToList();
        var yValues = dataPoints.Select(dp => dp.CompletionRate).ToList();

        var slope = CalculateSlope(xValues, yValues);
        var strength = Math.Abs(slope) * 10;

        var direction = slope > 0.5 ? TrendDirection.Increasing
            : slope < -0.5 ? TrendDirection.Decreasing
            : TrendDirection.Stable;

        return (direction, Math.Min(100, strength));
    }

    private double CalculateSlope(List<double> xValues, List<double> yValues)
    {
        var n = xValues.Count;
        var xMean = xValues.Average();
        var yMean = yValues.Average();

        var numerator = xValues.Zip(yValues, (x, y) => (x - xMean) * (y - yMean)).Sum();
        var denominator = xValues.Sum(x => Math.Pow(x - xMean, 2));

        return denominator != 0 ? numerator / denominator : 0;
    }

    private List<CompletionRateDataPoint> ForecastCompletionRates(
        List<CompletionRateDataPoint> historicalRates,
        int monthsAhead)
    {
        // Simple linear forecasting based on trend
        var xValues = Enumerable.Range(0, historicalRates.Count).Select(i => (double)i).ToList();
        var yValues = historicalRates.Select(dp => dp.CompletionRate).ToList();

        var slope = CalculateSlope(xValues, yValues);
        var intercept = yValues.Average() - slope * xValues.Average();

        var forecastedRates = new List<CompletionRateDataPoint>();
        var lastDate = historicalRates.Last().Date;

        for (int i = 1; i <= monthsAhead; i++)
        {
            var futureDate = lastDate.AddMonths(i);
            var xValue = historicalRates.Count + i - 1;
            var forecastedRate = Math.Max(0, Math.Min(100, slope * xValue + intercept));

            forecastedRates.Add(new CompletionRateDataPoint(futureDate, forecastedRate, true));
        }

        return forecastedRates;
    }
}

