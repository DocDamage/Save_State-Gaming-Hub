namespace SaveState.Application.Sync.Commands.Handlers;

using MediatR;
using SaveState.Core.Common;
using SaveState.Application.Sync.Commands;

/// <summary>
/// Handler for retrieving current network quality metrics.
/// Provides real-time information about connection performance.
/// </summary>
public class GetNetworkQualityCommandHandler : IRequestHandler<GetNetworkQualityCommand, Result<NetworkQualityInfo>>
{
    /// <summary>
    /// Handles the command to get current network quality information.
    /// </summary>
    /// <param name="request">The get network quality command.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A result containing the network quality information or an error.</returns>
    public Task<Result<NetworkQualityInfo>> Handle(GetNetworkQualityCommand request, CancellationToken ct)
    {
        // Return stub network quality info
        var quality = new NetworkQualityInfo(
            LatencyMs: 25,
            PacketLossPercent: 0,
            BandwidthMbps: 100,
            QualityLevel: "Excellent");

        return Task.FromResult(Result.Success<NetworkQualityInfo>(quality));
    }
}

