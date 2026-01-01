namespace SaveState.Application.Sync.Commands.Handlers;

using MediatR;
using SaveState.Core.Common;
using SaveState.Application.Sync.Commands;

public class PerformNetworkQualityTestCommandHandler : IRequestHandler<PerformNetworkQualityTestCommand, Result<NetworkTestResult>>
{
    public Task<Result<NetworkTestResult>> Handle(PerformNetworkQualityTestCommand request, CancellationToken ct)
    {
        var result = new NetworkTestResult(
            AverageLatency: 28,
            MinLatency: 22,
            MaxLatency: 45,
            PacketLoss: 0,
            OverallQuality: "Excellent");

        return Task.FromResult(Result<NetworkTestResult>.Success(result));
    }
}
