namespace SaveState.Application.Common.Events;

/// <summary>
/// Service for publishing domain events. Abstracts the event publishing mechanism.
/// </summary>
public interface IEventPublisher
{
    /// <summary>
    /// Publishes a single domain event asynchronously.
    /// </summary>
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken ct = default)
        where TEvent : SaveState.Core.Common.Events.IEvent;

    /// <summary>
    /// Publishes multiple domain events asynchronously.
    /// </summary>
    Task PublishAsync(IEnumerable<SaveState.Core.Common.Events.IEvent> events, CancellationToken ct = default);
}
