namespace SaveState.Core.Common.Events;

using MediatR;

/// <summary>
/// Marker interface for domain events.
/// All domain events inherit from this interface.
/// </summary>
public interface IEvent : INotification
{
    Guid EventId { get; }
    DateTime OccurredOn { get; }
    string EventType => GetType().Name;
}
