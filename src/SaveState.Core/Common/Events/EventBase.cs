namespace SaveState.Core.Common.Events;

/// <summary>
/// Base class for all domain events. Provides common properties and behavior.
/// </summary>
public abstract class EventBase : IEvent
{
    public Guid EventId { get; }
    public DateTime OccurredOn { get; }

    protected EventBase()
    {
        EventId = Guid.NewGuid();
        OccurredOn = DateTime.UtcNow;
    }

    protected EventBase(DateTime occurredOn)
    {
        EventId = Guid.NewGuid();
        OccurredOn = occurredOn;
    }
}
