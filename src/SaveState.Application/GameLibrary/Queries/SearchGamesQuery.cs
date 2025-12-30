using MediatR;
using SaveState.Application.Common;
using SaveState.Core.Common;
using SaveState.Application.Common.DTOs;
using SaveState.Application.GameLibrary.DTOs;
using SaveState.Core.Common.Enums;
using SaveState.Core.GameLibrary.Enums;

namespace SaveState.Application.GameLibrary.Queries;

public record SearchGamesQuery : IRequest<Result<Application.Common.DTOs.PagedResult<GameSummaryDto>>>
{
    public string? Title { get; init; }
    public string? Platform { get; init; }
    public IReadOnlyList<string>? Tags { get; init; }
    public GameStatus? Status { get; init; }
    public SortOption SortBy { get; init; } = SortOption.Title;
    public SortDirection SortDirection { get; init; } = SortDirection.Ascending;
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
