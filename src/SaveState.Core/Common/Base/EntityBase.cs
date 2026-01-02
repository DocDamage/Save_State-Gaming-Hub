namespace SaveState.Core.Common.Base;

using SaveState.Core.Common.Events;
using SaveState.Core.Common.Interfaces;

/// <summary>
/// Base class for all domain entities. Provides identity comparison and domain event support.
/// Entities are compared by their <see cref="Id"/> rather than their properties.
/// </summary>
public abstract class EntityBase : IEntity, IAggregateRoot
{
    /// <summary>
    /// Gets the unique identifier for this entity.
    /// </summary>
    public virtual Guid Id { get; protected set; } = Guid.NewGuid();

    private readonly List<IEvent> _domainEvents = new();

    /// <summary>
    /// Gets the collection of domain events raised by this entity.
    /// </summary>
    public IReadOnlyCollection<IEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>
    /// Adds a domain event to be dispatched after the entity is persisted.
    /// </summary>
    /// <param name="domainEvent">The domain event to add.</param>
    protected void AddDomainEvent(IEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    /// <summary>
    /// Clears all domain events. Call after events have been dispatched.
    /// </summary>
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        if (obj is not EntityBase other) return false;
        if (ReferenceEquals(this, other)) return true;
        if (GetType() != other.GetType()) return false;
        return Id.Equals(other.Id);
    }

    /// <inheritdoc/>
    public override int GetHashCode() => Id.GetHashCode();
}

