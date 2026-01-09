using SaveState.Core.Common;

namespace SaveState.Core.Analytics.Services;

/// <summary>
/// Service for advanced backlog analytics and burn-down analysis.
/// </summary>
public interface IBacklogAnalyticsService
{
    /// <summary>
    /// Gets detailed backlog burn-down data with trend analysis.
    /// </summary>
    /// <param name="monthsBack">Number of months to analyze (default: 6)</param>
    /// <param name="granularity">Data granularity (Daily, Weekly, Monthly)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Backlog burn-down analysis with trends and velocity</returns>
    Task<Result<BacklogBurndownAnalysis>> GetBurndownAnalysisAsync(
        int monthsBack = 6,
        BacklogGranularity granularity = BacklogGranularity.Weekly,
        CancellationToken ct = default);

    /// <summary>
    /// Gets completion rate trends with forecasting.
    /// </summary>
    /// <param name="monthsBack">Number of months to analyze (default: 6)</param>
    /// <param name="includeForecasting">Whether to include future projections</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Completion rate trends with optional forecasting</returns>
    Task<Result<CompletionRateTrends>> GetCompletionRateTrendsAsync(
        int monthsBack = 6,
        bool includeForecasting = true,
        CancellationToken ct = default);

    /// <summary>
    /// Calculates backlog velocity (games completed per period).
    /// </summary>
    /// <param name="weeks">Number of weeks to calculate velocity over</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Velocity metrics</returns>
    Task<Result<BacklogVelocity>> GetVelocityAsync(
        int weeks = 4,
        CancellationToken ct = default);
}

/// <summary>
/// Backlog burn-down analysis with comprehensive metrics.
/// </summary>
/// <param name="DataPoints">Time-series data points for burn-down chart</param>
/// <param name="CurrentBacklog">Current number of games in backlog</param>
/// <param name="CompletionVelocity">Average games completed per week</param>
/// <param name="EstimatedCompletionDate">Estimated date to clear backlog at current velocity</param>
/// <param name="TrendDirection">Overall trend (Increasing, Decreasing, Stable)</param>
/// <param name="TrendStrength">How strong the trend is (0-100)</param>
public record BacklogBurndownAnalysis(
    List<BacklogDataPoint> DataPoints,
    int CurrentBacklog,
    double CompletionVelocity,
    DateTime? EstimatedCompletionDate,
    TrendDirection TrendDirection,
    double TrendStrength);

/// <summary>
/// Single data point in backlog burn-down chart.
/// </summary>
/// <param name="Date">Date of the data point</param>
/// <param name="ActiveBacklog">Number of games in backlog</param>
/// <param name="CompletedCount">Number of games completed to date</param>
/// <param name="Added">Games added in this period</param>
/// <param name="Velocity">Completion velocity for this period</param>
public record BacklogDataPoint(
    DateTime Date,
    int ActiveBacklog,
    int CompletedCount,
    int Added = 0,
    double Velocity = 0);

/// <summary>
/// Completion rate trends with forecasting.
/// </summary>
/// <param name="HistoricalRates">Historical completion rate data</param>
/// <param name="CurrentRate">Current completion rate percentage</param>
/// <param name="AverageRate">Average rate over the period</param>
/// <param name="TrendDirection">Overall trend direction</param>
/// <param name="ForecastedRates">Forecasted future rates (if enabled)</param>
public record CompletionRateTrends(
    List<CompletionRateDataPoint> HistoricalRates,
    double CurrentRate,
    double AverageRate,
    TrendDirection TrendDirection,
    List<CompletionRateDataPoint>? ForecastedRates = null);

/// <summary>
/// Single completion rate data point.
/// </summary>
/// <param name="Date">Date of the measurement</param>
/// <param name="CompletionRate">Completion rate percentage (0-100)</param>
/// <param name="IsForecast">Whether this is a forecasted value</param>
public record CompletionRateDataPoint(
    DateTime Date,
    double CompletionRate,
    bool IsForecast = false);

/// <summary>
/// Backlog velocity metrics.
/// </summary>
/// <param name="GamesCompletedPerWeek">Average games completed per week</param>
/// <param name="GamesAddedPerWeek">Average games added per week</param>
/// <param name="NetVelocity">Net change in backlog per week</param>
/// <param name="IsGrowing">Whether backlog is growing</param>
/// <param name="EstimatedWeeksToComplete">Weeks to complete backlog at current velocity</param>
public record BacklogVelocity(
    double GamesCompletedPerWeek,
    double GamesAddedPerWeek,
    double NetVelocity,
    bool IsGrowing,
    int? EstimatedWeeksToComplete);

/// <summary>
/// Granularity for backlog burn-down analysis.
/// </summary>
public enum BacklogGranularity
{
    Daily,
    Weekly,
    Monthly
}

/// <summary>
/// Trend direction indicator.
/// </summary>
public enum TrendDirection
{
    Increasing,
    Decreasing,
    Stable
}
