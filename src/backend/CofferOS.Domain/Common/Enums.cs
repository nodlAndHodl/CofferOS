namespace CofferOS.Domain.Common;

/// <summary>Bitcoin network the wallet / descriptor belongs to.</summary>
public enum BitcoinNetwork
{
    Mainnet = 0,
    Testnet = 1,
    Signet = 2,
    Regtest = 3
}

/// <summary>The kind of key material a descriptor was imported from.</summary>
public enum DescriptorSource
{
    /// <summary>A raw extended public key (xpub / ypub / zpub / tpub ...).</summary>
    ExtendedPublicKey = 0,

    /// <summary>A full output descriptor string, e.g. wpkh([fp/84h/0h/0h]xpub.../0/*).</summary>
    OutputDescriptor = 1
}

/// <summary>Bitcoin script type used to derive addresses from a descriptor.</summary>
public enum ScriptType
{
    Unknown = 0,
    P2pkh = 1,   // legacy       (1...)
    P2sh = 2,    // nested       (3...)
    P2shP2wpkh = 3,
    P2wpkh = 4,  // native segwit (bc1q...)
    P2wsh = 5,
    P2tr = 6,    // taproot       (bc1p...)
    P2shP2wsh = 7 // P2SH of P2WSH (3...) for sh(wsh(...))
}

/// <summary>Direction of a transaction relative to the wallet.</summary>
public enum TransactionDirection
{
    Incoming = 0,
    Outgoing = 1,
    Internal = 2
}

/// <summary>The type of object a label or note is attached to.</summary>
public enum LabelTarget
{
    Address = 0,
    Transaction = 1,
    Utxo = 2,
    Wallet = 3
}

/// <summary>
/// The source / kind of a wallet timeline event. On-chain transaction events are
/// generated at query time; the remaining values cover user annotations and are
/// reserved for future event sources (Lightning, nodes, multisig, migrations).
/// </summary>
public enum TimelineEventType
{
    /// <summary>Free-form user annotation ("Moved funds to cold storage").</summary>
    Annotation = 0,

    /// <summary>Generated from an incoming on-chain transaction.</summary>
    TransactionReceived = 1,

    /// <summary>Generated from an outgoing on-chain transaction.</summary>
    TransactionSent = 2,

    /// <summary>Generated when the wallet was imported into CofferOS.</summary>
    WalletImported = 3,

    /// <summary>Reserved: Lightning channel / payment events.</summary>
    Lightning = 4,

    /// <summary>Reserved: Bitcoin / Lightning node lifecycle events.</summary>
    Node = 5,

    /// <summary>Reserved: multisig setup / cosigner events.</summary>
    Multisig = 6,

    /// <summary>Reserved: wallet migration events (sweeps, descriptor rotations).</summary>
    WalletMigration = 7
}
