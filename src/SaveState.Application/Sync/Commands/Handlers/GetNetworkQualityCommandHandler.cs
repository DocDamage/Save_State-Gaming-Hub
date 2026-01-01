namespace SaveState.Application.Sync.Commands.Handlers;

using MediatR;
using SaveState.Core.Common;
using SaveState.Application.Sync.Commands;

/// <summary>
/// Handler for getting network quality.
/// </summary>
public class GetNetworkQualityCommandHandler : IRequestHandler<GetNetworkQualityCommand, Result<NetworkQualityInfo>>
{
    public Task<Result<NetworkQualityInfo>> Handle(GetNetworkQualityCommand request, CancellationToken ct)
    {
        // Return stub network quality info
        var quality = new NetworkQualityInfo(
            LatencyMs: 25,
            PacketLossPercent: 0,
            BandwidthMbps: 100,
            QualityLevel: "Excellent");

        return Task.FromResult(Result<NetworkQualityInfo>.Success(quality));
    }
}
