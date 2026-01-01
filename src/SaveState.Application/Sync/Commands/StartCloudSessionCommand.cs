using MediatR;
using SaveState.Core.Common;
using SaveState.Core.Sync.Services.DTOs;

namespace SaveState.Application.Sync.Commands;

/// <summary>
/// Command to start a cloud gaming session.
/// </summary>
public record StartCloudSessionCommand(Guid GameId, CloudGamingProvider Provider) : IRequest<Result<CloudSession>>;

/// <summary>
/// Command to end a cloud gaming session.
/// </summary>
public record EndCloudSessionCommand(Guid SessionId) : IRequest<Result>;

/// <summary>
/// Command to start network monitoring.
/// </summary>
public record StartNetworkMonitoringCommand : IRequest<Result>
{
    public TimeSpan Interval { get; init; } = TimeSpan.FromMinutes(1);
}

/// <summary>
/// Command to stop network monitoring.
/// </summary>
public record StopNetworkMonitoringCommand : IRequest<Result>;

/// <summary>
/// Command to optimize network settings for a cloud gaming provider.
/// </summary>
public record OptimizeNetworkForProviderCommand(CloudGamingProvider Provider) : IRequest<Result>;

/// <summary>
/// Command to check if a game is available on a cloud gaming provider.
/// </summary>
public record CheckGameAvailabilityCommand(Guid GameId, CloudGamingProvider Provider) : IRequest<Result<bool>>;

/// <summary>
/// Command to get network recommendations for a cloud gaming provider.
/// </summary>
public record GetNetworkRecommendationsCommand(CloudGamingProvider Provider) : IRequest<Result<IReadOnlyList<string>>>;

/// <summary>
/// Command to get network diagnostics.
/// </summary>
public record GetNetworkDiagnosticsCommand : IRequest<Result<NetworkDiagnostics>>;

/// <summary>
/// Command to check if network quality is sufficient for cloud gaming.
/// </summary>
public record CheckNetworkSufficiencyCommand : IRequest<Result<bool>>
{
    public CloudGamingProvider Provider { get; init; }
}
