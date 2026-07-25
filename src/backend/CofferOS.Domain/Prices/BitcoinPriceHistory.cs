using CofferOS.Domain.Common;

namespace CofferOS.Domain.Prices;

/// <summary>
/// Historical snapshot of a Bitcoin price in USD fetched from a provider.
/// Used for caching, audit, and future charting.
/// </summary>
public sealed class BitcoinPriceHistory
{
    private BitcoinPriceHistory() { }

    public BitcoinPriceHistory(DateTimeOffset timestamp, decimal priceUsd, string provider)
    {
        if (priceUsd < 0)
            throw new ArgumentException("Price cannot be negative.", nameof(priceUsd));
        if (string.IsNullOrWhiteSpace(provider))
            throw new ArgumentException("Provider is required.", nameof(provider));

        Timestamp = timestamp;
        PriceUsd = priceUsd;
        Provider = provider.Trim();
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public DateTimeOffset Timestamp { get; private set; }
    public decimal PriceUsd { get; private set; }
    public string Provider { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }
}
