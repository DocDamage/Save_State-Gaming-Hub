using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Mugen.ReplayAnalysis;
using SaveState.Core.Mugen.ReplayAnalysis.Services;

namespace SaveState.Application.Mugen.ReplayAnalysis.Queries;

/// <summary>
/// Query to get combo statistics for a character across multiple replays.
/// </summary>
public sealed record GetComboStatisticsQuery(
    string Character,
    int? MinReplays = null) : IRequest<Result<ComboStatistics>>;

/// <summary>
/// Handler for GetComboStatisticsQuery.
/// </summary>
public sealed class GetComboStatisticsQueryHandler : IRequestHandler<GetComboStatisticsQuery, Result<ComboStatistics>>
{
    private readonly IReplayAnalysisService _analysisService;

    public GetComboStatisticsQueryHandler(IReplayAnalysisService analysisService)
    {
        _analysisService = analysisService;
    }

    public async Task<Result<ComboStatistics>> Handle(GetComboStatisticsQuery request, CancellationToken cancellationToken)
    {
        return await _analysisService.GetComboStatisticsAsync(
            request.Character, 
            request.MinReplays, 
            cancellationToken);
    }
}
