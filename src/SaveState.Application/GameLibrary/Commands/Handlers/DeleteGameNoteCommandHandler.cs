using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Common.ValueObjects;
using SaveState.Core.GameLibrary;

namespace SaveState.Application.GameLibrary.Commands.Handlers;

/// <summary>
/// Handler for deleting a game note.
/// </summary>
public class DeleteGameNoteCommandHandler : IRequestHandler<DeleteGameNoteCommand, Result>
{
    private readonly IGameNoteRepository _noteRepository;

    public DeleteGameNoteCommandHandler(IGameNoteRepository noteRepository)
    {
        _noteRepository = noteRepository;
    }

    public async Task<Result> Handle(DeleteGameNoteCommand request, CancellationToken cancellationToken)
    {
        var note = await _noteRepository.GetByIdAsync(request.NoteId, cancellationToken).ConfigureAwait(false);

        if (note == null)
        {
            return Result.Failure("Note not found.");
        }

        var userId = UserId.From(request.UserId);
        if (note.UserId != userId)
        {
            return Result.Failure("You don't have permission to delete this note.");
        }

        await _noteRepository.DeleteAsync(request.NoteId, cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}
