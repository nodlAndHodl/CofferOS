using System.Text.RegularExpressions;
using CofferOS.Application.Abstractions.Descriptors;
using CofferOS.Domain.Common;
using NBitcoin;
using NBitcoin.DataEncoders;
using ScriptType = CofferOS.Domain.Common.ScriptType;

namespace CofferOS.Infrastructure.Descriptors;

/// <summary>
/// Parses xpubs / output descriptors and derives addresses using NBitcoin.
///
/// The implementation is intentionally conservative: it extracts public key
/// material only, normalizes SLIP-132 prefixes, and derives addresses.
/// Supported:
/// - single-key: xpub, ypub, zpub, tpub and pkh/sh(wpkh)/wpkh/tr descriptors.
/// - multisig: multi / sortedmulti inside wsh, sh, or sh(wsh(...)).
///   Includes the modern &lt;0;1&gt;/* multipath expansion for receive/change chains.
/// It never sees or requires private key material.
/// </summary>
public sealed partial class NBitcoinDescriptorParser : IDescriptorParser
{
    public ParsedDescriptor Parse(string input, BitcoinNetwork network)
    {
        if (string.IsNullOrWhiteSpace(input))
            throw new ArgumentException("Descriptor is empty.", nameof(input));

        var raw = input.Trim();
        var (body, checksum) = SplitChecksum(raw);

        var scriptType = DetectScriptType(body);
        var source = body.Contains('(') ? DescriptorSource.OutputDescriptor : DescriptorSource.ExtendedPublicKey;

        if (TryParseMultisig(body, out var threshold, out var isSorted, out var keyTokens))
        {
            var cosigners = keyTokens
                .Select(t => ParseKeyExpression(t, network).ToCosignerInfo())
                .ToList();
            var first = cosigners.First();
            return new ParsedDescriptor(source, scriptType, raw, first.MasterFingerprint, first.OriginPath, checksum, threshold, isSorted, cosigners);
        }

        var single = ParseKeyExpression(ExtractSingleKeyExpression(body), network).ToCosignerInfo();
        return new ParsedDescriptor(source, scriptType, raw, single.MasterFingerprint, single.OriginPath, checksum, null, false, new[] { single });
    }

    public IReadOnlyList<DerivedAddress> Derive(
        ParsedDescriptor descriptor,
        BitcoinNetwork network,
        bool change,
        int startIndex,
        int count)
    {
        if (descriptor.Cosigners.Count > 1 || descriptor.Threshold.HasValue)
            return DeriveMultisig(descriptor, network, change, startIndex, count);

        return DeriveSingleKey(descriptor, network, change, startIndex, count);
    }

    private IReadOnlyList<DerivedAddress> DeriveSingleKey(ParsedDescriptor descriptor, BitcoinNetwork network, bool change, int startIndex, int count)
    {
        var nbNetwork = ToNetwork(network);
        var keyExpr = descriptor.Cosigners[0].KeyExpression;
        var parsed = ParseKeyExpression(ExtractSingleKeyExpression(descriptor.Raw), network);
        var scriptPubKeyType = ToScriptPubKeyType(descriptor.ScriptType);

        var results = new List<DerivedAddress>(count);
        for (var i = 0; i < count; i++)
        {
            var index = startIndex + i;
            var pubKey = DerivePublicKey(parsed, change, index);
            var address = pubKey.GetAddress(scriptPubKeyType, nbNetwork);
            results.Add(new DerivedAddress(index, change, address.ToString(), address.ScriptPubKey.ToHex()));
        }

        return results;
    }

    private IReadOnlyList<DerivedAddress> DeriveMultisig(ParsedDescriptor descriptor, BitcoinNetwork network, bool change, int startIndex, int count)
    {
        var nbNetwork = ToNetwork(network);
        if (!descriptor.Threshold.HasValue)
            throw new InvalidOperationException("Multisig descriptor missing threshold.");
        if (descriptor.Cosigners.Count < descriptor.Threshold.Value)
            throw new ArgumentException("Threshold exceeds number of cosigners.");

        var parsedKeys = descriptor.Cosigners
            .Select(c => ParseKeyExpression(ExtractSingleKeyExpression(c.KeyExpression), network))
            .ToList();

        var results = new List<DerivedAddress>(count);
        for (var i = 0; i < count; i++)
        {
            var index = startIndex + i;
            var pubKeys = parsedKeys.Select(k => DerivePublicKey(k, change, index)).ToList();

            if (descriptor.IsSortedMulti)
                pubKeys = SortPubKeys(pubKeys);

            var script = PayToMultiSigTemplate.Instance.GenerateScriptPubKey(descriptor.Threshold.Value, pubKeys.ToArray());
            var address = BuildAddress(script, descriptor.ScriptType, nbNetwork);
            results.Add(new DerivedAddress(index, change, address.ToString(), address.ScriptPubKey.ToHex()));
        }

        return results;
    }

