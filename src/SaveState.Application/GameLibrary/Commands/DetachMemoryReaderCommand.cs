using MediatR;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.GameLibrary.Services;
using SaveState.Application.Common;

namespace SaveState.Application.GameLibrary.Commands;

public class DetachMemoryReaderCommand : IRequest<Result>
{
    public Guid? GameId { get; init; }
    public string? GameName { get; init; }
}

public class DetachMemoryReaderCommandHandler : IRequestHandler<DetachMemoryReaderCommand, Result>
{
    private readonly IGameMemoryReader _memoryReader;
    private readonly ILogger<DetachMemoryReaderCommandHandler> _logger;

    public DetachMemoryReaderCommandHandler(
        IGameMemoryReader memoryReader,
        ILogger<DetachMemoryReaderCommandHandler> logger)
    {
        _memoryReader = memoryReader;
        _logger = logger;
    }

    public async Task<Result> Handle(DetachMemoryReaderCommand request, CancellationToken cancellationToken)
    {
        using (_logger.BeginCorrelationScope())
        {
            if (request.GameId.HasValue && !string.IsNullOrEmpty(request.GameName))
            {
                using (_logger.BeginGameScope(request.GameId.Value, request.GameName))
                {
                    _logger.LogInformation(
                        "Processing detach command for game {GameId} ({GameName})",
                        request.GameId,
                        request.GameName);
                }
            }
            else
            {
                _logger.LogInformation("Processing detach command");
            }
            
            try
            {
                var result = await _memoryReader.DetachAsync(cancellationToken);
                
                if (result.IsSuccess)
                {
                    _logger.LogInformation("Successfully detached memory reader");
                }
                else
                {
                    _logger.LogWarning("Failed to detach memory reader: {Error}", result.Error);
                }
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Detach command failed");
                throw;
            }
        }
    }
}
