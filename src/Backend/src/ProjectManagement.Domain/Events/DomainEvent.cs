namespace ProjectManagement.Domain.Events;

/// <summary>
/// Base class for domain events with common properties.
/// </summary>
public abstract class DomainEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
