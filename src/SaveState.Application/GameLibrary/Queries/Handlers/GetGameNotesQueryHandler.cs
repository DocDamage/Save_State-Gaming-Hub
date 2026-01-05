namespace SaveState.Application.GameLibrary.Queries.Handlers;

using MediatR;
using SaveState.Core.Common.ValueObjects;
using SaveState.Core.GameLibrary;
using SaveState.Core.GameLibrary.Entities;

/// <summary>
/// Handler for retrieving game notes.
/// </summary>
public class GetGameNotesQueryHandler : IRequestHandler<GetGameNotesQuery, IReadOnlyList<GameNote>>
{
    private readonly IGameNoteRepository _noteRepository;

    public GetGameNotesQueryHandler(IGameNoteRepository noteRepository)
    {
        _noteRepository = noteRepository;
    }

    public async Task<IReadOnlyList<GameNote>> Handle(GetGameNotesQuery request, CancellationToken cancellationToken)
    {
        var gameId = GameId.From(request.GameId);
        var userId = UserId.From(request.UserId);

        var notes = await _noteRepository.GetByGameIdAsync(gameId, userId, cancellationToken);
        return notes;
    }
}
