using MediatR;
using SaveState.Core.Common;

namespace SaveState.Application.GameLibrary.Commands;

/// <summary>
/// Command to delete a game note.
/// </summary>
public record DeleteGameNoteCommand(
    Guid NoteId,
    Guid UserId) : IRequest<Result>;
