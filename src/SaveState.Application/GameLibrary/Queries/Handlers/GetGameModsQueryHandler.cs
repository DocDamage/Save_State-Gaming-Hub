namespace SaveState.Application.GameLibrary.Queries.Handlers;

using MediatR;
using SaveState.Core.Common.ValueObjects;
using SaveState.Core.GameLibrary;
using SaveState.Core.GameLibrary.Entities;

/// <summary>
/// Handler for retrieving game mods.
/// </summary>
public class GetGameModsQueryHandler : IRequestHandler<GetGameModsQuery, IReadOnlyList<GameMod>>
{
    private readonly IGameModRepository _modRepository;

    public GetGameModsQueryHandler(IGameModRepository modRepository)
    {
        _modRepository = modRepository;
    }

    public async Task<IReadOnlyList<GameMod>> Handle(GetGameModsQuery request, CancellationToken cancellationToken)
    {
        var gameId = GameId.From(request.GameId);
        var mods = await _modRepository.GetByGameIdAsync(gameId, cancellationToken);
        return mods;
    }
}
