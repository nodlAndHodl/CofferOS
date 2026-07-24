using CofferOS.Domain.Common;

namespace CofferOS.Domain.Wallets;

/// <summary>
/// A single user-defined category assigned to an address, transaction, UTXO or wallet
/// (e.g. "Vehicle", "Income", "Cold Storage"). At most one category per object.
/// </summary>
public sealed class Category : Entity
{
    private Category() { }

    public Category(Guid walletId, LabelTarget target, string reference, string name)
    {
        WalletId = walletId;
        Target = target;
        Reference = reference;
        Name = name;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid WalletId { get; private set; }

    /// <summary>What kind of object this category is assigned to.</summary>
    public LabelTarget Target { get; private set; }

    /// <summary>The identifier of the target (address string, txid, outpoint...).</summary>
    public string Reference { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }

    public void Update(string name) => Name = name;
}
