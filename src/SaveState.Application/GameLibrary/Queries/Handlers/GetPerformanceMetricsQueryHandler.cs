namespace SaveState.Application.GameLibrary.Queries.Handlers;

using MediatR;
using SaveState.Core.Common;
using SaveState.Core.GameLibrary.Services;

public class GetPerformanceMetricsQueryHandler : IRequestHandler<GetPerformanceMetricsQuery, Result<PerformanceMetrics>>
{
    private readonly IPerformanceProfiler _performanceProfiler;

    public GetPerformanceMetricsQueryHandler(IPerformanceProfiler performanceProfiler)
    {
        _performanceProfiler = performanceProfiler;
    }

    public async Task<Result<PerformanceMetrics>> Handle(GetPerformanceMetricsQuery request, CancellationToken ct)
    {
        return await _performanceProfiler.GetCurrentMetricsAsync(ct);
    }
}