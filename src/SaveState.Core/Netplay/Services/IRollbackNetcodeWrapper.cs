using SaveState.Core.Common;
using SaveState.Core.Netplay.Models;

namespace SaveState.Core.Netplay.Services;

public interface IRollbackNetcodeWrapper
{
    Task<Result> InitializeAsync(NetplaySession session, CancellationToken ct = default);
    Task<Result> StartSessionAsync(CancellationToken ct = default);
    Task<Result> StopSessionAsync(CancellationToken ct = default);
    Task<Result> UpdateInputAsync(int playerIndex, byte[] inputs, CancellationToken ct = default);
    Task<Result<byte[]>> GetConfirmedInputsAsync(int frame, CancellationToken ct = default);
    Task<Result> SetRollbackConfigAsync(RollbackConfig config, CancellationToken ct = default);
    Task<Result<int>> GetCurrentFrameAsync(CancellationToken ct = default);
    Task<Result<int>> GetPingAsync(CancellationToken ct = default);
}
