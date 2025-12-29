using MediatR;
using SaveState.Core.Common.ValueObjects;
using SaveState.Core.GameLibrary;

namespace SaveState.Application.GameLibrary.Commands;

public record InstallGameCommand(GameId GameId, string InstallPath) : IRequest<Unit>;

public class InstallGameCommandHandler : IRequestHandler<InstallGameCommand, Unit>
{
    private readonly IGameRepository _repository;

    public InstallGameCommandHandler(IGameRepository repository)
    {
        _repository = repository;
    }

    public async Task<Unit> Handle(InstallGameCommand request, CancellationToken cancellationToken)
    {
        var game = await _repository.GetByIdAsync(request.GameId, cancellationToken).ConfigureAwait(false);
        if (game == null)
            throw new KeyNotFoundException($"Game with ID {request.GameId} not found");

        game.SetInstallPath(request.InstallPath);
        await _repository.UpdateAsync(game, cancellationToken).ConfigureAwait(false);

        return Unit.Value;
    }
}
