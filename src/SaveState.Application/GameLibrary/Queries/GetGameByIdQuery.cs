using MediatR;
using SaveState.Core.Common.ValueObjects;
using SaveState.Core.GameLibrary;
using SaveState.Core.GameLibrary.Entities;

namespace SaveState.Application.GameLibrary.Queries;

public record GetGameByIdQuery(GameId GameId) : IRequest<Game?>;

public class GetGameByIdQueryHandler : IRequestHandler<GetGameByIdQuery, Game?>
{
    private readonly IGameRepository _repository;

    public GetGameByIdQueryHandler(IGameRepository repository)
    {
        _repository = repository;
    }

    public async Task<Game?> Handle(GetGameByIdQuery request, CancellationToken cancellationToken)
    {
        return await _repository.GetByIdAsync(request.GameId, cancellationToken).ConfigureAwait(false);
    }
}
