using CofferOS.Domain.Common;

namespace CofferOS.Application.Abstractions.Descriptors;

/// <summary>
/// Parses xpubs and output descriptors and derives addresses from them. The
/// implementation (Infrastructure) uses NBitcoin. Kept behind an interface so the
/// application layer never depends on a specific Bitcoin library.
/// </summary>
public interface IDescriptorParser
{
    /// <summary>Parses an xpub or output descriptor string into normalized metadata.</summary>
    ParsedDescriptor Parse(string input, BitcoinNetwork network);

    /// <summary>Derives a contiguous range of addresses from a parsed descriptor.</summary>
    IReadOnlyList<DerivedAddress> Derive(
        ParsedDescriptor descriptor,
        BitcoinNetwork network,
        bool change,
        int startIndex,
        int count);
}

/// <summary>One cosigner extracted from a multisig descriptor.</summary>
/// <param name="MasterFingerprint">8-hex origin fingerprint, or null.</param>
/// <param name="OriginPath">Full origin path, e.g. m/48'/0'/0'/2', or null.</param>
/// <param name="KeyExpression">Full account-level key expression: [fp/path]xpub.../suffix (with SLIP-132 prefixes normalized).</param>
public sealed record CosignerInfo(
    string? MasterFingerprint,
    string? OriginPath,
    string KeyExpression);

/// <summary>Normalized, key-material-free description of an imported descriptor.</summary>
public sealed record ParsedDescriptor(
    DescriptorSource Source,
    ScriptType ScriptType,
    string Raw,
    string? MasterFingerprint,
    string? DerivationPath,
    string? Checksum,
    int? Threshold,
    bool IsSortedMulti,
    IReadOnlyList<CosignerInfo> Cosigners);

/// <summary>A single derived address plus the scriptPubKey it locks to.</summary>
public sealed record DerivedAddress(
    int Index,
    bool IsChange,
    string Address,
    string ScriptPubKeyHex);
