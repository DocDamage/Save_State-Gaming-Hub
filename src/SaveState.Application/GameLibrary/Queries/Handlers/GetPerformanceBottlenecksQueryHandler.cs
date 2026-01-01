namespace SaveState.Application.GameLibrary.Queries.Handlers;

using MediatR;
using SaveState.Core.Common;
using SaveState.Core.GameLibrary.Services;

public class GetPerformanceBottlenecksQueryHandler : IRequestHandler<GetPerformanceBottlenecksQuery, Result<IReadOnlyList<BottleneckAnalysis>>>
{
    private readonly IPerformanceProfiler _performanceProfiler;

    public GetPerformanceBottlenecksQueryHandler(IPerformanceProfiler performanceProfiler)
    {
        _performanceProfiler = performanceProfiler;
    }

    public async Task<Result<IReadOnlyList<BottleneckAnalysis>>> Handle(GetPerformanceBottlenecksQuery request, CancellationToken ct)
    {
        return await _performanceProfiler.AnalyzeBottlenecksAsync(ct);
    }
}