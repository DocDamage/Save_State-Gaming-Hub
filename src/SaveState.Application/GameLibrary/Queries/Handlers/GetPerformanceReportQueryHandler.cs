namespace SaveState.Application.GameLibrary.Queries.Handlers;

using MediatR;
using SaveState.Core.Common;
using SaveState.Core.GameLibrary.Services;

/// <summary>
/// Handler for retrieving game performance analysis reports.
/// Provides detailed performance metrics and bottleneck identification.
/// </summary>
public class GetPerformanceReportQueryHandler : IRequestHandler<GetPerformanceReportQuery, Result<PerformanceReport>>
{
    private readonly IPerformanceProfiler _performanceProfiler;

    public GetPerformanceReportQueryHandler(IPerformanceProfiler performanceProfiler)
    {
        _performanceProfiler = performanceProfiler;
    }

    /// <summary>
    /// Handles the query to get a performance analysis report.
    /// </summary>
    /// <param name="request">The performance report query.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result containing the performance report or an error.</returns>
    public async Task<Result<PerformanceReport>> Handle(GetPerformanceReportQuery request, CancellationToken ct)
    {
        return await _performanceProfiler.GenerateReportAsync(ct);
    }
}