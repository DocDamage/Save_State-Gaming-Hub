using MediatR;
using SaveState.Application.Common;
using SaveState.Core.Common;
using SaveState.Application.GameLibrary.ReadModels;
using SaveState.Core.GameLibrary;
using SaveState.Core.GameLibrary.DTOs;
using SaveState.Core.GameLibrary.Enums;
using SaveState.Core.Common.ValueObjects;

namespace SaveState.Application.GameLibrary.Queries.Handlers;

/// <summary>
/// Query handler for retrieving paginated game summaries.
/// Uses database projections for optimal performance.
/// </summary>
public class GetGameSummariesQueryHandler : IRequestHandler<GetGameSummariesQuery, Result<PagedResult<GameSummary>>>
{
    private readonly IGameRepository _gameRepository;

    public GetGameSummariesQueryHandler(IGameRepository gameRepository)
    {
        _gameRepository = gameRepository;
    }

    public async Task<Result<PagedResult<GameSummary>>> Handle(GetGameSummariesQuery request, CancellationToken ct)
    {
        try
        {
            // Use optimized projection query
            var projections = await _gameRepository.GetGameSummariesAsync(
                pageNumber: request.PageNumber,
                pageSize: request.PageSize,
                searchTerm: request.SearchTerm,
                statusFilter: request.StatusFilter,
                platformFilter: request.PlatformFilter,
                sortBy: MapSortBy(request.SortBy),
                sortDescending: request.SortDescending,
                ct);

            // Project to read models
            var summaries = projections.Items.Select(p => new GameSummary
            {
                Id = GameId.From(p.Id),
                Title = p.Title,
                Platform = p.PlatformName,
                Status = p.Status,
                CoverImageUrl = p.CoverImageUrl,
                LastPlayed = null, // Would be populated from separate tracking
                TotalPlayTime = TimeSpan.Zero // Would be populated from separate tracking
            }).ToList();

            var result = new PagedResult<GameSummary>(
                summaries,
                projections.TotalCount,
                projections.PageNumber,
                projections.PageSize);

            return Result.Success<PagedResult<GameSummary>>(result);
        }
        catch (Exception ex)
        {
            return Result.Failure<PagedResult<GameSummary>>($"Failed to retrieve game summaries: {ex.Message}");
        }
    }

    private static GameSortBy MapSortBy(GameSummarySortBy sortBy) => sortBy switch
    {
        GameSummarySortBy.Title => GameSortBy.Title,
        GameSummarySortBy.Platform => GameSortBy.Platform,
        GameSummarySortBy.Status => GameSortBy.Status,
        GameSummarySortBy.LastPlayed => GameSortBy.LastPlayed,
        GameSummarySortBy.TotalPlayTime => GameSortBy.PlayTime,
        _ => GameSortBy.Title
    };
}

