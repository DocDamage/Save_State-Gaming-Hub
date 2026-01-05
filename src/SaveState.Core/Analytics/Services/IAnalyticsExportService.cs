using SaveState.Core.Common;

namespace SaveState.Core.Analytics.Services;

/// <summary>
/// Service for exporting analytics data in various formats.
/// </summary>
public interface IAnalyticsExportService
{
    /// <summary>
    /// Exports analytics data to CSV format.
    /// </summary>
    Task<Result<string>> ExportToCsvAsync(AnalyticsExportData data, string filePath, CancellationToken ct = default);

    /// <summary>
    /// Exports analytics data to JSON format.
    /// </summary>
    Task<Result<string>> ExportToJsonAsync(AnalyticsExportData data, string filePath, CancellationToken ct = default);

    /// <summary>
    /// Generates an HTML report.
    /// </summary>
    Task<Result<string>> GenerateHtmlReportAsync(AnalyticsExportData data, string filePath, CancellationToken ct = default);
}

/// <summary>
/// Data structure for analytics export.
/// </summary>
public record AnalyticsExportData(
    DateTime GeneratedAt,
    string TotalPlayTime,
    int TotalGames,
    int TotalSessions,
    string AverageSessionLength,
    IReadOnlyList<SessionExportData> Sessions,
    IReadOnlyList<GenreExportData> Genres,
    IReadOnlyList<StreakExportData> Streaks,
    IReadOnlyList<PredictionExportData> Predictions);

public record SessionExportData(
    string GameTitle,
    DateTime StartedAt,
    TimeSpan Duration,
    string Platform);

public record GenreExportData(
    string Genre,
    double Hours,
    double Percentage);

public record StreakExportData(
    DateTime StartDate,
    DateTime EndDate,
    int DaysCount,
    int TotalSessions);

public record PredictionExportData(
    string GameName,
    string EstimatedTimeRemaining,
    double ConfidenceScore);
