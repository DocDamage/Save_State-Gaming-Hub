using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Mugen.ReplayAnalysis;
using SaveState.Core.Mugen.ReplayAnalysis.Services;
using ReplayAnalysisModel = SaveState.Core.Mugen.ReplayAnalysis.ReplayAnalysis;

namespace SaveState.Application.Mugen.ReplayAnalysis.Queries;

/// <summary>
/// Query to get a replay analysis by ID.
/// </summary>
public sealed record GetAnalysisQuery(Guid AnalysisId) : IRequest<Result<ReplayAnalysisModel>>;

/// <summary>
/// Handler for GetAnalysisQuery.
/// </summary>
public sealed class GetAnalysisQueryHandler : IRequestHandler<GetAnalysisQuery, Result<ReplayAnalysisModel>>
{
    private readonly IReplayAnalysisService _analysisService;

    public GetAnalysisQueryHandler(IReplayAnalysisService analysisService)
    {
        _analysisService = analysisService;
    }

    public async Task<Result<ReplayAnalysisModel>> Handle(GetAnalysisQuery request, CancellationToken cancellationToken)
    {
        return await _analysisService.GetAnalysisAsync(request.AnalysisId, cancellationToken);
    }
}
