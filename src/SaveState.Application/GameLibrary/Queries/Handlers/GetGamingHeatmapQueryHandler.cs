using MediatR;
using SaveState.Application.Common;
using SaveState.Core.Analytics.DTOs;
using SaveState.Core.Analytics.Services;

namespace SaveState.Application.GameLibrary.Queries.Handlers;

public class GetGamingHeatmapQueryHandler : IRequestHandler<GetGamingHeatmapQuery, Result<GamingHeatmapData>>
{
    private readonly IAnalyticsService _analyticsService;

    public GetGamingHeatmapQueryHandler(IAnalyticsService analyticsService)
    {
        _analyticsService = analyticsService;
    }

    public async Task<Result<GamingHeatmapData>> Handle(GetGamingHeatmapQuery request, CancellationToken ct)
    {
        return await _analyticsService.GetHeatmapAsync(request.Year, ct).ConfigureAwait(false);
    }
}