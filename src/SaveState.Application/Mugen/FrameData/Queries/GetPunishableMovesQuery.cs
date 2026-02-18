using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Mugen.CharacterFrameAnalysis;
using SaveState.Core.Mugen.CharacterFrameAnalysis.Services;

namespace SaveState.Application.Mugen.FrameData.Queries;

/// <summary>
/// Query to get punishable moves for a character.
/// </summary>
public sealed record GetPunishableMovesQuery(
    string CharacterName, 
    int PlayerSpeed = 5) : IRequest<Result<List<PunishableMove>>>;

/// <summary>
/// Handler for GetPunishableMovesQuery.
/// </summary>
public sealed class GetPunishableMovesQueryHandler : IRequestHandler<GetPunishableMovesQuery, Result<List<PunishableMove>>>
{
    private readonly IFrameDataService _frameDataService;

    public GetPunishableMovesQueryHandler(IFrameDataService frameDataService)
    {
        _frameDataService = frameDataService;
    }

    public async Task<Result<List<PunishableMove>>> Handle(GetPunishableMovesQuery request, CancellationToken cancellationToken)
    {
        return await _frameDataService.GetPunishableMovesAsync(
            request.CharacterName, request.PlayerSpeed, cancellationToken);
    }
}
