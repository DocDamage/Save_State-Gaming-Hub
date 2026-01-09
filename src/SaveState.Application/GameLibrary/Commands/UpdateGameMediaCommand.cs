using MediatR;
using SaveState.Core.Common;

namespace SaveState.Application.GameLibrary.Commands;

public record UpdateGameMediaCommand(
    Guid MediaId,
    string? Title = null,
    string? Description = null,
    bool? IsFavorite = null) : IRequest<Result>;
