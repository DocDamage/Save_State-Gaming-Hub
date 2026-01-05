using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Common.ValueObjects;
using SaveState.Core.GameLibrary;

namespace SaveState.Application.GameLibrary.Commands.Handlers;

/// <summary>
/// Handler for updating game tags.
/// </summary>
public class UpdateGameTagsCommandHandler : IRequestHandler<UpdateGameTagsCommand, Result>
{
    private readonly IGameRepository _gameRepository;

    public UpdateGameTagsCommandHandler(IGameRepository gameRepository)
    {
        _gameRepository = gameRepository;
    }

    public async Task<Result> Handle(UpdateGameTagsCommand request, CancellationToken cancellationToken)
    {
        var gameId = GameId.From(request.GameId);
        var game = await _gameRepository.GetByIdAsync(gameId, cancellationToken).ConfigureAwait(false);

        if (game == null)
        {
            return Result.Failure("Game not found.");
        }

        game.UpdateTags(request.Tags);

        await _gameRepository.UpdateAsync(game, cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}
