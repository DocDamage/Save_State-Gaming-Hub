using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Performance.Services;

namespace SaveState.Application.Performance.Commands;

public class WriteMemoryValueCommandHandler : IRequestHandler<WriteMemoryValueCommand, Result>
{
    private readonly IMemoryWatchService _watchService;

    public WriteMemoryValueCommandHandler(IMemoryWatchService watchService)
    {
        _watchService = watchService;
    }

    public async Task<Result> Handle(WriteMemoryValueCommand request, CancellationToken ct)
    {
        return await _watchService.WriteWatchValueAsync(request.WatchId, request.ProcessId, request.NewValue, ct);
    }
}
