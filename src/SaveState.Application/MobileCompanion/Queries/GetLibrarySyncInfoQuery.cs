using MediatR;
using SaveState.Core.Common;
using SaveState.Core.MobileCompanion.Models;
using SaveState.Core.MobileCompanion.Services;

namespace SaveState.Application.MobileCompanion.Queries;

public sealed record GetLibrarySyncInfoQuery : IRequest<Result<LibrarySyncInfoDto>>;

public sealed class GetLibrarySyncInfoQueryHandler : IRequestHandler<GetLibrarySyncInfoQuery, Result<LibrarySyncInfoDto>>
{
    private readonly IMobileCompanionService _companionService;

    public GetLibrarySyncInfoQueryHandler(IMobileCompanionService companionService)
    {
        _companionService = companionService;
    }

    public async Task<Result<LibrarySyncInfoDto>> Handle(GetLibrarySyncInfoQuery request, CancellationToken cancellationToken)
    {
        var result = await _companionService.GetLibrarySyncInfoAsync(cancellationToken).ConfigureAwait(false);

        if (result.IsFailure)
        {
            return Result<LibrarySyncInfoDto>.Failure(result.Error!, result.ErrorType);
        }

        var dto = new LibrarySyncInfoDto
        {
            TotalGames = result.Value.TotalGames,
            RecentlyPlayedCount = result.Value.RecentlyPlayedCount,
            InstalledCount = result.Value.InstalledCount,
            LastSyncAt = result.Value.LastSyncAt,
            RecentlyPlayed = result.Value.RecentlyPlayed.Select(MapToDto).ToList()
        };

        return Result<LibrarySyncInfoDto>.Success(dto);
    }

    private static GameSummaryDto MapToDto(GameSummary game)
    {
        return new GameSummaryDto
        {
            Id = game.Id,
            Name = game.Name,
            CoverImage = game.CoverImage,
            Platform = game.Platform,
            PlayTime = game.PlayTime,
            LastPlayed = game.LastPlayed,
            Status = game.Status
        };
    }
}
