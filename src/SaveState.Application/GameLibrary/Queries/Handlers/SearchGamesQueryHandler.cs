using MediatR;
using SaveState.Application.Common;
using SaveState.Application.Common.DTOs;
using SaveState.Application.GameLibrary.DTOs;
using SaveState.Core.Common.ValueObjects;
using SaveState.Core.GameLibrary;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Core.GameLibrary.Enums;

namespace SaveState.Application.GameLibrary.Queries.Handlers;

/// <summary>
/// Handler for searching games in the library.
/// Supports text search, filtering, and pagination of game results.
/// </summary>
public class SearchGamesQueryHandler : IRequestHandler<SearchGamesQuery, Result<Application.Common.DTOs.PagedResult<GameSummaryDto>>>
{
    private readonly IGameRepository _gameRepository;

    public SearchGamesQueryHandler(IGameRepository gameRepository)
    {
        _gameRepository = gameRepository;
    }

    /// <summary>
    /// Handles the query to search for games.
    /// </summary>
    /// <param name="request">The search games query with search criteria.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result containing the paged search results or an error.</returns>
    public async Task<Result<Application.Common.DTOs.PagedResult<GameSummaryDto>>> Handle(SearchGamesQuery request, CancellationToken ct)
    {
        // Map SortOption to GameSortBy
        var gameSortBy = request.SortBy switch
        {
            Core.Common.Enums.SortOption.Title => GameSortBy.Title,
            Core.Common.Enums.SortOption.Platform => GameSortBy.Platform,
            Core.Common.Enums.SortOption.AddedDate => GameSortBy.DateAdded,
            Core.Common.Enums.SortOption.LastPlayed => GameSortBy.LastPlayed,
            Core.Common.Enums.SortOption.PlayTime => GameSortBy.PlayTime,
            Core.Common.Enums.SortOption.Status => GameSortBy.Status,
            _ => GameSortBy.Title
        };

        // Use the new paginated repository method
        var pagedResult = await _gameRepository.GetGamesAsync(
            pageNumber: request.Page,
            pageSize: request.PageSize,
            searchTerm: request.Title, // Use Title as search term
            sortBy: gameSortBy,
            ct: ct).ConfigureAwait(false);

        // Convert to DTOs
        var dtos = pagedResult.Items.Select(g => new GameSummaryDto
        {
            Id = GameId.From(g.Id),
            Title = g.Title,
            Platform = g.Platform?.Name.Value ?? "Unknown",
            CoverImageUrl = g.CoverImagePath,
            Status = g.Status,
            AddedAt = g.CreatedAt
        }).ToList();

        var result = new SaveState.Application.Common.DTOs.PagedResult<GameSummaryDto>(dtos, pagedResult.TotalCount, request.Page, request.PageSize);

        return Result<SaveState.Application.Common.DTOs.PagedResult<GameSummaryDto>>.Success(result);
    }

}
