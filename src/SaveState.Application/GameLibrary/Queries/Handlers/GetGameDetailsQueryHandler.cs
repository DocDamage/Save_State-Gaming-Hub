using MediatR;
using SaveState.Application.Common;
using SaveState.Core.Common;
using SaveState.Application.GameLibrary.DTOs;
using SaveState.Core.Common.ValueObjects;
using SaveState.Core.GameLibrary;

namespace SaveState.Application.GameLibrary.Queries.Handlers;

public class GetGameDetailsQueryHandler : IRequestHandler<GetGameDetailsQuery, Result<GameDetailsDto>>
{
    private readonly IGameRepository _gameRepository;

    public GetGameDetailsQueryHandler(IGameRepository gameRepository)
    {
        _gameRepository = gameRepository;
    }

    public async Task<Result<GameDetailsDto>> Handle(GetGameDetailsQuery request, CancellationToken ct)
    {
        var game = await _gameRepository.GetByIdAsync(request.GameId, ct).ConfigureAwait(false);

        if (game is null)
            return Result<GameDetailsDto>.Failure("Game not found");

        var dto = new GameDetailsDto
        {
            Id = GameId.From(game.Id),
            Title = game.Title,
            Description = game.Description,
            Platform = game.Platform?.Name.Value ?? "Unknown",
            InstallPath = game.InstallPath,
            Source = game.Source,
            SourceId = game.SourceId,
            LastPlayed = null, // Would be tracked separately
            TotalPlayTime = TimeSpan.Zero, // Would be tracked separately
            CoverImageUrl = game.CoverImagePath,
            Status = game.Status,
            Tags = Array.Empty<string>(), // Would be populated from related entities
            Files = game.Files.Select(f => new GameFileDto
            {
                Path = f.Path,
                FileName = f.FileName,
                FileSize = f.FileSize
            }).ToArray()
        };

        return Result<GameDetailsDto>.Success(dto);
    }
}
