using CofferOS.Domain.Common;

namespace CofferOS.Domain.Wallets;

/// <summary>
/// A derived address. Addresses are not primary data; they are deterministic
/// outputs of a <see cref="Descriptor"/> at a given derivation index.
/// </summary>
public sealed class Address : Entity
{
    private Address() { }

    public Address(Guid walletId, Guid descriptorId, int derivationIndex, bool isChange, string value, string scriptPubKeyHex)
    {
        WalletId = walletId;
        DescriptorId = descriptorId;
        DerivationIndex = derivationIndex;
        IsChange = isChange;
        Value = value;
        ScriptPubKeyHex = scriptPubKeyHex;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid WalletId { get; private set; }
    public Guid DescriptorId { get; private set; }

    /// <summary>Index in the derivation chain (0/1/2...).</summary>
    public int DerivationIndex { get; private set; }

    /// <summary>True for the change (internal) chain, false for the receive (external) chain.</summary>
    public bool IsChange { get; private set; }

    /// <summary>The encoded address string.</summary>
    public string Value { get; private set; } = string.Empty;

    public string ScriptPubKeyHex { get; private set; } = string.Empty;

    /// <summary>Marked true once the address has appeared in a transaction.</summary>
    public bool IsUsed { get; private set; }

    /// <summary>Number of distinct transactions that have touched this address.</summary>
    public int UseCount { get; private set; }

    /// <summary>First transaction id that touched this address, chronologically.</summary>
    public string? FirstTxId { get; private set; }

    /// <summary>Most recent transaction id that touched this address.</summary>
    public string? LastTxId { get; private set; }

    /// <summary>Sum of unspent outputs still locked to this address, in satoshis.</summary>
    public long CurrentSats { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public void MarkUsed() => IsUsed = true;

    public void UpdateStats(int useCount, string? firstTxId, string? lastTxId, long currentSats)
    {
        UseCount = useCount;
        FirstTxId = firstTxId;
        LastTxId = lastTxId;
        CurrentSats = currentSats;
        IsUsed = useCount > 0;
    }
}
