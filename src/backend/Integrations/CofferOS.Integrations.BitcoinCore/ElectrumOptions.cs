namespace CofferOS.Integrations.BitcoinCore;

public sealed class ElectrumOptions
{
    public const string SectionName = "Integrations:ElectrumServer";

    public bool Enabled { get; set; }
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 50001;
    public string? Socks5Proxy { get; set; }
    public string Network { get; set; } = "Mainnet";
    /// <summary>Hard upper bound on addresses scanned per chain (safety cap for gap-limit scanning).</summary>
    public int AddressScanCount { get; set; } = 1000;

    /// <summary>Number of consecutive unused addresses that ends a chain scan (BIP-44 gap limit).</summary>
    public int GapLimit { get; set; } = 20;

    /// <summary>Number of addresses requested per Electrum get_history call. Should be close to GapLimit for the earliest possible early stop.</summary>
    public int DiscoveryWindowSize { get; set; } = 20;

    public int BatchSize { get; set; } = 100;
    public int TimeoutSeconds { get; set; } = 60;
}
