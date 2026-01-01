namespace SaveState.Application.Sync.Commands;

using MediatR;
using SaveState.Core.Common;

/// <summary>
/// Command to get current network quality.
/// </summary>
public record GetNetworkQualityCommand() : IRequest<Result<NetworkQualityInfo>>;

/// <summary>
/// Network quality information.
/// </summary>
public record NetworkQualityInfo(
    int LatencyMs,
    int PacketLossPercent,
    int BandwidthMbps,
    string QualityLevel);