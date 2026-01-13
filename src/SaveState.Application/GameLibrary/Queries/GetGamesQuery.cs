namespace SaveState.Application.GameLibrary.Queries;

using MediatR;
using SaveState.Core.Common;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Core.GameLibrary.Enums;

/// <summary>
/// Query to retrieve a paginated list of games with optional filtering and sorting.
/// </summary>
public record GetGamesQuery : IRequest<PagedResult<Game>>
{
    /// <summary>
    /// The page number to retrieve (1-based).
    /// </summary>
    public int PageNumber { get; init; } = 1;

    /// <summary>
    /// The number of items per page.
    /// </summary>
    public int PageSize { get; init; } = 50;

    /// <summary>
    /// Optional search term to filter games by title.
    /// </summary>
    public string? SearchTerm { get; init; }

    /// <summary>
    /// Optional platform ID to filter games.
    /// </summary>
    public Guid? PlatformId { get; init; }

    /// <summary>
    /// Optional collection ID to filter games.
    /// </summary>
    public Guid? CollectionId { get; init; }

    /// <summary>
    /// Optional game status filter.
    /// </summary>
    public GameStatus? StatusFilter { get; init; }

    /// <summary>
    /// Optional platform name filter.
    /// </summary>
    public string? PlatformFilter { get; init; }

    /// <summary>
    /// Field to sort results by.
    /// </summary>
    public GameSortBy SortBy { get; init; } = GameSortBy.Title;

    /// <summary>
    /// Whether to sort in descending order.
    /// </summary>
    public bool SortDescending { get; init; } = false;
    /// <summary>
    /// Optional ad-hoc filter for natural language search logic.
    /// </summary>
    public CollectionFilter? AdHocFilter { get; init; }
}

/// <summary>
/// Handler for GetGamesQuery that retrieves a paginated list of games from the repository.
/// </summary>
public class GetGamesQueryHandler : IRequestHandler<GetGamesQuery, PagedResult<Game>>
{
    private readonly SaveState.Core.GameLibrary.IGameRepository _repository;

    public GetGamesQueryHandler(SaveState.Core.GameLibrary.IGameRepository repository)
    {
        _repository = repository;
    }

    public async Task<PagedResult<Game>> Handle(GetGamesQuery request, CancellationToken ct)
    {
        return await _repository.GetGamesAsync(
            pageNumber: request.PageNumber,
            pageSize: request.PageSize,
            searchTerm: request.SearchTerm,
            platformId: request.PlatformId,
            collectionId: request.CollectionId,
            statusFilter: request.StatusFilter,
            platformFilter: request.PlatformFilter,
            sortBy: request.SortBy,
            sortDescending: request.SortDescending,
            adHocFilter: request.AdHocFilter,
            ct: ct).ConfigureAwait(false);
    }
}
