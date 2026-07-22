using CofferOS.Domain.Common;

namespace CofferOS.Domain.Wallets;

/// <summary>
/// A single cosigner inside a multisig descriptor. Stored as an owned child of
/// <see cref="Descriptor"/>.
/// </summary>
public sealed class Cosigner
{
    // EF Core constructor
    private Cosigner() { }

    public Cosigner(int orderIndex, string? masterFingerprint, string? originPath, string keyExpression)
    {
        OrderIndex = orderIndex;
        MasterFingerprint = masterFingerprint;
        OriginPath = originPath;
        KeyExpression = keyExpression;
    }

    public int OrderIndex { get; private set; }

    /// <summary>8-hex origin master key fingerprint, or null.</summary>
    public string? MasterFingerprint { get; private set; }

    /// <summary>Full origin path, e.g. m/48'/0'/0'/2'.</summary>
    public string? OriginPath { get; private set; }

    /// <summary>Full account-level key expression: [fp/path]xpub.../suffix.</summary>
    public string KeyExpression { get; private set; } = string.Empty;
}

/// <summary>
/// A descriptor is the source of truth in CofferOS. Everything else (addresses,
/// UTXOs, balances) is <em>derived</em> from a descriptor. A descriptor is either
/// a raw extended public key or a full output descriptor string. It never contains
/// private key material.
/// </summary>
public sealed class Descriptor : Entity
{
    // EF Core constructor
    private Descriptor() { }

    public Descriptor(
        Guid walletId,
        DescriptorSource source,
        ScriptType scriptType,
        string raw,
        string? masterFingerprint,
        string? derivationPath,
        string? checksum,
        int? threshold,
        bool isSortedMulti,
        IEnumerable<Cosigner>? cosigners)
    {
        WalletId = walletId;
        Source = source;
        ScriptType = scriptType;
        Raw = raw;
        MasterFingerprint = masterFingerprint;
        DerivationPath = derivationPath;
        Checksum = checksum;
        Threshold = threshold;
        IsSortedMulti = isSortedMulti;
        _cosigners.AddRange(cosigners ?? Enumerable.Empty<Cosigner>());
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid WalletId { get; private set; }

    /// <summary>Whether this originated from an xpub or a full output descriptor.</summary>
    public DescriptorSource Source { get; private set; }

    /// <summary>The address type this descriptor produces.</summary>
    public ScriptType ScriptType { get; private set; }

    /// <summary>The canonical descriptor / xpub string, without private keys.</summary>
    public string Raw { get; private set; } = string.Empty;

    /// <summary>Origin master key fingerprint (8 hex chars) when known.</summary>
    public string? MasterFingerprint { get; private set; }

    /// <summary>Origin derivation path, e.g. m/84'/0'/0'.</summary>
    public string? DerivationPath { get; private set; }

    /// <summary>Descriptor checksum (the part after '#') when present.</summary>
    public string? Checksum { get; private set; }

    /// <summary>For multisig descriptors, the required signature threshold (m in m-of-n).</summary>
    public int? Threshold { get; private set; }

    /// <summary>Whether this is a sortedmulti descriptor (BIP-67 pubkey sorting).</summary>
    public bool IsSortedMulti { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    private readonly List<Cosigner> _cosigners = new();
    public IReadOnlyCollection<Cosigner> Cosigners => _cosigners.AsReadOnly();

    private readonly List<Address> _addresses = new();
    public IReadOnlyCollection<Address> Addresses => _addresses.AsReadOnly();

    public Address DeriveAddress(int derivationIndex, bool isChange, string addressValue, string scriptPubKeyHex)
    {
        var address = new Address(WalletId, Id, derivationIndex, isChange, addressValue, scriptPubKeyHex);
        _addresses.Add(address);
        return address;
    }
}
