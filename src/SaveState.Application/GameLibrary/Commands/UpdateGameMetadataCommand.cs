using MediatR;
using SaveState.Application.Common;
using SaveState.Core.Common.ValueObjects;

namespace SaveState.Application.GameLibrary.Commands;

public record UpdateGameMetadataCommand : IRequest<Result>
{
    public GameId GameId { get; init; }
    public string? Description { get; init; }
    public IReadOnlyList<string>? Tags { get; init; }
    public string? CoverImageUrl { get; init; }
}
