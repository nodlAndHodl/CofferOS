using CofferOS.Application.Abstractions.Events;
using CofferOS.Application.Abstractions.Persistence;
using CofferOS.Application.Abstractions.Providers;
using CofferOS.Domain.Events;
using CofferOS.Domain.Prices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CofferOS.Application.Prices;

/// <summary>
/// Orchestrates Bitcoin price fetching, caching to history, and publishing events.
/// The application should call this (or the background worker) rather than providers directly.
/// </summary>
public sealed class BitcoinPriceService
{
    private readonly IBitcoinPriceProvider _currentProvider; // the one the app sees for "current"
    private readonly IMutableBitcoinPriceSource? _mutable;
    private readonly IBitcoinPriceHistoryRepository _history;
    private readonly IDomainEventDispatcher _dispatcher;
    private readonly IOptionsMonitor<BitcoinPriceOptions> _options;
    private readonly ILogger<BitcoinPriceService> _logger;

    public BitcoinPriceService(
        IBitcoinPriceProvider currentProvider,
        IMutableBitcoinPriceSource? mutable,
        IBitcoinPriceHistoryRepository history,
        IDomainEventDispatcher dispatcher,
        IOptionsMonitor<BitcoinPriceOptions> options,
        ILogger<BitcoinPriceService> logger)
    {
        _currentProvider = currentProvider;
        _mutable = mutable;
        _history = history;
        _dispatcher = dispatcher;
        _options = options;
        _logger = logger;
    }

    /// <summary>Returns the currently known price (from holder or last cached).</summary>
    public async Task<decimal?> GetCurrentPriceAsync(CancellationToken ct = default)
    {
        // The registered IBitcoinPriceProvider is the holder in our design.
        return await _currentProvider.GetCurrentPriceAsync(ct);
    }

    /// <summary>
    /// Performs one refresh cycle using the configured provider (unless PrivacyMode or disabled).
    /// On success: persist history, update mutable holder, publish PriceUpdatedEvent.
    /// </summary>
    public async Task<RefreshResult> RefreshAsync(CancellationToken ct = default)
    {
        var opts = _options.CurrentValue;

        if (!opts.Enabled)
        {
            return new RefreshResult(false, "Automatic updates are disabled.");
        }

        if (opts.PrivacyMode)
        {
            return new RefreshResult(false, "Privacy Mode is enabled; no outbound requests.");
        }

        var price = await _currentProvider.GetCurrentPriceAsync(ct);
        if (price is null || price <= 0)
        {
            return new RefreshResult(false, "Provider returned no usable price.");
        }

        var now = DateTimeOffset.UtcNow;

        // Persist history
        var entry = new BitcoinPriceHistory(now, price.Value, _currentProvider.ProviderId);
        await _history.AddAsync(entry, ct);

        // Push into the current holder so IBitcoinPriceProvider returns it
        if (_mutable is not null)
        {
            _mutable.SetPrice(price.Value);
        }
        else if (_currentProvider is IMutableBitcoinPriceSource m)
        {
            m.SetPrice(price.Value);
        }

        // Publish event
        await _dispatcher.DispatchAsync(new[] { new PriceUpdatedEvent(price.Value, _currentProvider.ProviderId, now) }, ct);

        _logger.LogInformation("Bitcoin price updated: {Price} via {Provider}", price, _currentProvider.ProviderId);

        return new RefreshResult(true, null, price.Value, _currentProvider.ProviderId, now);
    }

    public sealed record RefreshResult(bool Success, string? Message, decimal? Price = null, string? Provider = null, DateTimeOffset? Timestamp = null);
}
