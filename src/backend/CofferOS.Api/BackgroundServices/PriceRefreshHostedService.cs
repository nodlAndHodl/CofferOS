using CofferOS.Application.Prices;
using Microsoft.Extensions.Options;

namespace CofferOS.Api.BackgroundServices;

/// <summary>
/// Background worker that periodically refreshes the Bitcoin price using the configured provider.
/// Respects Enabled and PrivacyMode settings.
/// </summary>
public sealed class PriceRefreshHostedService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<PriceRefreshHostedService> _logger;

    public PriceRefreshHostedService(IServiceProvider services, ILogger<PriceRefreshHostedService> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Small startup delay
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = _services.CreateAsyncScope();
                var opts = scope.ServiceProvider.GetRequiredService<IOptionsMonitor<BitcoinPriceOptions>>().CurrentValue;

                if (!opts.Enabled)
                {
                    _logger.LogDebug("Bitcoin price auto-refresh is disabled.");
                }
                else if (opts.PrivacyMode)
                {
                    _logger.LogDebug("Privacy Mode enabled; skipping price refresh.");
                }
                else
                {
                    var priceService = scope.ServiceProvider.GetRequiredService<BitcoinPriceService>();
                    var result = await priceService.RefreshAsync(stoppingToken);

                    if (!result.Success)
                    {
                        _logger.LogInformation("Price refresh did not update: {Message}", result.Message);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during price refresh cycle");
            }

            // Determine next interval
            int intervalSeconds = 300;
            try
            {
                await using var scope2 = _services.CreateAsyncScope();
                var opts2 = scope2.ServiceProvider.GetRequiredService<IOptionsMonitor<BitcoinPriceOptions>>().CurrentValue;
                if (opts2.PollIntervalSeconds > 0)
                    intervalSeconds = opts2.PollIntervalSeconds;
            }
            catch { /* ignore */ }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
        }
    }
}
