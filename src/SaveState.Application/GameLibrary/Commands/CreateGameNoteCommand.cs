using MediatR;
using SaveState.Core.Common;

namespace SaveState.Application.GameLibrary.Commands;

/// <summary>
/// Command to create a new game note.
/// </summary>
public record CreateGameNoteCommand(
    Guid GameId,
    Guid UserId,
    string Title,
    string Content,
    string? Category = null,
    List<string>? Tags = null,
    bool IsPinned = false) : IRequest<Result<Guid>>;
