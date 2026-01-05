namespace SaveState.Application.GameLibrary.Queries;

using MediatR;
using SaveState.Core.GameLibrary.Entities;

/// <summary>
/// Query to retrieve game sessions for a specific game.
/// </summary>
public record GetGameSessionsQuery(
    Guid GameId,
    int Limit = 50
) : IRequest<IReadOnlyList<GameSession>>;
