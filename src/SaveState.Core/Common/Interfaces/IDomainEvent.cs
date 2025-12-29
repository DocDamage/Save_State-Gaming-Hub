namespace SaveState.Core.Common.Interfaces;

public interface IDomainEvent
{
    DateTime OccurredOn { get; }
    Guid EventId { get; }
}
