namespace SaveState.Core.Common.Events;

using SaveState.Core.Common.Services;

/// <summary>
/// Base class for all domain events. Provides common properties and behavior.
/// </summary>
public abstract class EventBase : IEvent
{
    public Guid EventId { get; }
    public DateTime OccurredOn { get; }

    protected EventBase(ITimeProvider timeProvider)
    {
        Guard.Against.Null(timeProvider, nameof(timeProvider));
        EventId = Guid.NewGuid();
        OccurredOn = timeProvider.UtcNow;
    }

    protected EventBase(DateTime occurredOn)
    {
        EventId = Guid.NewGuid();
        OccurredOn = occurredOn;
    }

    [Obsolete("Use constructor with ITimeProvider or DateTime parameter")]
    protected EventBase()
    {
        EventId = Guid.NewGuid();
        OccurredOn = SystemTimeProvider.Instance.UtcNow;
    }
}
