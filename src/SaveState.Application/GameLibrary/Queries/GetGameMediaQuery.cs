namespace SaveState.Application.GameLibrary.Queries;

using MediatR;
using SaveState.Core.GameLibrary.Entities;

/// <summary>
/// Query to retrieve media items for a specific game.
/// </summary>
public record GetGameMediaQuery(
    Guid GameId,
    Guid UserId,
    MediaType? MediaType = null
) : IRequest<IReadOnlyList<GameMedia>>;
