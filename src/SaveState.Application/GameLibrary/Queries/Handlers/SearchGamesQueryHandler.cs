using MediatR;
using SaveState.Application.Common;
using SaveState.Core.Common;
using SaveState.Application.Common.DTOs;
using SaveState.Application.GameLibrary.DTOs;
using SaveState.Core.Common.ValueObjects;
using SaveState.Core.GameLibrary;
using SaveState.Core.GameLibrary.Entities;

namespace SaveState.Application.GameLibrary.Queries.Handlers;

public class SearchGamesQueryHandler : IRequestHandler<SearchGamesQuery, Result<PagedResult<GameSummaryDto>>>
{
    private readonly IGameRepository _gameRepository;

    public SearchGamesQueryHandler(IGameRepository gameRepository)
    {
        _gameRepository = gameRepository;
    }

    public async Task<Result<PagedResult<GameSummaryDto>>> Handle(SearchGamesQuery request, CancellationToken ct)
    {
        // Get all games (in a real implementation, this would be filtered and paged at the database level)
        var allGames = await _gameRepository.GetAllAsync(ct).ConfigureAwait(false);

        // Apply filters
        var filteredGames = ApplyFilters(allGames, request);

        // Apply sorting
        var sortedGames = ApplySorting(filteredGames, request);

        // Apply pagination
        var paginatedGames = sortedGames
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        // Convert to DTOs
        var dtos = paginatedGames.Select(g => new GameSummaryDto
        {
            Id = GameId.From(g.Id),
            Title = g.Title,
            Platform = g.Platform?.Name.Value ?? "Unknown",
            CoverImageUrl = g.CoverImagePath,
            Status = g.Status,
            AddedAt = g.CreatedAt
        }).ToList();

        int totalCount = filteredGames.Count();
        var result = new PagedResult<GameSummaryDto>(dtos, totalCount, 1, 20);

        return Result<PagedResult<GameSummaryDto>>.Success(result);
    }

    private static IEnumerable<Game> ApplyFilters(IEnumerable<Game> games, SearchGamesQuery request)
    {
        var query = games.AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Title))
        {
            query = query.Where(g => g.Title.Contains(request.Title, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(request.Platform))
        {
            query = query.Where(g => g.Platform != null && g.Platform.Name.Value.Contains(request.Platform, StringComparison.OrdinalIgnoreCase));
        }

        if (request.Tags?.Any() == true)
        {
            // In a real implementation, this would filter by tags
            // For now, we'll skip this filter
        }

        if (request.Status.HasValue)
        {
            query = query.Where(g => g.Status == request.Status.Value);
        }

        return query.ToList();
    }

    private static IEnumerable<Game> ApplySorting(IEnumerable<Game> games, SearchGamesQuery request)
    {
        var query = games.AsQueryable();

        query = request.SortDirection == Core.Common.Enums.SortDirection.Ascending
            ? request.SortBy switch
            {
                Core.Common.Enums.SortOption.Title => query.OrderBy(g => g.Title),
                Core.Common.Enums.SortOption.Platform => query.OrderBy(g => g.Platform != null ? g.Platform.Name.Value : ""),
                Core.Common.Enums.SortOption.AddedDate => query.OrderBy(g => g.CreatedAt),
                _ => query.OrderBy(g => g.Title)
            }
            : request.SortBy switch
            {
                Core.Common.Enums.SortOption.Title => query.OrderByDescending(g => g.Title),
                Core.Common.Enums.SortOption.Platform => query.OrderByDescending(g => g.Platform != null ? g.Platform.Name.Value : ""),
                Core.Common.Enums.SortOption.AddedDate => query.OrderByDescending(g => g.CreatedAt),
                _ => query.OrderByDescending(g => g.Title)
            };

        return query.ToList();
    }
}
