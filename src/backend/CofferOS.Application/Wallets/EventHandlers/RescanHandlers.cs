using CofferOS.Application.Abstractions.Events;
using CofferOS.Application.Abstractions.Persistence;
using CofferOS.Domain.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CofferOS.Application.Wallets.EventHandlers;

/// <summary>Triggers a rescan after a wallet is imported so the UI shows live data immediately.</summary>
public sealed class RescanOnWalletImportedHandler : IDomainEventHandler<WalletImportedEvent>
{
    private readonly WalletRescanService _rescan;
    private readonly ILogger<RescanOnWalletImportedHandler> _logger;

    public RescanOnWalletImportedHandler(WalletRescanService rescan, ILogger<RescanOnWalletImportedHandler> logger)
    {
        _rescan = rescan;
        _logger = logger;
    }

    public async Task HandleAsync(WalletImportedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Wallet {WalletId} imported; starting initial rescan", domainEvent.WalletId);
        await _rescan.RescanAsync(domainEvent.WalletId, cancellationToken);
    }
}

/// <summary>Rescans every wallet when a new block is detected on the node.</summary>
public sealed class RescanOnNewBlockHandler : IDomainEventHandler<NewBlockDetectedEvent>
{
    private readonly IWalletRepository _wallets;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RescanOnNewBlockHandler> _logger;

    public RescanOnNewBlockHandler(IWalletRepository wallets, IServiceScopeFactory scopeFactory, ILogger<RescanOnNewBlockHandler> logger)
    {
        _wallets = wallets;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task HandleAsync(NewBlockDetectedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        var walletList = await _wallets.GetAllAsync(cancellationToken);
        if (walletList.Count == 0) return;

        _logger.LogInformation("New block {Height} detected; rescanning {WalletCount} wallets", domainEvent.Height, walletList.Count);

        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = 3,
            CancellationToken = cancellationToken
        };

        await Parallel.ForEachAsync(walletList, options, async (wallet, ct) =>
            await RescanWalletAsync(wallet.Id, ct));
    }

    private async Task RescanWalletAsync(Guid walletId, CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var rescan = scope.ServiceProvider.GetRequiredService<WalletRescanService>();

        try
        {
            await rescan.RescanAsync(walletId, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Rescan failed for wallet {WalletId}", walletId);
        }
    }
}
