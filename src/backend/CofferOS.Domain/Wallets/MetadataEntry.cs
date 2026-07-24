using CofferOS.Domain.Common;

namespace CofferOS.Domain.Wallets;

/// <summary>
/// A user-defined key/value pair attached to an address, transaction, UTXO or wallet.
/// This is the escape hatch for structured metadata CofferOS does not model directly
/// (e.g. "invoice" = "INV-2026-014", "counterparty" = "private seller").
/// </summary>
public sealed class MetadataEntry : Entity
{
    private MetadataEntry() { }

    public MetadataEntry(Guid walletId, LabelTarget target, string reference, string key, string value)
    {
        WalletId = walletId;
        Target = target;
        Reference = reference;
        Key = key.Trim();
        Value = value;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public Guid WalletId { get; private set; }

    /// <summary>What kind of object this metadata is attached to.</summary>
    public LabelTarget Target { get; private set; }

    /// <summary>The identifier of the target (address string, txid, outpoint...).</summary>
    public string Reference { get; private set; } = string.Empty;

    public string Key { get; private set; } = string.Empty;
    public string Value { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public void Update(string value)
    {
        Value = value;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
