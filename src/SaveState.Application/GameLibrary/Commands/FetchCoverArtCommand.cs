using MediatR;
using SaveState.Core.Common;
using SaveState.Core.GameLibrary.Services;
using SaveState.Core.GameLibrary;

namespace SaveState.Application.GameLibrary.Commands;

public sealed record FetchCoverArtCommand(Guid GameId) : IRequest<Result<string>>;

public sealed class FetchCoverArtCommandHandler : IRequestHandler<FetchCoverArtCommand, Result<string>>
{
    private readonly ICoverArtService _coverArtService;
    private readonly IGameRepository _gameRepository;

    public FetchCoverArtCommandHandler(ICoverArtService coverArtService, IGameRepository gameRepository)
    {
        _coverArtService = coverArtService;
        _gameRepository = gameRepository;
    }

    public async Task<Result<string>> Handle(FetchCoverArtCommand request, CancellationToken ct)
    {
        var result = await _coverArtService.FetchCoverArtAsync(request.GameId, ct);
        return result.IsSuccess
            ? Result.Success<string>(result.Value!.LocalPath)
            : Result.Failure<string>(result.Error!, result.ErrorType);
    }
}
