using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Mugen.AiBattleAnalysis.Services;

namespace SaveState.Application.Mugen.AiBattleAnalysis.Queries;

/// <summary>
/// Query to get performance trend for a character.
/// </summary>
public sealed record GetPerformanceTrendQuery(
    string CharacterName,
    DateTime? Since = null) : IRequest<Result<PerformanceTrend>>;

/// <summary>
/// Handler for GetPerformanceTrendQuery.
/// </summary>
public sealed class GetPerformanceTrendQueryHandler : IRequestHandler<GetPerformanceTrendQuery, Result<PerformanceTrend>>
{
    private readonly IAiBattleAnalysisService _analysisService;

    public GetPerformanceTrendQueryHandler(IAiBattleAnalysisService analysisService)
    {
        _analysisService = analysisService;
    }

    public async Task<Result<PerformanceTrend>> Handle(GetPerformanceTrendQuery request, CancellationToken cancellationToken)
    {
        return await _analysisService.GetPerformanceTrendAsync(
            request.CharacterName, request.Since, cancellationToken);
    }
}
