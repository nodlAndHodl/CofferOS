using CofferOS.Domain.Common;

namespace CofferOS.Domain.Wallets;

/// <summary>
/// An unspent transaction output belonging to a wallet. Identified by the
/// outpoint (txid:vout).
/// </summary>
public sealed class Utxo : Entity
{
    private Utxo() { }

    public Utxo(
        Guid walletId,
        string txId,
        int vout,
        long valueSats,
        string scriptPubKeyHex,
        string? address,
        int confirmations,
        long? blockHeight)
    {
        WalletId = walletId;
        TxId = txId;
        Vout = vout;
        ValueSats = valueSats;
        ScriptPubKeyHex = scriptPubKeyHex;
        Address = address;
        Confirmations = confirmations;
        BlockHeight = blockHeight;
    }

    public Guid WalletId { get; private set; }
    public string TxId { get; private set; } = string.Empty;
    public int Vout { get; private set; }
    public long ValueSats { get; private set; }
    public string ScriptPubKeyHex { get; private set; } = string.Empty;
    public string? Address { get; private set; }
    public int Confirmations { get; private set; }
    public long? BlockHeight { get; private set; }
    public bool IsSpent { get; private set; }

    public string Outpoint => $"{TxId}:{Vout}";

    public void MarkSpent() => IsSpent = true;

    public void UpdateConfirmations(int confirmations) => Confirmations = confirmations;
}
