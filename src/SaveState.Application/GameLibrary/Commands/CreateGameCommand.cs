namespace SaveState.Application.GameLibrary.Commands;

using MediatR;
using SaveState.Core.GameLibrary;
using SaveState.Core.GameLibrary.Entities;

public record CreateGameCommand(string Title, string? CoverImagePath) : IRequest<int>;

public class CreateGameCommandHandler : IRequestHandler<CreateGameCommand, int>
{
    private readonly IGameRepository _repository;

    public CreateGameCommandHandler(IGameRepository repository)
    {
        _repository = repository;
    }

    public async Task<int> Handle(CreateGameCommand request, CancellationToken cancellationToken)
    {
        var game = Game.Create(request.Title, null, null, request.CoverImagePath);
        await _repository.AddAsync(game, cancellationToken).ConfigureAwait(false);
        return 1; // Return affected rows
    }
}
