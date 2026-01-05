using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Common.ValueObjects;
using SaveState.Core.GameLibrary;

namespace SaveState.Application.GameLibrary.Commands.Handlers;

/// <summary>
/// Handler for updating an existing game note.
/// </summary>
public class UpdateGameNoteCommandHandler : IRequestHandler<UpdateGameNoteCommand, Result>
{
    private readonly IGameNoteRepository _noteRepository;

    public UpdateGameNoteCommandHandler(IGameNoteRepository noteRepository)
    {
        _noteRepository = noteRepository;
    }

    public async Task<Result> Handle(UpdateGameNoteCommand request, CancellationToken cancellationToken)
    {
        var note = await _noteRepository.GetByIdAsync(request.NoteId, cancellationToken).ConfigureAwait(false);

        if (note == null)
        {
            return Result.Failure("Note not found.");
        }

        var userId = UserId.From(request.UserId);
        if (note.UserId != userId)
        {
            return Result.Failure("You don't have permission to update this note.");
        }

        note.Update(
            request.Title ?? note.Title,
            request.Content ?? note.Content,
            request.Category ?? note.Category,
            request.Tags ?? note.Tags);

        if (request.IsPinned.HasValue)
        {
            if (request.IsPinned.Value && !note.IsPinned)
            {
                note.TogglePin();
            }
            else if (!request.IsPinned.Value && note.IsPinned)
            {
                note.TogglePin();
            }
        }

        await _noteRepository.UpdateAsync(note, cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}
