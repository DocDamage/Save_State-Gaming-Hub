namespace SaveState.Application.GameLibrary.Queries.Handlers;

using MediatR;
using SaveState.Core.Common;
using SaveState.Core.GameLibrary.Services;

/// <summary>
/// Handler for identifying performance bottlenecks.
/// Analyzes system resources and identifies performance limiting factors.
/// </summary>
public class GetPerformanceBottlenecksQueryHandler : IRequestHandler<GetPerformanceBottlenecksQuery, Result<IReadOnlyList<BottleneckAnalysis>>>
{
    private readonly IPerformanceProfiler _performanceProfiler;

    public GetPerformanceBottlenecksQueryHandler(IPerformanceProfiler performanceProfiler)
    {
        _performanceProfiler = performanceProfiler;
    }

    /// <summary>
    /// Handles the query to analyze performance bottlenecks.
    /// </summary>
    /// <param name="request">The performance bottlenecks query.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result containing the bottleneck analysis or an error.</returns>
    public async Task<Result<IReadOnlyList<BottleneckAnalysis>>> Handle(GetPerformanceBottlenecksQuery request, CancellationToken ct)
    {
        return await _performanceProfiler.AnalyzeBottlenecksAsync(ct);
    }
}