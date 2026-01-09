using MediatR;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Sync.Services;

namespace SaveState.Application.Sync.Commands.Handlers;

/// <summary>
/// Handler for stopping network quality monitoring.
/// </summary>
public class StopNetworkMonitoringCommandHandler : IRequestHandler<StopNetworkMonitoringCommand, Result>
{
    private readonly INetworkQualityMonitor _networkMonitor;
    private readonly ILogger<StopNetworkMonitoringCommandHandler> _logger;

    public StopNetworkMonitoringCommandHandler(
        INetworkQualityMonitor networkMonitor,
        ILogger<StopNetworkMonitoringCommandHandler> logger)
    {
        _networkMonitor = networkMonitor;
        _logger = logger;
    }

    public async Task<Result> Handle(StopNetworkMonitoringCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (!_networkMonitor.IsMonitoring)
            {
                _logger.LogWarning("Network monitoring is not running");
                return Result.Success();
            }

            await _networkMonitor.StopMonitoringAsync(cancellationToken);
            _logger.LogInformation("Network quality monitoring stopped");

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop network monitoring");
            return Result.Failure("Failed to stop network monitoring", ErrorType.Internal);
        }
    }
}
