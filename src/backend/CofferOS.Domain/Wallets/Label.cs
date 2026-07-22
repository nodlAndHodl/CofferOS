using CofferOS.Domain.Common;

namespace CofferOS.Domain.Wallets;

/// <summary>
/// A short, user-defined label attached to an address, transaction, UTXO or wallet.
/// Compatible in spirit with BIP-329 labelling.
/// </summary>
public sealed class Label : Entity
{
    private Label() { }

    public Label(Guid walletId, LabelTarget target, string reference, string text)
    {
        WalletId = walletId;
        Target = target;
        Reference = reference;
        Text = text;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid WalletId { get; private set; }

    /// <summary>What kind of object this label points at.</summary>
    public LabelTarget Target { get; private set; }

    /// <summary>The identifier of the target (address string, txid, outpoint...).</summary>
    public string Reference { get; private set; } = string.Empty;

    public string Text { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }

    public void Update(string text) => Text = text;
}
