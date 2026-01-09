using MediatR;
using SaveState.Core.Common;
using SaveState.Core.GameLibrary;
using SaveState.Core.GameLibrary.Entities;

namespace SaveState.Application.GameLibrary.Queries.Handlers;

public class GetRandomGameQueryHandler : IRequestHandler<GetRandomGameQuery, Result<Game>>
{
    private readonly IGameRepository _repository;

    public GetRandomGameQueryHandler(IGameRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<Game>> Handle(GetRandomGameQuery request, CancellationToken ct)
    {
        // Get total count
        var count = await _repository.CountAsync(ct);
        if (count == 0)
            return Result.Failure<Game>("No games in library", ErrorType.NotFound);

        // Get random index
        var random = new Random();
        var skip = random.Next(0, count);

        // Get game at index
        var result = await _repository.GetGamesAsync(
            pageNumber: skip / 1 + 1, // Page number is 1-based. Logic here is tricky with pagination.
            pageSize: 1,
            ct: ct);

        if (result.Items.Any())
        {
            return Result.Success(result.Items.First());
        }

        return Result.Failure<Game>("Could not retrieve random game", ErrorType.NotFound);
    }
}

