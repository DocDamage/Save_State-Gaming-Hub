using MediatR;
using SaveState.Core.Common.ValueObjects;

namespace SaveState.Application.GameLibrary.Commands;

public record UpdateGameCommand(
    GameId GameId,
    string? Title,
    string? Description,
    string? CoverImagePath) : IRequest<Unit>;