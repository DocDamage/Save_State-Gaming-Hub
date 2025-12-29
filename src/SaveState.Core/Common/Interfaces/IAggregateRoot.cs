namespace SaveState.Core.Common.Interfaces;

using SaveState.Core.Common.Events;

public interface IAggregateRoot
{
    IReadOnlyCollection<IEvent> DomainEvents { get; }
    void ClearDomainEvents();
}