    // ---- helpers -----------------------------------------------------------

    private static (string body, string? checksum) SplitChecksum(string raw)
    {
        var hash = raw.IndexOf('#');
        return hash >= 0 ? (raw[..hash], raw[(hash + 1)..]) : (raw, null);
    }

    private static ScriptType DetectScriptType(string body)
    {
        var s = body.TrimStart();
        if (s.StartsWith("sh(wpkh(", StringComparison.OrdinalIgnoreCase)) return ScriptType.P2shP2wpkh;
        if (s.StartsWith("sh(wsh(", StringComparison.OrdinalIgnoreCase)) return ScriptType.P2shP2wsh;
        if (s.StartsWith("wpkh(", StringComparison.OrdinalIgnoreCase)) return ScriptType.P2wpkh;
        if (s.StartsWith("wsh(", StringComparison.OrdinalIgnoreCase)) return ScriptType.P2wsh;
        if (s.StartsWith("tr(", StringComparison.OrdinalIgnoreCase)) return ScriptType.P2tr;
        if (s.StartsWith("pkh(", StringComparison.OrdinalIgnoreCase)) return ScriptType.P2pkh;
        if (s.StartsWith("sh(", StringComparison.OrdinalIgnoreCase)) return ScriptType.P2sh;

        // Raw extended key: infer from SLIP-132 prefix.
        if (s.StartsWith("zpub", StringComparison.Ordinal) || s.StartsWith("vpub", StringComparison.Ordinal))
            return ScriptType.P2wpkh;
        if (s.StartsWith("ypub", StringComparison.Ordinal) || s.StartsWith("upub", StringComparison.Ordinal))
            return ScriptType.P2shP2wpkh;
        // Plain xpub/tpub is BIP44 legacy by convention.
        return ScriptType.P2pkh;
    }

    private static string ExtractSingleKeyExpression(string body)
    {
        var s = body.Trim();
        while (s.EndsWith(")"))
        {
            var innerEnd = s.Length - 1;
            var innerStart = FindMatchingParen(s, innerEnd);
            if (innerStart < 1)
                break;

            var wrapper = s[..(innerStart - 1)];
            var inner = s[innerStart..innerEnd].Trim();

            if (wrapper.Equals("tr", StringComparison.OrdinalIgnoreCase))
            {
                var comma = FindTopLevelComma(inner);
                if (comma >= 0)
                    inner = inner[..comma].Trim();
                return inner;
            }

            if (wrapper.Equals("pkh", StringComparison.OrdinalIgnoreCase) ||
                wrapper.Equals("wpkh", StringComparison.OrdinalIgnoreCase) ||
                wrapper.Equals("sh", StringComparison.OrdinalIgnoreCase) ||
                wrapper.Equals("wsh", StringComparison.OrdinalIgnoreCase))
            {
                s = inner;
                continue;
            }

            break;
        }

        return s;
    }

    private static int FindMatchingParen(string s, int closeIndex)
    {
        var depth = 0;
        for (var i = closeIndex; i >= 0; i--)
        {
            if (s[i] == ')') depth++;
            else if (s[i] == '(')
            {
                depth--;
                if (depth == 0)
                    return i + 1;
            }
        }
        return -1;
    }

    private static int FindTopLevelComma(string s)
    {
        var depth = 0;
        var bracket = false;
        for (var i = 0; i < s.Length; i++)
        {
            var c = s[i];
            if (c == '[') bracket = true;
            else if (c == ']') bracket = false;
            else if (c == '(') depth++;
            else if (c == ')') depth--;
            else if (c == ',' && depth == 0 && !bracket)
                return i;
        }
        return -1;
    }

    private static bool TryParseMultisig(string body, out int threshold, out bool isSorted, out IReadOnlyList<string> keyExpressions)
    {
        threshold = 0;
        isSorted = false;
        keyExpressions = Array.Empty<string>();

        var match = MultiRegex().Match(body);
        if (!match.Success)
            return false;

        isSorted = match.Groups["sorted"].Success;
        if (!int.TryParse(match.Groups["threshold"].Value, out threshold) || threshold <= 0)
            throw new ArgumentException("Invalid multisig threshold.");

        var inner = match.Groups["inner"].Value;
        var tokens = SplitTopLevel(inner).Where(t => t.Length > 0).ToList();
        if (tokens.Count < threshold)
            throw new ArgumentException("Threshold exceeds number of keys.");

        keyExpressions = tokens;
        return true;
    }

