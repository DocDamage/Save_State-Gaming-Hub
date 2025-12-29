using MediatR;
using SaveState.Application.Common;
using SaveState.Core.Common;
using SaveState.Core.Common.ValueObjects;

namespace SaveState.Application.GameLibrary.Commands;

public record ImportGameCommand : IRequest<Result<GameId>>
{
    public string Title { get; init; } = string.Empty;
    public string PlatformName { get; init; } = string.Empty;
    public string? InstallPath { get; init; }
    public string? Source { get; init; }
    public string? SourceId { get; init; }
    public IReadOnlyList<string>? Tags { get; init; }
    public string? Description { get; init; }
    public string? CoverImageUrl { get; init; }
}
