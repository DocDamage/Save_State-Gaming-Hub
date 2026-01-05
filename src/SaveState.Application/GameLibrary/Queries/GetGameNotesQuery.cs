namespace SaveState.Application.GameLibrary.Queries;

using MediatR;
using SaveState.Core.GameLibrary.Entities;

/// <summary>
/// Query to retrieve notes for a specific game.
/// </summary>
public record GetGameNotesQuery(
    Guid GameId,
    Guid UserId
) : IRequest<IReadOnlyList<GameNote>>;
