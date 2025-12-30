using MediatR;
using SaveState.Application.Common;
using SaveState.Core.Common;
using SaveState.Application.GameLibrary.ReadModels;
using SaveState.Core.Common.ValueObjects;

namespace SaveState.Application.GameLibrary.Queries;

public record GetGameDetailsQuery : IRequest<Result<GameDetail>>
{
    public GameId GameId { get; init; }
    public bool IncludeMetadata { get; init; } = true;
}
