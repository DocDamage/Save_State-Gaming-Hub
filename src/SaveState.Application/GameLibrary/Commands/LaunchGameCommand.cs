using MediatR;
using SaveState.Application.Common;
using SaveState.Core.Common;
using SaveState.Application.Common.Options;
using SaveState.Core.Common.Interfaces;
using SaveState.Core.Common.ValueObjects;

namespace SaveState.Application.GameLibrary.Commands;

public record LaunchGameCommand : IRequest<Result<ProcessInfo>>
{
    public required GameId GameId { get; init; }
    public LaunchOptions? Options { get; init; }
}
