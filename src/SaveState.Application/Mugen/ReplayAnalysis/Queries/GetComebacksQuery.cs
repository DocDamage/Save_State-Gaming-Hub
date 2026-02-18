using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Mugen.ReplayAnalysis;
using SaveState.Core.Mugen.ReplayAnalysis.Services;

namespace SaveState.Application.Mugen.ReplayAnalysis.Queries;

/// <summary>
/// Query to get comeback moments from a replay.
/// </summary>
public sealed record GetComebacksQuery(
    Guid AnalysisId,
    ComebackSeverity? MinSeverity = null) : IRequest<Result<List<ComebackMoment>>>;

/// <summary>
/// Handler for GetComebacksQuery.
/// </summary>
public sealed class GetComebacksQueryHandler : IRequestHandler<GetComebacksQuery, Result<List<ComebackMoment>>>
{
    private readonly IReplayAnalysisService _analysisService;

    public GetComebacksQueryHandler(IReplayAnalysisService analysisService)
    {
        _analysisService = analysisService;
    }

    public async Task<Result<List<ComebackMoment>>> Handle(GetComebacksQuery request, CancellationToken cancellationToken)
    {
        return await _analysisService.GetComebacksAsync(
            request.AnalysisId, 
            request.MinSeverity, 
            cancellationToken);
    }
}
