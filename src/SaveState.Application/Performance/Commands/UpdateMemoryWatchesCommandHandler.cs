using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Performance.Services;

namespace SaveState.Application.Performance.Commands;

public class UpdateMemoryWatchesCommandHandler : IRequestHandler<UpdateMemoryWatchesCommand, Result<int>>
{
    private readonly IMemoryWatchService _watchService;

    public UpdateMemoryWatchesCommandHandler(IMemoryWatchService watchService)
    {
        _watchService = watchService;
    }

    public async Task<Result<int>> Handle(UpdateMemoryWatchesCommand request, CancellationToken ct)
    {
        return await _watchService.UpdateAllWatchesAsync(request.GameId, request.ProcessId, ct);
    }
}
