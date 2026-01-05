using MediatR;
using SaveState.Core.Common;

namespace SaveState.Application.Analytics.Queries;

/// <summary>
/// Query to get play pattern analytics for visualization.
/// </summary>
public record GetPlayPatternsQuery : IRequest<Result<PlayPatternAnalytics>>;

/// <summary>
/// Play pattern analytics data.
/// </summary>
public record PlayPatternAnalytics(
    Dictionary<DayOfWeek, double> DayOfWeekDistribution,
    Dictionary<int, double> HourOfDayDistribution,
    Dictionary<string, int> GenrePlaytime,
    List<PlayStreakData> PlayStreaks,
    AverageSessionData AverageSession,
    List<CompletionPrediction> CompletionPredictions,
    List<BacklogDataPoint> BacklogBurndown,
    List<CompletionRateDataPoint> CompletionRates);

/// <summary>
/// Play streak information.
/// </summary>
public record PlayStreakData(
    DateTime StartDate,
    DateTime EndDate,
    int DaysCount,
    int TotalSessions);

/// <summary>
/// Average session statistics.
/// </summary>
public record AverageSessionData(
    TimeSpan AverageDuration,
    double SessionsPerDay,
    double SessionsPerWeek,
    string MostPlayedTime);

/// <summary>
/// Prediction for game completion.
/// </summary>
public record CompletionPrediction(
    string GameName,
    TimeSpan EstimatedTimeRemaining,
    double ConfidenceScore);

/// <summary>
/// Data point for backlog burn-down chart.
/// </summary>
public record BacklogDataPoint(
    DateTime Date,
    int BacklogCount,
    int CompletedCount);

/// <summary>
/// Data point for completion rate trend.
/// </summary>
public record CompletionRateDataPoint(
    DateTime Date,
    double CompletionRate);
