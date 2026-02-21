using SaveState.Core.Common;

namespace SaveState.Core.Netplay.Services;

public interface ISpectatorRelayService
{
    Task<Result> StartRelayAsync(string sessionId, CancellationToken ct = default);
    Task<Result> StopRelayAsync(string sessionId, CancellationToken ct = default);
    Task<Result> AddSpectatorAsync(string sessionId, string spectatorId, CancellationToken ct = default);
    Task<Result> RemoveSpectatorAsync(string sessionId, string spectatorId, CancellationToken ct = default);
    Task<Result<int>> GetSpectatorCountAsync(string sessionId, CancellationToken ct = default);
    Task<Result> BroadcastFrameAsync(string sessionId, byte[] frameData, CancellationToken ct = default);
}
