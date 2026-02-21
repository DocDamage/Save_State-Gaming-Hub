using System.Collections.Concurrent;
using SaveState.Core.Netplay.Models;
using SaveState.Core.Netplay.Services;

namespace SaveState.Infrastructure.Netplay.Services;

public class InMemoryMatchmakingQueue : IMatchmakingQueue
{
    private readonly ConcurrentDictionary<string, MatchmakingTicket> _queue;
    private readonly ConcurrentQueue<string> _ticketOrder;

    public InMemoryMatchmakingQueue()
    {
        _queue = new ConcurrentDictionary<string, MatchmakingTicket>();
        _ticketOrder = new ConcurrentQueue<string>();
    }

    public Task EnqueueAsync(MatchmakingTicket ticket, CancellationToken ct = default)
    {
        _queue[ticket.TicketId] = ticket;
        _ticketOrder.Enqueue(ticket.TicketId);
        return Task.CompletedTask;
    }

    public Task<MatchmakingTicket?> DequeueAsync(CancellationToken ct = default)
    {
        if (_ticketOrder.TryDequeue(out var ticketId) && _queue.TryRemove(ticketId, out var ticket))
        {
            return Task.FromResult<MatchmakingTicket?>(ticket);
        }
        return Task.FromResult<MatchmakingTicket?>(null);
    }

    public Task<bool> RemoveAsync(string ticketId, CancellationToken ct = default)
    {
        return Task.FromResult(_queue.TryRemove(ticketId, out _));
    }

    public Task<IReadOnlyList<MatchmakingTicket>> GetQueuedTicketsAsync(CancellationToken ct = default)
    {
        return Task.FromResult<IReadOnlyList<MatchmakingTicket>>(_queue.Values.ToList());
    }

    public Task<MatchmakingTicket?> FindMatchAsync(MatchmakingTicket ticket, CancellationToken ct = default)
    {
        // Find a matching ticket based on game ID, region, and skill rating proximity
        var potentialMatch = _queue.Values.FirstOrDefault(t =>
            t.TicketId != ticket.TicketId &&
            t.Request.GameId == ticket.Request.GameId &&
            t.Request.Region == ticket.Request.Region &&
            Math.Abs((int)t.Request.Rating - (int)ticket.Request.Rating) <= 1 &&
            t.Status == MatchmakingStatus.Queued);

        return Task.FromResult(potentialMatch);
    }
}
