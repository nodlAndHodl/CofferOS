using CofferOS.Domain.Common;

namespace CofferOS.Domain.Wallets;

/// <summary>
/// A transaction as it relates to a specific wallet. CofferOS records the net
/// effect on the wallet rather than the full raw transaction, though the raw txid
/// is kept so richer data can be fetched from a node / explorer on demand.
/// </summary>
public sealed class WalletTransaction : Entity
{
    private WalletTransaction() { }

    public WalletTransaction(
        Guid walletId,
        string txId,
        long netAmountSats,
        long feeSats,
        TransactionDirection direction,
        int confirmations,
        long? blockHeight,
        string? blockHash,
        DateTimeOffset? timestamp)
    {
        WalletId = walletId;
        TxId = txId;
        NetAmountSats = netAmountSats;
        FeeSats = feeSats;
        Direction = direction;
        Confirmations = confirmations;
        BlockHeight = blockHeight;
        BlockHash = blockHash;
        Timestamp = timestamp;
    }

    public Guid WalletId { get; private set; }
    public string TxId { get; private set; } = string.Empty;

    /// <summary>Net effect on the wallet balance, in satoshis (can be negative).</summary>
    public long NetAmountSats { get; private set; }

    public long FeeSats { get; private set; }
    public TransactionDirection Direction { get; private set; }
    public int Confirmations { get; private set; }
    public long? BlockHeight { get; private set; }
    public string? BlockHash { get; private set; }
    public DateTimeOffset? Timestamp { get; private set; }

    public void UpdateConfirmations(int confirmations, long? blockHeight, string? blockHash)
    {
        Confirmations = confirmations;
        BlockHeight = blockHeight;
        BlockHash = blockHash;
    }
}
