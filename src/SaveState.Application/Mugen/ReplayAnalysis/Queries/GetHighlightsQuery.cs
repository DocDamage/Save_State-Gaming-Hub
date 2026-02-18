using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Mugen.ReplayAnalysis;
using SaveState.Core.Mugen.ReplayAnalysis.Services;

namespace SaveState.Application.Mugen.ReplayAnalysis.Queries;

/// <summary>
/// Query to get highlight moments from a replay.
/// </summary>
public sealed record GetHighlightsQuery(
    Guid AnalysisId,
    HighlightType? Type = null,
    int? MinIntensity = null) : IRequest<Result<List<HighlightMoment>>>;

/// <summary>
/// Handler for GetHighlightsQuery.
/// </summary>
public sealed class GetHighlightsQueryHandler : IRequestHandler<GetHighlightsQuery, Result<List<HighlightMoment>>>
{
    private readonly IReplayAnalysisService _analysisService;

    public GetHighlightsQueryHandler(IReplayAnalysisService analysisService)
    {
        _analysisService = analysisService;
    }

    public async Task<Result<List<HighlightMoment>>> Handle(GetHighlightsQuery request, CancellationToken cancellationToken)
    {
        return await _analysisService.GetHighlightsAsync(
            request.AnalysisId, 
            request.Type, 
            request.MinIntensity, 
            cancellationToken);
    }
}
