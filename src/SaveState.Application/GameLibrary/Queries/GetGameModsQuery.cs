namespace SaveState.Application.GameLibrary.Queries;

using MediatR;
using SaveState.Core.GameLibrary.Entities;

/// <summary>
/// Query to retrieve mods for a specific game.
/// </summary>
public record GetGameModsQuery(
    Guid GameId
) : IRequest<IReadOnlyList<GameMod>>;
