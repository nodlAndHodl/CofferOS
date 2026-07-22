namespace CofferOS.Integrations.BitcoinCore;

/// <summary>Configuration for connecting to a Bitcoin Core JSON-RPC endpoint.</summary>
public sealed class BitcoinCoreOptions
{
    public const string SectionName = "Integrations:BitcoinCore";

    /// <summary>When false, the provider is not registered and CofferOS runs without a node.</summary>
    public bool Enabled { get; set; }

    /// <summary>Full RPC URL, e.g. http://127.0.0.1:8332 or http://node.onion:8332.</summary>
    public string RpcUrl { get; set; } = "http://127.0.0.1:8332";

    /// <summary>Optional SOCKS5 proxy for .onion/Tor RPC, e.g. tor:9051.</summary>
    public string? Socks5Proxy { get; set; }

    public string? RpcUser { get; set; }

    public string? RpcPassword { get; set; }

    /// <summary>Optional wallet name for wallet-scoped RPC calls (appended to the URL path).</summary>
    public string? WalletName { get; set; }

    public int TimeoutSeconds { get; set; } = 30;
}
