using CofferOS.Application.Abstractions.Events;
using CofferOS.Domain.Events;
using Microsoft.Extensions.Logging;

namespace CofferOS.Application.Wallets.EventHandlers;

/// <summary>
/// Example handlers that show how modules react to domain events without being
/// directly coupled. In a fuller build, the new-block handler would kick off a
/// wallet rescan; here it logs, demonstrating the wiring end to end.
/// </summary>
public sealed class WalletImportedLoggingHandler : IDomainEventHandler<WalletImportedEvent>
{
    private readonly ILogger<WalletImportedLoggingHandler> _logger;
    public WalletImportedLoggingHandler(ILogger<WalletImportedLoggingHandler> logger) => _logger = logger;

    public Task HandleAsync(WalletImportedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Domain event: wallet {WalletId} imported ({Name})", domainEvent.WalletId, domainEvent.Name);
        return Task.CompletedTask;
    }
}

public sealed class NewBlockLoggingHandler : IDomainEventHandler<NewBlockDetectedEvent>
{
    private readonly ILogger<NewBlockLoggingHandler> _logger;
    public NewBlockLoggingHandler(ILogger<NewBlockLoggingHandler> logger) => _logger = logger;

    public Task HandleAsync(NewBlockDetectedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        // Pipeline: New Block -> (future) Wallet Scanner -> Transaction Updated -> Dashboard Updated
        _logger.LogInformation("Domain event: new block {Height} on {Network} ({Hash})",
            domainEvent.Height, domainEvent.Network, domainEvent.BlockHash);
        return Task.CompletedTask;
    }
}
