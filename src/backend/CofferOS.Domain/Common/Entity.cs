namespace CofferOS.Domain.Common;

/// <summary>
/// Base class for all entities. Provides identity and a domain-event buffer.
/// Aggregate roots record domain events which are collected and dispatched
/// after the unit of work commits.
/// </summary>
public abstract class Entity
{
    private readonly List<IDomainEvent> _domainEvents = new();

    public Guid Id { get; protected set; } = Guid.NewGuid();

    /// <summary>Domain events raised by this entity that have not yet been dispatched.</summary>
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void Raise(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();
}
