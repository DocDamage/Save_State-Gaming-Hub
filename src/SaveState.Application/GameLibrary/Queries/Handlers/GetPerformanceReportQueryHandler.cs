namespace SaveState.Application.GameLibrary.Queries.Handlers;

using MediatR;
using SaveState.Core.Common;
using SaveState.Core.GameLibrary.Services;

public class GetPerformanceReportQueryHandler : IRequestHandler<GetPerformanceReportQuery, Result<PerformanceReport>>
{
    private readonly IPerformanceProfiler _performanceProfiler;

    public GetPerformanceReportQueryHandler(IPerformanceProfiler performanceProfiler)
    {
        _performanceProfiler = performanceProfiler;
    }

    public async Task<Result<PerformanceReport>> Handle(GetPerformanceReportQuery request, CancellationToken ct)
    {
        return await _performanceProfiler.GenerateReportAsync(ct);
    }
}