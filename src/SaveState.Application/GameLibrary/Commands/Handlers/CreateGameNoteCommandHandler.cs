using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Common.ValueObjects;
using SaveState.Core.GameLibrary;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Core.GameLibrary.ValueObjects;

namespace SaveState.Application.GameLibrary.Commands.Handlers;

/// <summary>
/// Handler for creating a new game note.
/// </summary>
public class CreateGameNoteCommandHandler : IRequestHandler<CreateGameNoteCommand, Result<Guid>>
{
    private readonly IGameNoteRepository _noteRepository;

    public CreateGameNoteCommandHandler(IGameNoteRepository noteRepository)
    {
        _noteRepository = noteRepository;
    }

    public async Task<Result<Guid>> Handle(CreateGameNoteCommand request, CancellationToken cancellationToken)
    {
        var gameId = GameId.From(request.GameId);
        var userId = UserId.From(request.UserId);

        var note = GameNote.Create(
            gameId,
            userId,
            request.Title,
            request.Content,
            request.Category,
            request.Tags);

        if (request.IsPinned)
        {
            note.TogglePin();
        }

        await _noteRepository.AddAsync(note, cancellationToken).ConfigureAwait(false);

        return Result<Guid>.Success(note.Id);
    }
}
