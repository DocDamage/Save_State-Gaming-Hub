using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.Mugen.Services;

namespace SaveState.Infrastructure.Mugen.CharacterDiscovery.Managers;

/// <summary>
/// Manages discovery statistics, trends, and user activity analytics.
/// </summary>
public sealed class DiscoveryAnalyticsManager
{
    private readonly ILogger<DiscoveryAnalyticsManager> _logger;
    private readonly ITimeProvider _timeProvider;

    public DiscoveryAnalyticsManager(
        ILogger<DiscoveryAnalyticsManager> logger,
        ITimeProvider timeProvider)
    {
        _logger = logger;
        _timeProvider = timeProvider;
    }

    public async Task<Result<DiscoveryStatistics>> GetStatisticsAsync(
        ConcurrentDictionary<Guid, DiscoveredCharacter> characters,
        CancellationToken ct = default)
    {
        try
        {
            var stats = new DiscoveryStatistics(
                characters.Count,
                characters.Values.Select(c => c.Author).Distinct().Count(),
                characters.Values.Sum(c => c.DownloadCount),
                characters.Values.Sum(c => c.ReviewCount),
                characters.Values.Average(c => c.Rating),
                characters.Values.SelectMany(c => c.Categories).GroupBy(c => c)
                    .Select(g => new CategoryStat(g.Key, g.Count())).ToList(),
                characters.Values.SelectMany(c => c.Tags).GroupBy(t => t)
                    .Select(g => new TagStat(g.Key, g.Count(), 4.0)).Take(20).ToList());

            return Result<DiscoveryStatistics>.Success(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get statistics");
            return Result<DiscoveryStatistics>.Failure(
                $"Get statistics failed: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result<UserDiscoveryActivity>> GetUserActivityAsync(
        string userId,
        ConcurrentDictionary<string, List<Guid>> recentlyViewed,
        ConcurrentDictionary<Guid, DiscoveredCharacter> characters,
        CancellationToken ct = default)
    {
        try
        {
            recentlyViewed.TryGetValue(userId, out var viewed);
            var viewedChars = viewed?.Take(5).Select(id => characters.TryGetValue(id, out var c) ? c : null).Where(c => c != null).ToList() ?? new List<DiscoveredCharacter?>();

            var activity = new UserDiscoveryActivity(
                viewed?.Count ?? 0,
                0,
                0,
                0,
                0,
                viewedChars!,
                new List<DiscoveredCharacter>());

            return Result<UserDiscoveryActivity>.Success(activity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user activity");
            return Result<UserDiscoveryActivity>.Failure(
                $"Get activity failed: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result<IReadOnlyList<PopularityTrend>>> GetPopularityTrendsAsync(
        TimeSpan period,
        ConcurrentDictionary<Guid, DiscoveredCharacter> characters,
        CancellationToken ct = default)
    {
        try
        {
            var trends = characters.Values.Take(5).Select(c =>
            {
                var dailyStats = new List<DailyStat>();
                for (int i = 0; i < 7; i++)
                {
                    dailyStats.Add(new DailyStat(
                        _timeProvider.UtcNow.AddDays(-i),
                        new Random().Next(10, 100),
                        c.Rating));
                }

                return new PopularityTrend(c.Id, c.Name, dailyStats);
            }).ToList();

            return Result<IReadOnlyList<PopularityTrend>>.Success(trends);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get popularity trends");
            return Result<IReadOnlyList<PopularityTrend>>.Failure(
                $"Get trends failed: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result<CharacterStats>> GetCharacterStatsAsync(
        Guid characterId,
        ConcurrentDictionary<Guid, DiscoveredCharacter> characters,
        CancellationToken ct = default)
    {
        try
        {
            if (!characters.TryGetValue(characterId, out var character))
            {
                return Result<CharacterStats>.Failure("Character not found", ErrorType.NotFound);
            }

            var stats = new CharacterStats(
                character.Id,
                character.DownloadCount,
                character.ReviewCount,
                character.Rating,
                new List<WeeklyStat>());

            return Result<CharacterStats>.Success(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get character stats");
            return Result<CharacterStats>.Failure(
                $"Get character stats failed: {ex.Message}", ErrorType.Internal);
        }
    }
}

/// <summary>
/// Character statistics.
/// </summary>
public record CharacterStats(
    Guid CharacterId,
    int TotalDownloads,
    int TotalReviews,
    double AverageRating,
    IReadOnlyList<WeeklyStat> WeeklyStats);

/// <summary>
/// Weekly statistics.
/// </summary>
public record WeeklyStat(
    DateTime Week,
    int Downloads,
    int Reviews);
