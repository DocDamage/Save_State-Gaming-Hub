using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Mugen.CharacterFusion;
using SaveState.Core.Mugen.CharacterFusion.Services;

namespace SaveState.Application.Mugen.CharacterFusion.Queries;

/// <summary>
/// Query to get the fusion leaderboard.
/// </summary>
public sealed record GetFusionLeaderboardQuery(int Top = 100) : IRequest<Result<List<FusionLeaderboardEntry>>>;

/// <summary>
/// Handler for GetFusionLeaderboardQuery.
/// </summary>
public sealed class GetFusionLeaderboardQueryHandler : IRequestHandler<GetFusionLeaderboardQuery, Result<List<FusionLeaderboardEntry>>>
{
    private readonly ICharacterFusionService _fusionService;

    public GetFusionLeaderboardQueryHandler(ICharacterFusionService fusionService)
    {
        _fusionService = fusionService;
    }

    public async Task<Result<List<FusionLeaderboardEntry>>> Handle(GetFusionLeaderboardQuery request, CancellationToken cancellationToken)
    {
        return await _fusionService.GetLeaderboardAsync(request.Top, cancellationToken);
    }
}
