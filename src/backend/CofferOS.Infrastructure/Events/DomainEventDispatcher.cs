using CofferOS.Application.Abstractions.Events;
using CofferOS.Domain.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CofferOS.Infrastructure.Events;

/// <summary>
/// Resolves and invokes all <see cref="IDomainEventHandler{TEvent}"/> registered for
/// each event's concrete type. Handlers are resolved from the DI container so new
/// reactions can be added by simply registering a handler.
/// </summary>
public sealed class DomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IServiceProvider _services;
    private readonly ILogger<DomainEventDispatcher> _logger;

    public DomainEventDispatcher(IServiceProvider services, ILogger<DomainEventDispatcher> logger)
    {
        _services = services;
        _logger = logger;
    }

    public async Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default)
    {
        foreach (var domainEvent in domainEvents)
        {
            var eventType = domainEvent.GetType();
            var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(eventType);
            var handlers = _services.GetServices(handlerType);

            foreach (var handler in handlers)
            {
                if (handler is null) continue;
                try
                {
                    var method = handlerType.GetMethod(nameof(IDomainEventHandler<IDomainEvent>.HandleAsync))!;
                    await (Task)method.Invoke(handler, new object[] { domainEvent, cancellationToken })!;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Handler {Handler} failed for event {Event}", handler.GetType().Name, eventType.Name);
                }
            }
        }
    }
}
