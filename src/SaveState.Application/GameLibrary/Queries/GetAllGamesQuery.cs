namespace SaveState.Application.GameLibrary.Queries;

using MediatR;
using SaveState.Core.GameLibrary;
using SaveState.Core.GameLibrary.Entities;

public record GetAllGamesQuery : IRequest<IReadOnlyList<Game>>;

public class GetAllGamesQueryHandler : IRequestHandler<GetAllGamesQuery, IReadOnlyList<Game>>
{
    private readonly IGameRepository _repository;

    public GetAllGamesQueryHandler(IGameRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<Game>> Handle(GetAllGamesQuery request, CancellationToken ct)
    {
        return await _repository.GetAllAsync(ct).ConfigureAwait(false);
    }
}
