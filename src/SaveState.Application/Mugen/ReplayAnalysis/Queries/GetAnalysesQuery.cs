using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Mugen.ReplayAnalysis;
using SaveState.Core.Mugen.ReplayAnalysis.Services;

namespace SaveState.Application.Mugen.ReplayAnalysis.Queries;

/// <summary>
/// Query to get all replay analyses with optional filtering.
/// </summary>
public sealed record GetAnalysesQuery(
    string? Character = null,
    string? PlayerName = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    int? MinComboHits = null,
    List<string>? Tags = null) : IRequest<Result<List<ReplayAnalysisSummary>>>;

/// <summary>
/// Handler for GetAnalysesQuery.
/// </summary>
public sealed class GetAnalysesQueryHandler : IRequestHandler<GetAnalysesQuery, Result<List<ReplayAnalysisSummary>>>
{
    private readonly IReplayAnalysisService _analysisService;

    public GetAnalysesQueryHandler(IReplayAnalysisService analysisService)
    {
        _analysisService = analysisService;
    }

    public async Task<Result<List<ReplayAnalysisSummary>>> Handle(GetAnalysesQuery request, CancellationToken cancellationToken)
    {
        var filter = new ReplayAnalysisFilter
        {
            Character = request.Character,
            PlayerName = request.PlayerName,
            FromDate = request.FromDate,
            ToDate = request.ToDate,
            MinComboHits = request.MinComboHits,
            Tags = request.Tags
        };

        return await _analysisService.GetAnalysesAsync(filter, cancellationToken);
    }
}
