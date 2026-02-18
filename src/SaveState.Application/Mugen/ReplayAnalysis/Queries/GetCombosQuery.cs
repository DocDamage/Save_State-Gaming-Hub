using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Mugen.ReplayAnalysis;
using SaveState.Core.Mugen.ReplayAnalysis.Services;

namespace SaveState.Application.Mugen.ReplayAnalysis.Queries;

/// <summary>
/// Query to get all combos from a replay analysis.
/// </summary>
public sealed record GetCombosQuery(
    Guid AnalysisId,
    int? Player = null,
    int? MinHits = null) : IRequest<Result<List<DetectedCombo>>>;

/// <summary>
/// Handler for GetCombosQuery.
/// </summary>
public sealed class GetCombosQueryHandler : IRequestHandler<GetCombosQuery, Result<List<DetectedCombo>>>
{
    private readonly IReplayAnalysisService _analysisService;

    public GetCombosQueryHandler(IReplayAnalysisService analysisService)
    {
        _analysisService = analysisService;
    }

    public async Task<Result<List<DetectedCombo>>> Handle(GetCombosQuery request, CancellationToken cancellationToken)
    {
        return await _analysisService.GetCombosAsync(
            request.AnalysisId, 
            request.Player, 
            request.MinHits, 
            cancellationToken);
    }
}
