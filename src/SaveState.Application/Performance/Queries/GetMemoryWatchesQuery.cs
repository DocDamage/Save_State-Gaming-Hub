using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Performance.Entities;

namespace SaveState.Application.Performance.Queries;

/// <summary>
/// Query to get all memory watches for a game.
/// </summary>
public sealed record GetMemoryWatchesQuery(Guid GameId) : IRequest<Result<IReadOnlyList<MemoryWatch>>>;