    private static IReadOnlyList<string> SplitTopLevel(string input)
    {
        var result = new List<string>();
        var depth = 0;
        var bracket = false;
        var start = 0;
        for (var i = 0; i < input.Length; i++)
        {
            var c = input[i];
            if (c == '[') bracket = true;
            else if (c == ']') bracket = false;
            else if (c == '(') depth++;
            else if (c == ')') depth--;
            else if (c == ',' && depth == 0 && !bracket)
            {
                result.Add(input[start..i].Trim());
                start = i + 1;
            }
        }
        result.Add(input[start..].Trim());
        return result;
    }

    private sealed class ParsedKeyExpression
    {
        public string? Fingerprint { get; init; }
        public string? OriginPath { get; init; }
        public ExtPubKey ExtPubKey { get; init; } = null!;
        public string NormalizedXpub { get; init; } = null!;
        public string Suffix { get; init; } = string.Empty;

        public CosignerInfo ToCosignerInfo()
        {
            var prefix = !string.IsNullOrEmpty(Fingerprint) && !string.IsNullOrEmpty(OriginPath)
                ? $"[{Fingerprint}{OriginPath}]"
                : !string.IsNullOrEmpty(Fingerprint)
                    ? $"[{Fingerprint}]"
                    : string.Empty;
            var normalizedPath = string.IsNullOrEmpty(OriginPath)
                ? null
                : "m" + OriginPath.Replace("h", "'").Replace("H", "'");
            return new CosignerInfo(Fingerprint?.ToLowerInvariant(), normalizedPath, prefix + NormalizedXpub + Suffix);
        }
    }

    private static ParsedKeyExpression ParseKeyExpression(string token, BitcoinNetwork network)
    {
        var match = KeyExprRegex().Match(token.Trim());
        if (!match.Success)
            throw new ArgumentException($"Could not parse key expression: '{token}'");

        var fp = match.Groups["fp"].Success ? match.Groups["fp"].Value.ToLowerInvariant() : null;
        var originPath = match.Groups["path"].Success ? match.Groups["path"].Value : null;
        var extKeyToken = match.Groups["extkey"].Value;
        var suffix = match.Groups["suffix"].Value;

        var extKey = ToExtPubKey(extKeyToken, network);
        var normalized = extKey.ToString(ToNetwork(network));

        return new ParsedKeyExpression
        {
            Fingerprint = fp,
            OriginPath = originPath,
            ExtPubKey = extKey,
            NormalizedXpub = normalized,
            Suffix = suffix
        };
    }

