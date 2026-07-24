using CofferOS.Domain.Common;

namespace CofferOS.Domain.Wallets;

/// <summary>
/// A short, free-form tag attached to an address, transaction, UTXO or wallet.
/// Unlike labels (one descriptive title), an object can carry many tags
/// (e.g. "truck", "personal", "large-expense").
/// </summary>
public sealed class Tag : Entity
{
    private Tag() { }

    public Tag(Guid walletId, LabelTarget target, string reference, string value)
    {
        WalletId = walletId;
        Target = target;
        Reference = reference;
        Value = Normalize(value);
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid WalletId { get; private set; }

    /// <summary>What kind of object this tag points at.</summary>
    public LabelTarget Target { get; private set; }

    /// <summary>The identifier of the target (address string, txid, outpoint...).</summary>
    public string Reference { get; private set; } = string.Empty;

    public string Value { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Tags are stored lower-case and trimmed so "Truck" and "truck" are the same tag.</summary>
    public static string Normalize(string value) => value.Trim().ToLowerInvariant();
}
