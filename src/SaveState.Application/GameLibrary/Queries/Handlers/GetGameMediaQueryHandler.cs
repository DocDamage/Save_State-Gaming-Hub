namespace SaveState.Application.GameLibrary.Queries.Handlers;

using MediatR;
using SaveState.Core.Common.ValueObjects;
using SaveState.Core.GameLibrary;
using SaveState.Core.GameLibrary.Entities;

/// <summary>
/// Handler for retrieving game media.
/// </summary>
public class GetGameMediaQueryHandler : IRequestHandler<GetGameMediaQuery, IReadOnlyList<GameMedia>>
{
    private readonly IGameMediaRepository _mediaRepository;

    public GetGameMediaQueryHandler(IGameMediaRepository mediaRepository)
    {
        _mediaRepository = mediaRepository;
    }

    public async Task<IReadOnlyList<GameMedia>> Handle(GetGameMediaQuery request, CancellationToken cancellationToken)
    {
        var gameId = GameId.From(request.GameId);
        var userId = UserId.From(request.UserId);

        if (request.MediaType.HasValue)
        {
            var media = await _mediaRepository.GetByTypeAsync(gameId, userId, request.MediaType.Value, cancellationToken);
            return media;
        }

        var allMedia = await _mediaRepository.GetByGameIdAsync(gameId, userId, cancellationToken);
        return allMedia;
    }
}
