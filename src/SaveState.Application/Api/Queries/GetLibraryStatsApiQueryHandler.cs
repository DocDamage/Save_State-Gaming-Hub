using MediatR;
using SaveState.Core.Api.DTOs;
using SaveState.Core.Common;
using SaveState.Core.GameLibrary;
using SaveState.Core.GameLibrary.Enums;

namespace SaveState.Application.Api.Queries;

/// <summary>
/// Handler for getting library statistics through API.
/// </summary>
public class GetLibraryStatsApiQueryHandler : IRequestHandler<GetLibraryStatsApiQuery, Result<ApiLibraryStatsDto>>
{
    private readonly IGameRepository _gameRepository;

    public GetLibraryStatsApiQueryHandler(IGameRepository gameRepository)
    {
        _gameRepository = gameRepository;
    }

    public async Task<Result<ApiLibraryStatsDto>> Handle(GetLibraryStatsApiQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var games = await _gameRepository.GetAllAsync(cancellationToken);
            var platformStats = await _gameRepository.GetPlatformStatisticsAsync(cancellationToken);

            var totalPlayTime = TimeSpan.FromTicks(games.Sum(g => g.TotalPlayTime.Ticks));

            var recentlyPlayed = games
                .Where(g => g.LastPlayedAt.HasValue)
                .OrderByDescending(g => g.LastPlayedAt)
                .Take(5)
                .Select(g => new ApiGameDto(
                    Id: g.Id,
                    Title: g.Title,
                    Description: g.Description,
                    CoverImagePath: g.CoverImagePath,
                    Platform: g.Platform?.Name.ToString(),
                    Status: g.Status.ToString(),
                    LastPlayedAt: g.LastPlayedAt,
                    TotalPlayTime: g.TotalPlayTime.ToString(@"hh\:mm"),
                    UserRating: g.UserRating,
                    IsCompleted: g.IsCompleted,
                    Tags: g.Tags.ToList()))
                .ToList();

            var stats = new ApiLibraryStatsDto(
                TotalGames: games.Count,
                GamesPlayed: games.Count(g => g.LastPlayedAt.HasValue),
                GamesCompleted: games.Count(g => g.IsCompleted),
                TotalPlayTime: totalPlayTime.ToString(@"hh\:mm"),
                GamesByPlatform: platformStats.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                RecentlyPlayed: recentlyPlayed);

            return Result.Success(stats);
        }
        catch (Exception ex)
        {
            return Result.Failure<ApiLibraryStatsDto>($"Failed to get library stats: {ex.Message}");
        }
    }
}

