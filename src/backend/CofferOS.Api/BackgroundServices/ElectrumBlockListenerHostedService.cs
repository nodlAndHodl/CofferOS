using CofferOS.Application.Abstractions.Events;
using CofferOS.Domain.Common;
using CofferOS.Domain.Events;
using CofferOS.Integrations.BitcoinCore;
using Microsoft.Extensions.Options;
using System.IO;

namespace CofferOS.Api.BackgroundServices;

/// <summary>
/// Opens a persistent Electrum connection and subscribes to <c>blockchain.headers.subscribe</c>.
/// Each incoming header notification is dispatched as a <see cref="NewBlockDetectedEvent"/>,
/// which the rescan handler turns into wallet rescans.
/// </summary>
public sealed class ElectrumBlockListenerHostedService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<ElectrumBlockListenerHostedService> _logger;

    public ElectrumBlockListenerHostedService(IServiceProvider services, ILogger<ElectrumBlockListenerHostedService> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Give the rest of the app time to finish startup before opening the long-lived connection.
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await using var scope = _services.CreateAsyncScope();
            var electrum = scope.ServiceProvider.GetService<ElectrumServerProvider>();
            if (electrum is null)
            {
                _logger.LogInformation("Electrum integration is not enabled; block listener will not start.");
                return;
            }

            var options = scope.ServiceProvider.GetRequiredService<IOptions<ElectrumOptions>>().Value;
            var network = MapNetwork(options.Network);

            try
            {
                await electrum.ListenForNewBlocksAsync(async (height, blockHash, ct) =>
                {
                    _logger.LogInformation("Dispatching new block event for height {Height}", height);

                    await using var dispatchScope = _services.CreateAsyncScope();
                    var dispatcher = dispatchScope.ServiceProvider.GetRequiredService<IDomainEventDispatcher>();
                    var evt = new NewBlockDetectedEvent(network, (int)height, blockHash);
                    await dispatcher.DispatchAsync(new[] { evt }, ct);
                }, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (IOException ex)
            {
                _logger.LogWarning("Electrum server closed the connection; reconnecting in 15s... ({Message})", ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Electrum block listener disconnected; reconnecting in 15s...");
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
        }
    }

    private static BitcoinNetwork MapNetwork(string? value)
    {
        if (Enum.TryParse<BitcoinNetwork>(value, ignoreCase: true, out var parsed))
            return parsed;

        return BitcoinNetwork.Mainnet;
    }
}
