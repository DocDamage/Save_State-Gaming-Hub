using SaveState.Core.Analytics.DTOs;
using SaveState.Core.Common;

namespace SaveState.Core.Analytics.Services;

public interface IAnalyticsService
{
    Task<Result<GamingHeatmapData>> GetHeatmapAsync(int year, CancellationToken ct = default);
    Task<Result<IReadOnlyList<WeeklyTrend>>> GetWeeklyTrendsAsync(int weeks = 12, CancellationToken ct = default);
    Task<Result<TimeDistribution>> GetPlaytimeDistributionAsync(CancellationToken ct = default);
    Task<Result<IReadOnlyList<TopGame>>> GetTopGamesAsync(int count = 10, DateOnly? since = null, CancellationToken ct = default);
    Task<Result<AnalyticsExportData>> GetExportDataAsync(CancellationToken ct = default);
}

public sealed record WeeklyTrend(int WeekNumber, TimeSpan TotalPlaytime, int SessionCount, float ChangePercent);
public sealed record TimeDistribution(IReadOnlyDictionary<DayOfWeek, TimeSpan> ByDayOfWeek, IReadOnlyDictionary<int, TimeSpan> ByHour);
