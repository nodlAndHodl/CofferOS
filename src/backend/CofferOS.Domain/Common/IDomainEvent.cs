namespace CofferOS.Domain.Common;

/// <summary>
/// Marker interface for domain events. Domain events describe something meaningful
/// that has happened inside the domain (e.g. a wallet was imported, a new block was
/// detected). They are raised by aggregates and dispatched by the application layer,
/// which keeps modules decoupled.
/// </summary>
public interface IDomainEvent
{
    DateTimeOffset OccurredOn { get; }
}
