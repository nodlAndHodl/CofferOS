using CofferOS.Application.Abstractions.Events;
using CofferOS.Application.Abstractions.Notifications;
using CofferOS.Application.Abstractions.Providers;
using CofferOS.Application.Abstractions.Settings;
using CofferOS.Domain.Events;
using Microsoft.Extensions.Logging;

namespace CofferOS.Application.Prices;

/// <summary>
/// Broadcasts a bitcoin_price_updated WebSocket event to all connected clients
/// whenever the price is refreshed, respecting the user's live-update preference.
/// </summary>
public sealed class PriceUpdatedBroadcastHandler : IDomainEventHandler<PriceUpdatedEvent>
{
    private readonly INotificationService _notifications;
    private readonly IUserSettingsService _settings;
    private readonly IExchangeRateProvider _rates;
    private readonly ILogger<PriceUpdatedBroadcastHandler> _logger;

    public PriceUpdatedBroadcastHandler(
        INotificationService notifications,
        IUserSettingsService settings,
        IExchangeRateProvider rates,
        ILogger<PriceUpdatedBroadcastHandler> logger)
    {
        _notifications = notifications;
        _settings = settings;
        _rates = rates;
        _logger = logger;
    }

    public async Task HandleAsync(PriceUpdatedEvent evt, CancellationToken ct)
    {
        var userSettings = await _settings.GetAsync(ct);
        if (!userSettings.EnableLivePriceUpdates)
        {
            _logger.LogDebug("Live price updates disabled by user settings; skipping broadcast.");
            return;
        }

        var exchangeRates = _rates.GetCachedRates()
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        await _notifications.BroadcastAsync("bitcoin_price_updated", new
        {
            priceUsd = evt.PriceUsd,
            exchangeRates,
            provider = evt.Provider,
            timestamp = evt.OccurredOn,
        }, ct);

        _logger.LogDebug("Broadcast bitcoin_price_updated: {Price} USD", evt.PriceUsd);
    }
}
