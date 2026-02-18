using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Mugen.ReplayAnalysis;
using SaveState.Core.Mugen.ReplayAnalysis.Services;

namespace SaveState.Application.Mugen.ReplayAnalysis.Queries;

/// <summary>
/// Query to compare two replay analyses.
/// </summary>
public sealed record CompareReplaysQuery(
    Guid AnalysisId1,
    Guid AnalysisId2) : IRequest<Result<ReplayComparison>>;

/// <summary>
/// Handler for CompareReplaysQuery.
/// </summary>
public sealed class CompareReplaysQueryHandler : IRequestHandler<CompareReplaysQuery, Result<ReplayComparison>>
{
    private readonly IReplayAnalysisService _analysisService;

    public CompareReplaysQueryHandler(IReplayAnalysisService analysisService)
    {
        _analysisService = analysisService;
    }

    public async Task<Result<ReplayComparison>> Handle(CompareReplaysQuery request, CancellationToken cancellationToken)
    {
        return await _analysisService.CompareReplaysAsync(
            request.AnalysisId1, 
            request.AnalysisId2, 
            cancellationToken);
    }
}
