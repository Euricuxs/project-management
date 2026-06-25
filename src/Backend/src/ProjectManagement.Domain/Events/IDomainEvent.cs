namespace ProjectManagement.Domain.Events;

/// <summary>
/// Interface for domain events.
/// Domain events are used to communicate state changes within the domain.
/// </summary>
public interface IDomainEvent
{
    Guid EventId { get; }
    DateTime OccurredAt { get; }
}
