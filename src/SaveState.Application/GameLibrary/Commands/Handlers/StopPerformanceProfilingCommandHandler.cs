namespace SaveState.Application.GameLibrary.Commands.Handlers;

using MediatR;
using SaveState.Core.Common;
using SaveState.Core.GameLibrary.Services;

public class StopPerformanceProfilingCommandHandler : IRequestHandler<StopPerformanceProfilingCommand, Result>
{
    private readonly IPerformanceProfiler _performanceProfiler;

    public StopPerformanceProfilingCommandHandler(IPerformanceProfiler performanceProfiler)
    {
        _performanceProfiler = performanceProfiler;
    }

    public async Task<Result> Handle(StopPerformanceProfilingCommand request, CancellationToken ct)
    {
        return await _performanceProfiler.StopProfilingAsync(ct);
    }
}