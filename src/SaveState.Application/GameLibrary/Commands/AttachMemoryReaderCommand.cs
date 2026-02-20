using MediatR;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.GameLibrary.Services;
using SaveState.Application.Common;

namespace SaveState.Application.GameLibrary.Commands;

public class AttachMemoryReaderCommand : IRequest<Result>
{
    public int ProcessId { get; init; }
    public Guid GameId { get; init; }
    public string GameName { get; init; } = string.Empty;
}

public class AttachMemoryReaderCommandHandler : IRequestHandler<AttachMemoryReaderCommand, Result>
{
    private readonly IGameMemoryReader _memoryReader;
    private readonly ILogger<AttachMemoryReaderCommandHandler> _logger;

    public AttachMemoryReaderCommandHandler(
        IGameMemoryReader memoryReader,
        ILogger<AttachMemoryReaderCommandHandler> logger)
    {
        _memoryReader = memoryReader;
        _logger = logger;
    }

    public async Task<Result> Handle(AttachMemoryReaderCommand request, CancellationToken cancellationToken)
    {
        using (_logger.BeginCorrelationScope())
        using (_logger.BeginGameScope(request.GameId, request.GameName))
        {
            _logger.LogInformation(
                "Processing attach command for game {GameId} ({GameName}), process {ProcessId}",
                request.GameId,
                request.GameName,
                request.ProcessId);
                
            try
            {
                var result = await _memoryReader.AttachToProcessAsync(request.ProcessId, cancellationToken);
                
                if (result.IsSuccess)
                {
                    _logger.LogInformation(
                        "Successfully attached to process {ProcessId} for game {GameId}",
                        request.ProcessId,
                        request.GameId);
                }
                else
                {
                    _logger.LogWarning(
                        "Failed to attach to process {ProcessId} for game {GameId}: {Error}",
                        request.ProcessId,
                        request.GameId,
                        result.Error);
                }
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Attach command failed for game {GameId}", request.GameId);
                throw;
            }
        }
    }
}
