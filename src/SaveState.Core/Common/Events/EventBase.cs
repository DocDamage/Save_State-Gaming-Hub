namespace SaveState.Core.Common.Events;

/// <summary>
/// Base class for all domain events. Provides common properties and behavior.
/// </summary>
public abstract class EventBase : IEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
