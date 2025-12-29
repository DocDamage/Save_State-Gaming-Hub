namespace SaveState.Application.Common.Events;

using MediatR;
using Microsoft.Extensions.Logging;

/// <summary>
/// Implementation of IEventPublisher using MediatR for event publishing.
/// </summary>
public class EventPublisher : IEventPublisher
{
    private readonly IMediator _mediator;
    private readonly ILogger<EventPublisher> _logger;

    public EventPublisher(IMediator mediator, ILogger<EventPublisher> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken ct = default)
        where TEvent : SaveState.Core.Common.Events.IEvent
    {
        _logger.LogInformation("Publishing event {EventType} with ID {EventId}",
            @event.EventType, @event.EventId);

        await _mediator.Publish(@event, ct).ConfigureAwait(false);
    }

    public async Task PublishAsync(IEnumerable<SaveState.Core.Common.Events.IEvent> events, CancellationToken ct = default)
    {
        foreach (var @event in events)
        {
            await PublishAsync(@event, ct).ConfigureAwait(false);
        }
    }
}
