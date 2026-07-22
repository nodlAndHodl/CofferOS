using CofferOS.Domain.Common;

namespace CofferOS.Application.Abstractions.Providers;

/// <summary>
/// A pluggable source of Bitcoin blockchain data. Implementations behave like
/// plugins: BitcoinCoreProvider, ElectrumProvider, MempoolProvider, ...
/// The rest of the application depends only on this abstraction, never on a
/// concrete implementation.
/// </summary>
public interface IBitcoinNodeProvider
{
    /// <summary>Stable identifier for the provider, e.g. "bitcoin-core".</summary>
    string ProviderId { get; }

    /// <summary>Human friendly name, e.g. "Bitcoin Core".</summary>
    string DisplayName { get; }

    /// <summary>Verifies connectivity and credentials without throwing.</summary>
    Task<NodeConnectionResult> TestConnectionAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns current chain state.</summary>
    Task<BlockchainInfo> GetBlockchainInfoAsync(CancellationToken cancellationToken = default);
}

/// <summary>Provides node-side wallet/account information where supported.</summary>
public interface IWalletProvider
{
    Task<NodeWalletInfo> GetWalletInfoAsync(CancellationToken cancellationToken = default);
}

/// <summary>Looks up individual transactions.</summary>
public interface ITransactionProvider
{
    Task<NodeTransaction?> GetTransactionAsync(string txId, CancellationToken cancellationToken = default);
}

/// <summary>Discovers UTXOs for a set of descriptors (e.g. Bitcoin Core scantxoutset).</summary>
public interface IUtxoProvider
{
    Task<IReadOnlyList<NodeUtxo>> ScanUtxoSetAsync(
        IReadOnlyCollection<string> descriptors,
        CancellationToken cancellationToken = default);
}

public sealed record NodeConnectionResult(bool Success, string ProviderId, string? Error = null);

public sealed record BlockchainInfo(
    string Chain,
    long Blocks,
    long Headers,
    string BestBlockHash,
    double VerificationProgress,
    bool InitialBlockDownload,
    bool Pruned,
    long SizeOnDiskBytes);

public sealed record NodeWalletInfo(string Name, long TxCount, double BalanceBtc, int? Descriptors);

public sealed record NodeTransaction(
    string TxId,
    int Confirmations,
    long? BlockHeight,
    string? BlockHash,
    DateTimeOffset? Timestamp,
    long FeeSats);

public sealed record NodeUtxo(
    string TxId,
    int Vout,
    long ValueSats,
    string ScriptPubKeyHex,
    string? Address,
    long? Height);

/// <summary>Discovers full wallet transaction history and per-address activity.</summary>
public interface IWalletHistoryProvider
{
    Task<WalletHistoryScan> GetWalletHistoryAsync(
        IReadOnlyCollection<(Guid DescriptorId, string Raw)> descriptors,
        CancellationToken cancellationToken = default);
}

public sealed record NodeAddressInfo(Guid DescriptorId, int Index, bool IsChange, string Address, string ScriptPubKeyHex);

public sealed record AddressTxRef(string Address, string TxId, long Height);

public sealed record NodeWalletTransaction(
    string TxId,
    long NetAmountSats,
    long FeeSats,
    TransactionDirection Direction,
    long? BlockHeight,
    DateTimeOffset? Timestamp);

public sealed record WalletHistoryScan(
    IReadOnlyList<NodeWalletTransaction> Transactions,
    IReadOnlyList<AddressTxRef> AddressHistory,
    IReadOnlyList<NodeAddressInfo> DerivedAddresses);
