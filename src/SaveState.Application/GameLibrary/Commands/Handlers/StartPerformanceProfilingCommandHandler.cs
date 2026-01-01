namespace SaveState.Application.GameLibrary.Commands.Handlers;

using MediatR;
using SaveState.Core.Common;
using SaveState.Core.GameLibrary.Services;

public class StartPerformanceProfilingCommandHandler : IRequestHandler<StartPerformanceProfilingCommand, Result>
{
    private readonly IPerformanceProfiler _performanceProfiler;

    public StartPerformanceProfilingCommandHandler(IPerformanceProfiler performanceProfiler)
    {
        _performanceProfiler = performanceProfiler;
    }

    public async Task<Result> Handle(StartPerformanceProfilingCommand request, CancellationToken ct)
    {
        return await _performanceProfiler.StartProfilingAsync(request.GameId, ct);
    }
}