    private static PubKey DerivePublicKey(ParsedKeyExpression key, bool change, int index)
    {
        var suffix = key.Suffix.Trim();
        if (string.IsNullOrEmpty(suffix))
        {
            // Bare xpub: derive /change/index per BIP44/84.
            return key.ExtPubKey.Derive((uint)(change ? 1 : 0)).Derive((uint)index).PubKey;
        }

        if (!suffix.StartsWith("/"))
            throw new ArgumentException($"Key expression suffix must start with '/': {suffix}");

        var segments = suffix.TrimStart('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0)
            return key.ExtPubKey.Derive((uint)(change ? 1 : 0)).Derive((uint)index).PubKey;

        var current = key.ExtPubKey;
        for (var s = 0; s < segments.Length - 1; s++)
        {
            if (segments[s] == "*")
                throw new ArgumentException("Wildcard '*' is only allowed as the last derivation step in a descriptor.");
            current = current.Derive((uint)ResolveSegment(segments[s], change));
        }

        var last = segments[^1];
        if (last == "*")
        {
            current = current.Derive((uint)index);
        }
        else
        {
            current = current.Derive((uint)ResolveSegment(last, change));
        }

        return current.PubKey;
    }

    private static int ResolveSegment(string segment, bool change)
    {
        if (segment.StartsWith('<') && segment.EndsWith('>'))
        {
            var inner = segment[1..^1];
            var choices = inner.Split(';', StringSplitOptions.RemoveEmptyEntries)
                .Select(static v => int.Parse(v, System.Globalization.CultureInfo.InvariantCulture))
                .ToList();
            if (choices.Count == 0)
                throw new ArgumentException("Empty multipath choice.");
            return change && choices.Count > 1 ? choices[1] : choices[0];
        }

        if (int.TryParse(segment, out var n))
            return n;

        throw new ArgumentException($"Unsupported descriptor derivation step: '{segment}'");
    }

    private static List<PubKey> SortPubKeys(IEnumerable<PubKey> pubKeys)
    {
        return pubKeys.OrderBy(p => p.ToBytes(), PubKeyLexicographicComparer.Instance).ToList();
    }

    private sealed class PubKeyLexicographicComparer : IComparer<byte[]>
    {
        public static readonly PubKeyLexicographicComparer Instance = new();
        public int Compare(byte[]? x, byte[]? y)
        {
            if (x is null || y is null) return 0;
            for (var i = 0; i < Math.Min(x.Length, y.Length); i++)
            {
                var c = x[i].CompareTo(y[i]);
                if (c != 0) return c;
            }
            return x.Length.CompareTo(y.Length);
        }
    }

    private static BitcoinAddress BuildAddress(Script script, ScriptType scriptType, Network network) => scriptType switch
    {
        ScriptType.P2wsh => script.WitHash.GetAddress(network),
        ScriptType.P2sh => script.Hash.GetAddress(network),
        ScriptType.P2shP2wsh => PayToWitScriptHashTemplate.Instance.GenerateScriptPubKey(script.WitHash).Hash.GetAddress(network),
        _ => throw new NotSupportedException($"Multisig address building not supported for script type {scriptType}.")
    };

    private static ExtPubKey ToExtPubKey(string token, BitcoinNetwork network)
    {
        var nbNetwork = ToNetwork(network);
        ValidatePrefixNetwork(token, network);

        try
        {
            return ExtPubKey.Parse(token, nbNetwork);
        }
        catch
        {
            // Fall through to SLIP-132 handling.
        }

        var data = Encoders.Base58Check.DecodeData(token);
        if (data.Length < 4)
            throw new ArgumentException("Invalid extended public key.");

        var version = network == BitcoinNetwork.Mainnet
            ? new byte[] { 0x04, 0x88, 0xB2, 0x1E }   // xpub
            : new byte[] { 0x04, 0x35, 0x87, 0xCF };  // tpub

        Array.Copy(version, 0, data, 0, 4);
        var normalized = Encoders.Base58Check.EncodeData(data);
        return ExtPubKey.Parse(normalized, nbNetwork);
    }

    private static void ValidatePrefixNetwork(string token, BitcoinNetwork network)
    {
        var mainnet = IsMainnetPrefix(token);
        var testnet = IsTestnetPrefix(token);

        if (network == BitcoinNetwork.Mainnet && !mainnet)
            throw new ArgumentException($"Extended public key prefix '{token[..4]}' is not valid for mainnet.");
        if (network != BitcoinNetwork.Mainnet && mainnet)
            throw new ArgumentException($"Extended public key prefix '{token[..4]}' is not valid for the selected network.");
    }

    private static bool IsMainnetPrefix(string token) =>
        token.StartsWith("xpub", StringComparison.Ordinal) ||
        token.StartsWith("ypub", StringComparison.Ordinal) ||
        token.StartsWith("zpub", StringComparison.Ordinal);

    private static bool IsTestnetPrefix(string token) =>
        token.StartsWith("tpub", StringComparison.Ordinal) ||
        token.StartsWith("upub", StringComparison.Ordinal) ||
        token.StartsWith("vpub", StringComparison.Ordinal);

    private static ScriptPubKeyType ToScriptPubKeyType(ScriptType type) => type switch
    {
        ScriptType.P2pkh => ScriptPubKeyType.Legacy,
        ScriptType.P2shP2wpkh => ScriptPubKeyType.SegwitP2SH,
        ScriptType.P2wpkh => ScriptPubKeyType.Segwit,
        ScriptType.P2tr => ScriptPubKeyType.TaprootBIP86,
        _ => throw new NotSupportedException(
            $"Address derivation for script type '{type}' is not supported yet. " +
            "Supported: legacy (pkh), nested segwit (sh(wpkh)), native segwit (wpkh), taproot (tr).")
    };

    private static Network ToNetwork(BitcoinNetwork network) => network switch
    {
        BitcoinNetwork.Mainnet => Network.Main,
        BitcoinNetwork.Testnet => Network.TestNet,
        BitcoinNetwork.Regtest => Network.RegTest,
        BitcoinNetwork.Signet => Network.GetNetwork("signet") ?? Network.TestNet,
        _ => Network.Main
    };

    [GeneratedRegex(@"\b(?<sorted>sorted)?multi\(\s*(?<threshold>\d+)\s*,\s*(?<inner>.*?)\)", RegexOptions.Singleline)]
    private static partial Regex MultiRegex();

    [GeneratedRegex(@"^(\[(?<fp>[0-9a-fA-F]{8})(?<path>(?:/[0-9]+['hH]?)*)\])?(?<extkey>(?:xpub|ypub|zpub|tpub|upub|vpub)[1-9A-HJ-NP-Za-km-z]{100,120})(?<suffix>.*)$")]
    private static partial Regex KeyExprRegex();

    [GeneratedRegex(@"(?:xpub|ypub|zpub|tpub|upub|vpub)[1-9A-HJ-NP-Za-km-z]{100,120}")]
    private static partial Regex ExtKeyRegex();
}
