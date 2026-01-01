namespace SaveState.Application.Sync.Commands;

using MediatR;
using SaveState.Core.Common;

public record PerformNetworkQualityTestCommand() : IRequest<Result<NetworkTestResult>>;

public record NetworkTestResult(
    int AverageLatency,
    int MinLatency,
    int MaxLatency,
    int PacketLoss,
    string OverallQuality);