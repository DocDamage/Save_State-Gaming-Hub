using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Mugen.AiBattleAnalysis;
using SaveState.Core.Mugen.AiBattleAnalysis.Services;

namespace SaveState.Application.Mugen.AiBattleAnalysis.Queries;

/// <summary>
/// Query to get matchup advice.
/// </summary>
public sealed record GetMatchupAdviceQuery(
    string CharacterName,
    string OpponentName) : IRequest<Result<List<CounterStrategy>>>;

/// <summary>
/// Handler for GetMatchupAdviceQuery.
/// </summary>
public sealed class GetMatchupAdviceQueryHandler : IRequestHandler<GetMatchupAdviceQuery, Result<List<CounterStrategy>>>
{
    private readonly IAiBattleAnalysisService _analysisService;

    public GetMatchupAdviceQueryHandler(IAiBattleAnalysisService analysisService)
    {
        _analysisService = analysisService;
    }

    public async Task<Result<List<CounterStrategy>>> Handle(GetMatchupAdviceQuery request, CancellationToken cancellationToken)
    {
        return await _analysisService.GetMatchupAdviceAsync(
            request.CharacterName, request.OpponentName, cancellationToken);
    }
}
