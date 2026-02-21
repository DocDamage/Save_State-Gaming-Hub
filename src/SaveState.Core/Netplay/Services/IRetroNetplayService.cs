using SaveState.Core.Common;
using SaveState.Core.Netplay.Models;

namespace SaveState.Core.Netplay.Services;

public interface IRetroNetplayService
{
    Task<Result<MatchmakingTicket>> StartMatchmakingAsync(
        MatchmakingRequest request,
        CancellationToken ct = default);

    Task<Result> CancelMatchmakingAsync(
        string ticketId,
        CancellationToken ct = default);

    Task<Result<NetplaySession>> ConnectToPeerAsync(
        MatchmakingTicket ticket,
        CancellationToken ct = default);

    Task<Result> VerifyRomHashAsync(
        string gameId,
        string romHash,
        CancellationToken ct = default);

    Task<Result> StartSpectatorModeAsync(
        string sessionId,
        CancellationToken ct = default);

    Task<Result> DisconnectAsync(
        string sessionId,
        CancellationToken ct = default);

    Task<Result<IReadOnlyList<NetplaySession>>> GetActiveSessionsAsync(
        CancellationToken ct = default);

    Task<Result<IReadOnlyList<LeaderboardEntry>>> GetLeaderboardAsync(
        string gameId,
        CancellationToken ct = default);
}
