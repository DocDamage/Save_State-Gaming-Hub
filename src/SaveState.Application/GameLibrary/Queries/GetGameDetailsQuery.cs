using MediatR;
using SaveState.Application.Common;
using SaveState.Core.Common;
using SaveState.Application.GameLibrary.DTOs;
using SaveState.Core.Common.ValueObjects;

namespace SaveState.Application.GameLibrary.Queries;

public record GetGameDetailsQuery : IRequest<Result<GameDetailsDto>>
{
    public GameId GameId { get; init; }
    public bool IncludeMetadata { get; init; } = true;
}
