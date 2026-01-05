using MediatR;
using SaveState.Core.Common;

namespace SaveState.Application.GameLibrary.Commands;

/// <summary>
/// Command to update tags for a game.
/// </summary>
public record UpdateGameTagsCommand(
    Guid GameId,
    List<string> Tags) : IRequest<Result>;
