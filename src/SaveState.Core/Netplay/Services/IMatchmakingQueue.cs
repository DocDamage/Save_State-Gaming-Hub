using SaveState.Core.Netplay.Models;

namespace SaveState.Core.Netplay.Services;

public interface IMatchmakingQueue
{
    Task EnqueueAsync(MatchmakingTicket ticket, CancellationToken ct = default);
    Task<MatchmakingTicket?> DequeueAsync(CancellationToken ct = default);
    Task<bool> RemoveAsync(string ticketId, CancellationToken ct = default);
    Task<IReadOnlyList<MatchmakingTicket>> GetQueuedTicketsAsync(CancellationToken ct = default);
    Task<MatchmakingTicket?> FindMatchAsync(MatchmakingTicket ticket, CancellationToken ct = default);
}
