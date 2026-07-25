namespace CofferOS.Application.Abstractions.Providers;

/// <summary>
/// Abstraction for obtaining the current Bitcoin price in fiat (e.g., USD).
/// Implementations are pluggable: Manual, Mempool, CoinGecko, Kraken, Coinbase, etc.
/// The rest of the application depends only on this interface.
/// </summary>
public interface IBitcoinPriceProvider
{
    /// <summary>Stable identifier for the provider, e.g. "manual", "mempool", "coingecko".</summary>
    string ProviderId { get; }

    /// <summary>Human friendly name, e.g. "Manual Entry", "Mempool.space".</summary>
    string DisplayName { get; }

    /// <summary>
    /// Attempts to get the current BTC price. Returns null if unavailable or not configured.
    /// Price is expected in the provider's native fiat currency (typically USD).
    /// </summary>
    Task<decimal?> GetCurrentPriceAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// The UTC time the current price value was last successfully set or fetched.
    /// Null if no price has ever been available.
    /// </summary>
    DateTimeOffset? LastUpdated { get; }
}

/// <summary>
/// A mutable price source for manual entry scenarios.
/// Allows the UI or other callers to set a price that subsequent reads will return.
/// Registered as a singleton so the value persists across requests within a process.
/// </summary>
public interface IMutableBitcoinPriceSource : IBitcoinPriceProvider
{
    /// <summary>Sets the price that will be returned by GetCurrentPriceAsync.</summary>
    void SetPrice(decimal price);
}
