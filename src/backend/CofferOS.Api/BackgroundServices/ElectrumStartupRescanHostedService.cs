using CofferOS.Application.Abstractions.Persistence;
using CofferOS.Application.Abstractions.Providers;
using CofferOS.Application.Wallets;
using Microsoft.Extensions.DependencyInjection;

namespace CofferOS.Api.BackgroundServices;

/// <summary>
/// Performs a one-time rescan of all wallets on application startup.
/// This ensures wallet data is current after a restart before waiting for the next block.
/// </summary>
public sealed class ElectrumStartupRescanHostedService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<ElectrumStartupRescanHostedService> _logger;

    public ElectrumStartupRescanHostedService(IServiceProvider services, ILogger<ElectrumStartupRescanHostedService> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Give the rest of the app time to finish startup before scanning.
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        await using var scope = _services.CreateAsyncScope();
        var utxoProvider = scope.ServiceProvider.GetService<IUtxoProvider>();
        if (utxoProvider is null)
        {
            _logger.LogInformation("No UTXO provider is enabled; skipping startup wallet rescan.");
            return;
        }

        var wallets = await scope.ServiceProvider.GetRequiredService<IWalletRepository>().GetAllAsync(stoppingToken);
        if (wallets.Count == 0)
        {
            _logger.LogInformation("No wallets found; skipping startup rescan.");
            return;
        }

        _logger.LogInformation("Starting startup rescan for {WalletCount} wallets", wallets.Count);

        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = 3,
            CancellationToken = stoppingToken
        };

        await Parallel.ForEachAsync(wallets, options, async (wallet, ct) =>
        {
            await using var rescanScope = _services.CreateAsyncScope();
            var rescan = rescanScope.ServiceProvider.GetRequiredService<WalletRescanService>();

            try
            {
                _logger.LogInformation("Startup rescan for wallet {WalletId} starting", wallet.Id);
                await rescan.RescanAsync(wallet.Id, ct);
                _logger.LogInformation("Startup rescan for wallet {WalletId} completed", wallet.Id);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Startup rescan failed for wallet {WalletId}", wallet.Id);
            }
        });

        _logger.LogInformation("Startup wallet rescan complete for {WalletCount} wallets", wallets.Count);
    }
}
