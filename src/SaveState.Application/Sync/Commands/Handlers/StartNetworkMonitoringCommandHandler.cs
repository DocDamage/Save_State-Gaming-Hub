using MediatR;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Sync.Services;

namespace SaveState.Application.Sync.Commands.Handlers;

/// <summary>
/// Handler for starting network quality monitoring.
/// </summary>
public class StartNetworkMonitoringCommandHandler : IRequestHandler<StartNetworkMonitoringCommand, Result>
{
    private readonly INetworkQualityMonitor _networkMonitor;
    private readonly ILogger<StartNetworkMonitoringCommandHandler> _logger;

    public StartNetworkMonitoringCommandHandler(
        INetworkQualityMonitor networkMonitor,
        ILogger<StartNetworkMonitoringCommandHandler> logger)
    {
        _networkMonitor = networkMonitor;
        _logger = logger;
    }

    public async Task<Result> Handle(StartNetworkMonitoringCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (_networkMonitor.IsMonitoring)
            {
                _logger.LogWarning("Network monitoring is already running");
                return Result.Success();
            }

            await _networkMonitor.StartMonitoringAsync(TimeSpan.FromMinutes(1), cancellationToken);
            _logger.LogInformation("Network quality monitoring started");

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start network monitoring");
            return Result.Failure("Failed to start network monitoring", ErrorType.Internal);
        }
    }
}
