using MediatR;
using SaveState.Application.Common;
using SaveState.Core.Common;
using SaveState.Application.GameLibrary.DTOs;

namespace SaveState.Application.GameLibrary.Queries;

public record GetLibraryStatisticsQuery : IRequest<Result<LibraryStatisticsDto>>
{
    public bool IncludeHidden { get; init; } = false;
}
