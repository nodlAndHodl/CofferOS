using CofferOS.Domain.Common;

namespace CofferOS.Application.Abstractions.Events;

/// <summary>
/// Handles a specific type of domain event. Multiple handlers can subscribe to the
/// same event, which is how modules stay decoupled: the Wallets module raises an
/// event, and the Dashboard / Notifications modules react without any direct call.
/// </summary>
public interface IDomainEventHandler<in TEvent> where TEvent : IDomainEvent
{
    Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken = default);
}

/// <summary>
/// Collects domain events raised by aggregates and dispatches them to every
/// registered handler. Dispatched after a unit of work is committed.
/// </summary>
public interface IDomainEventDispatcher
{
    Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default);
}
