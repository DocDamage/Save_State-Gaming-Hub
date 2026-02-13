using MediatR;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Performance.Entities;
using SaveState.Core.Performance.Services;
using SaveState.Core.Performance.ValueObjects;

namespace SaveState.Application.Performance.Commands;

/// <summary>
/// Handler for adding a new memory watch.
/// </summary>
public sealed class AddMemoryWatchCommandHandler : IRequestHandler<AddMemoryWatchCommand, Result<MemoryWatch>>
{
    private readonly IMemoryWatchService _watchService;
    private readonly ILogger<AddMemoryWatchCommandHandler> _logger;

    public AddMemoryWatchCommandHandler(
        IMemoryWatchService watchService,
        ILogger<AddMemoryWatchCommandHandler> logger)
    {
        _watchService = watchService;
        _logger = logger;
    }

    public async Task<Result<MemoryWatch>> Handle(AddMemoryWatchCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating memory watch '{Label}' for game {GameId}", request.Label, request.GameId);

        var address = request.Offsets != null && request.Offsets.Length > 0
            ? MemoryAddress.CreatePointerChain(request.Address, request.Offsets)
            : MemoryAddress.Create(request.Address);

        return await _watchService.CreateWatchAsync(
            request.GameId,
            request.Label,
            address,
            request.DataType,
            request.Description,
            cancellationToken);
    }
}
