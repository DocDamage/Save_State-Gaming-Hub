using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Performance.Services;

namespace SaveState.Application.Performance.Commands;

public class ModifyMemoryWatchCommandHandler : IRequestHandler<ModifyMemoryWatchCommand, Result>
{
    private readonly IMemoryWatchService _watchService;

    public ModifyMemoryWatchCommandHandler(IMemoryWatchService watchService)
    {
        _watchService = watchService;
    }

    public async Task<Result> Handle(ModifyMemoryWatchCommand request, CancellationToken ct)
    {
        if (request.IsFrozen.HasValue)
        {
            // Simple toggle logic for now, or we could add SetFrozenAsync to service
            await _watchService.ToggleFreezeAsync(request.WatchId, ct);
        }

        // Additional modifications (Label, Description) could be added to service interface
        return Result.Success();
    }
}
