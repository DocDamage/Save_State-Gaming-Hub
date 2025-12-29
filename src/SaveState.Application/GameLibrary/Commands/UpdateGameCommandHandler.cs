using MediatR;
using SaveState.Core.GameLibrary;

namespace SaveState.Application.GameLibrary.Commands;

public class UpdateGameCommandHandler : IRequestHandler<UpdateGameCommand, Unit>
{
    private readonly IGameRepository _repository;

    public UpdateGameCommandHandler(IGameRepository repository)
    {
        _repository = repository;
    }

    public async Task<Unit> Handle(UpdateGameCommand request, CancellationToken cancellationToken)
    {
        var game = await _repository.GetByIdAsync(request.GameId, cancellationToken).ConfigureAwait(false);
        if (game is null)
        {
            throw new KeyNotFoundException($"Game with ID {request.GameId} not found");
        }

        game.Update(request.Title, request.Description, request.CoverImagePath);

        await _repository.UpdateAsync(game, cancellationToken).ConfigureAwait(false);

        return Unit.Value;
    }
}
