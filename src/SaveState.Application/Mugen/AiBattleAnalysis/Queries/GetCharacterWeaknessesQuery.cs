using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Mugen.AiBattleAnalysis;
using SaveState.Core.Mugen.AiBattleAnalysis.Services;

namespace SaveState.Application.Mugen.AiBattleAnalysis.Queries;

/// <summary>
/// Query to get character weaknesses.
/// </summary>
public sealed record GetCharacterWeaknessesQuery(
    string CharacterName,
    SeverityLevel? MinSeverity = null) : IRequest<Result<List<PlayerWeakness>>>;

/// <summary>
/// Handler for GetCharacterWeaknessesQuery.
/// </summary>
public sealed class GetCharacterWeaknessesQueryHandler : IRequestHandler<GetCharacterWeaknessesQuery, Result<List<PlayerWeakness>>>
{
    private readonly IAiBattleAnalysisService _analysisService;

    public GetCharacterWeaknessesQueryHandler(IAiBattleAnalysisService analysisService)
    {
        _analysisService = analysisService;
    }

    public async Task<Result<List<PlayerWeakness>>> Handle(GetCharacterWeaknessesQuery request, CancellationToken cancellationToken)
    {
        return await _analysisService.GetCharacterWeaknessesAsync(
            request.CharacterName, request.MinSeverity, cancellationToken);
    }
}
