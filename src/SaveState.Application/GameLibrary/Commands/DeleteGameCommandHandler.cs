using MediatR;
using SaveState.Core.GameLibrary;

namespace SaveState.Application.GameLibrary.Commands;

public class DeleteGameCommandHandler : IRequestHandler<DeleteGameCommand, Unit>
{
    private readonly IGameRepository _repository;

    public DeleteGameCommandHandler(IGameRepository repository)
    {
        _repository = repository;
    }

    public async Task<Unit> Handle(DeleteGameCommand request, CancellationToken cancellationToken)
    {
        var game = await _repository.GetByIdAsync(request.GameId, cancellationToken).ConfigureAwait(false);
        if (game is null)
        {
            throw new KeyNotFoundException($"Game with ID {request.GameId} not found");
        }

        // Soft delete the game
        game.MarkAsDeleted();

        await _repository.UpdateAsync(game, cancellationToken).ConfigureAwait(false);

        return Unit.Value;
    }
}
