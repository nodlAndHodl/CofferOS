namespace CofferOS.Infrastructure.Providers;

/// <summary>
/// Configuration for the Bitcoin price engine.
/// </summary>
public sealed class BitcoinPriceOptions
{
    public const string SectionName = "BitcoinPrice";

    /// <summary>Enable/disable automatic price refresh background worker.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Disable ALL outbound HTTP for price fetching (privacy mode).</summary>
    public bool PrivacyMode { get; set; } = false;

    /// <summary>Polling interval in seconds. Default 5 minutes.</summary>
    public int PollIntervalSeconds { get; set; } = 300;

    /// <summary>
    /// Selected provider id: "manual", "coingecko", "coinbase".
    /// </summary>
    public string Provider { get; set; } = "manual";
}
