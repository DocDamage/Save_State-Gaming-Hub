using MediatR;
using SaveState.Application.Common;
using SaveState.Application.GameLibrary.ReadModels;
using SaveState.Core.GameLibrary.Enums;

namespace SaveState.Application.GameLibrary.Queries;

/// <summary>
/// Query for retrieving a paginated list of game summaries.
/// Optimized for game library browsing and search.
/// </summary>
public record GetGameSummariesQuery : IRequest<Result<PagedResult<GameSummary>>>
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? SearchTerm { get; init; }
    public GameStatus? StatusFilter { get; init; }
    public string? PlatformFilter { get; init; }
    public GameSummarySortBy SortBy { get; init; } = GameSummarySortBy.Title;
    public bool SortDescending { get; init; }
}

public enum GameSummarySortBy
{
    Title,
    Platform,
    Status,
    LastPlayed,
    TotalPlayTime
}
