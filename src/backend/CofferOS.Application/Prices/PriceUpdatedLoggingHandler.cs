using CofferOS.Application.Abstractions.Events;
using CofferOS.Domain.Events;
using Microsoft.Extensions.Logging;

namespace CofferOS.Application.Prices;

/// <summary>
/// Simple logging handler for PriceUpdatedEvent. Demonstrates decoupled consumption of price changes.
/// </summary>
public sealed class PriceUpdatedLoggingHandler : IDomainEventHandler<PriceUpdatedEvent>
{
    private readonly ILogger<PriceUpdatedLoggingHandler> _logger;

    public PriceUpdatedLoggingHandler(ILogger<PriceUpdatedLoggingHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(PriceUpdatedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Bitcoin price updated: {Price} USD via {Provider} at {OccurredOn}",
            domainEvent.PriceUsd, domainEvent.Provider, domainEvent.OccurredOn);
        return Task.CompletedTask;
    }
}
