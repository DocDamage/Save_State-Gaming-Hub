using MediatR;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Performance.Entities;
using SaveState.Core.Performance.Services;

namespace SaveState.Application.Performance.Queries;

/// <summary>
/// Handler for retrieving memory watches.
/// </summary>
public sealed class GetMemoryWatchesQueryHandler : IRequestHandler<GetMemoryWatchesQuery, Result<IReadOnlyList<MemoryWatch>>>
{
    private readonly IMemoryWatchService _watchService;
    private readonly ILogger<GetMemoryWatchesQueryHandler> _logger;

    public GetMemoryWatchesQueryHandler(
        IMemoryWatchService watchService,
        ILogger<GetMemoryWatchesQueryHandler> logger)
    {
        _watchService = watchService;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<MemoryWatch>>> Handle(GetMemoryWatchesQuery request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Retrieving memory watches for game {GameId}", request.GameId);
        return await _watchService.GetWatchesAsync(request.GameId, cancellationToken);
    }
}
