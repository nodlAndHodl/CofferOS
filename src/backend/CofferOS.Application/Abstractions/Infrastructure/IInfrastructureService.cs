namespace CofferOS.Application.Abstractions.Infrastructure;

/// <summary>
/// Provides infrastructure health and status information.
/// Designed to support future services like Lightning, Mempool, and additional node types.
/// </summary>
public interface IInfrastructureService
{
    /// <summary>
    /// Gets the current infrastructure status.
    /// </summary>
    Task<InfrastructureStatus> GetStatusAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Current infrastructure status.
/// </summary>
public sealed record InfrastructureStatus(
    int WalletCount,
    BitcoinNodeStatus? BitcoinNode,
    ElectrumStatus? Electrum);

/// <summary>
/// Bitcoin node status.
/// </summary>
public sealed record BitcoinNodeStatus(
    bool Connected,
    string ProviderId,
    string? Chain,
    long? BlockHeight,
    double? VerificationProgress,
    string? Error);

/// <summary>
/// Electrum server status.
/// </summary>
public sealed record ElectrumStatus(
    bool Connected,
    string ProviderId,
    string Host,
    int Port,
    string? Socks5Proxy,
    long? BlockHeight,
    string? Error);
