namespace CofferOS.Integrations.BitcoinCore;

public sealed class ElectrumOptions
{
    public const string SectionName = "Integrations:ElectrumServer";

    public bool Enabled { get; set; }
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 50001;
    public string? Socks5Proxy { get; set; }
    public string Network { get; set; } = "Mainnet";
    public int AddressScanCount { get; set; } = 100;
    public int BatchSize { get; set; } = 100;
    public int TimeoutSeconds { get; set; } = 60;
}
