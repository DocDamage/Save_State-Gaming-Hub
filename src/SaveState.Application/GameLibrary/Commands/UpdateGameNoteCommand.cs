using MediatR;
using SaveState.Core.Common;

namespace SaveState.Application.GameLibrary.Commands;

/// <summary>
/// Command to update an existing game note.
/// </summary>
public record UpdateGameNoteCommand(
    Guid NoteId,
    Guid UserId,
    string? Title = null,
    string? Content = null,
    string? Category = null,
    List<string>? Tags = null,
    bool? IsPinned = null) : IRequest<Result>;
