using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Mugen.AiBattleAnalysis.Services;
using AiBattleAnalysisModel = SaveState.Core.Mugen.AiBattleAnalysis.AiBattleAnalysis;

namespace SaveState.Application.Mugen.AiBattleAnalysis.Queries;

/// <summary>
/// Query to get analyses for a character.
/// </summary>
public sealed record GetCharacterAnalysesQuery(
    string CharacterName, 
    string? OpponentName = null) : IRequest<Result<List<AiBattleAnalysisModel>>>;

/// <summary>
/// Handler for GetCharacterAnalysesQuery.
/// </summary>
public sealed class GetCharacterAnalysesQueryHandler : IRequestHandler<GetCharacterAnalysesQuery, Result<List<AiBattleAnalysisModel>>>
{
    private readonly IAiBattleAnalysisService _analysisService;

    public GetCharacterAnalysesQueryHandler(IAiBattleAnalysisService analysisService)
    {
        _analysisService = analysisService;
    }

    public async Task<Result<List<AiBattleAnalysisModel>>> Handle(GetCharacterAnalysesQuery request, CancellationToken cancellationToken)
    {
        return await _analysisService.GetCharacterAnalysesAsync(
            request.CharacterName, request.OpponentName, cancellationToken);
    }
}
