using CofferOS.Domain.Common;
using CofferOS.Domain.Events;

namespace CofferOS.Domain.Wallets;

/// <summary>
/// The Wallet aggregate root. A wallet in CofferOS is always <b>watch-only</b>:
/// it is a named collection of descriptors plus the data derived and observed
/// from them. It never holds private keys or seeds.
/// </summary>
public sealed class Wallet : Entity
{
    private Wallet() { }

    private Wallet(string name, string? description, BitcoinNetwork network)
    {
        Name = name;
        Description = description;
        Network = network;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public BitcoinNetwork Network { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Always true. Encoded explicitly so the invariant is visible in data.</summary>
    public bool WatchOnly { get; private set; } = true;

    private readonly List<Descriptor> _descriptors = new();
    public IReadOnlyCollection<Descriptor> Descriptors => _descriptors.AsReadOnly();

    private readonly List<WalletTransaction> _transactions = new();
    public IReadOnlyCollection<WalletTransaction> Transactions => _transactions.AsReadOnly();

    private readonly List<Utxo> _utxos = new();
    public IReadOnlyCollection<Utxo> Utxos => _utxos.AsReadOnly();

    /// <summary>Factory that creates a wallet and records the import event.</summary>
    public static Wallet Create(string name, string? description, BitcoinNetwork network)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Wallet name is required.", nameof(name));

        var wallet = new Wallet(name.Trim(), description?.Trim(), network);
        wallet.Raise(new WalletImportedEvent(wallet.Id, wallet.Name));
        return wallet;
    }

    public Descriptor AddDescriptor(
        DescriptorSource source,
        ScriptType scriptType,
        string raw,
        string? masterFingerprint,
        string? derivationPath,
        string? checksum,
        int? threshold = null,
        bool isSortedMulti = false,
        IEnumerable<Cosigner>? cosigners = null)
    {
        var descriptor = new Descriptor(Id, source, scriptType, raw, masterFingerprint, derivationPath, checksum, threshold, isSortedMulti, cosigners);
        _descriptors.Add(descriptor);
        Raise(new DescriptorAddedEvent(Id, descriptor.Id));
        return descriptor;
    }

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Wallet name is required.", nameof(name));
        Name = name.Trim();
    }
}
