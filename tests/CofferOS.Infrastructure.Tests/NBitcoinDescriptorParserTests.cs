using CofferOS.Domain.Common;
using CofferOS.Infrastructure.Descriptors;
using NBitcoin;
using NBitcoin.DataEncoders;
using Xunit;
using ScriptType = CofferOS.Domain.Common.ScriptType;

namespace CofferOS.Infrastructure.Tests;

/// <summary>
/// Validates the descriptor parser using keys generated with NBitcoin, so the
/// tests do not depend on any external / hand-typed vectors.
/// </summary>
public class NBitcoinDescriptorParserTests
{
    private readonly NBitcoinDescriptorParser _parser = new();

    // A deterministic account-level (m/84'/0'/0') extended PUBLIC key on mainnet.
    private static string AccountXpub()
    {
        var seed = Encoders.Hex.DecodeData("000102030405060708090a0b0c0d0e0f");
        var master = ExtKey.CreateFromSeed(seed);
        var account = master.Derive(new KeyPath("84'/0'/0'"));
        return account.Neuter().ToString(Network.Main); // "xpub..."
    }

    private static string ToZpub(string xpub)
    {
        var data = Encoders.Base58Check.DecodeData(xpub);
        // mainnet zpub version bytes
        byte[] zpubVersion = { 0x04, 0xB2, 0x47, 0x46 };
        Array.Copy(zpubVersion, 0, data, 0, 4);
        return Encoders.Base58Check.EncodeData(data);
    }

    [Fact]
    public void Parses_plain_xpub_as_legacy_and_derives_p2pkh()
    {
        var parsed = _parser.Parse(AccountXpub(), BitcoinNetwork.Mainnet);

        Assert.Equal(DescriptorSource.ExtendedPublicKey, parsed.Source);
        Assert.Equal(ScriptType.P2pkh, parsed.ScriptType);

        var addresses = _parser.Derive(parsed, BitcoinNetwork.Mainnet, change: false, startIndex: 0, count: 3);
        Assert.Equal(3, addresses.Count);
        Assert.All(addresses, a => Assert.StartsWith("1", a.Address));
        Assert.All(addresses, a => Assert.False(string.IsNullOrWhiteSpace(a.ScriptPubKeyHex)));
    }

    [Fact]
    public void Parses_slip132_zpub_as_native_segwit()
    {
        var zpub = ToZpub(AccountXpub());

        var parsed = _parser.Parse(zpub, BitcoinNetwork.Mainnet);
        Assert.Equal(ScriptType.P2wpkh, parsed.ScriptType);

        var addresses = _parser.Derive(parsed, BitcoinNetwork.Mainnet, change: false, startIndex: 0, count: 2);
        Assert.All(addresses, a => Assert.StartsWith("bc1q", a.Address));
    }

    [Fact]
    public void Parses_wpkh_output_descriptor_with_origin_and_multipath()
    {
        var xpub = AccountXpub();
        var descriptor = $"wpkh([d34db33f/84h/0h/0h]{xpub}/<0;1>/*)";

        var parsed = _parser.Parse(descriptor, BitcoinNetwork.Mainnet);

        Assert.Equal(DescriptorSource.OutputDescriptor, parsed.Source);
        Assert.Equal(ScriptType.P2wpkh, parsed.ScriptType);
        Assert.Equal("d34db33f", parsed.MasterFingerprint);
        Assert.Equal("m/84'/0'/0'", parsed.DerivationPath);

        var receive = _parser.Derive(parsed, BitcoinNetwork.Mainnet, change: false, startIndex: 0, count: 2);
        var change = _parser.Derive(parsed, BitcoinNetwork.Mainnet, change: true, startIndex: 0, count: 2);

        Assert.All(receive, a => Assert.StartsWith("bc1q", a.Address));
        Assert.All(change, a => Assert.StartsWith("bc1q", a.Address));
        // Multipath expansion must produce different receive/change chains.
        Assert.NotEqual(receive[0].Address, change[0].Address);
    }

    [Fact]
    public void Derivation_is_deterministic()
    {
        var parsed = _parser.Parse(AccountXpub(), BitcoinNetwork.Mainnet);
        var first = _parser.Derive(parsed, BitcoinNetwork.Mainnet, false, 0, 5);
        var second = _parser.Derive(parsed, BitcoinNetwork.Mainnet, false, 0, 5);
        Assert.Equal(first.Select(a => a.Address), second.Select(a => a.Address));
    }

    [Fact]
    public void Rejects_garbage_input()
    {
        Assert.ThrowsAny<Exception>(() => _parser.Parse("not-a-real-key", BitcoinNetwork.Mainnet));
    }

    [Fact]
    public void Parses_wsh_sortedmulti_descriptor_with_multipath()
    {
        var xpubA = DeriveXpub("48'/0'/0'/2'");
        var xpubB = DeriveXpub("48'/0'/0'/3'");
        var xpubC = DeriveXpub("48'/0'/0'/4'");
        var descriptor = $"wsh(sortedmulti(2,[d34db33f/48h/0h/0h/2h]{xpubA}/<0;1>/*,[a1a1a1a1/48h/0h/0h/3h]{xpubB}/<0;1>/*,[b2b2b2b2/48h/0h/0h/4h]{xpubC}/<0;1>/*))";

        var parsed = _parser.Parse(descriptor, BitcoinNetwork.Mainnet);

        Assert.Equal(DescriptorSource.OutputDescriptor, parsed.Source);
        Assert.Equal(ScriptType.P2wsh, parsed.ScriptType);
        Assert.Equal(2, parsed.Threshold);
        Assert.True(parsed.IsSortedMulti);
        Assert.Equal(3, parsed.Cosigners.Count);
        Assert.Equal("d34db33f", parsed.Cosigners[0].MasterFingerprint);
        Assert.Equal("m/48'/0'/0'/2'", parsed.Cosigners[0].OriginPath);
    }

    [Fact]
    public void Derives_p2wsh_receive_and_change_addresses_from_sortedmulti()
    {
        var xpubA = DeriveXpub("48'/0'/0'/2'");
        var xpubB = DeriveXpub("48'/0'/0'/3'");
        var xpubC = DeriveXpub("48'/0'/0'/4'");
        var descriptor = $"wsh(sortedmulti(2,[d34db33f/48h/0h/0h/2h]{xpubA}/<0;1>/*,[a1a1a1a1/48h/0h/0h/3h]{xpubB}/<0;1>/*,[b2b2b2b2/48h/0h/0h/4h]{xpubC}/<0;1>/*))";

        var parsed = _parser.Parse(descriptor, BitcoinNetwork.Mainnet);

        var receive = _parser.Derive(parsed, BitcoinNetwork.Mainnet, change: false, startIndex: 0, count: 2);
        var change = _parser.Derive(parsed, BitcoinNetwork.Mainnet, change: true, startIndex: 0, count: 2);

        Assert.Equal(2, receive.Count);
        Assert.Equal(2, change.Count);
        Assert.All(receive, a => Assert.StartsWith("bc1q", a.Address));
        Assert.All(change, a => Assert.StartsWith("bc1q", a.Address));
        Assert.NotEqual(receive[0].Address, change[0].Address);
        Assert.All(receive, a => Assert.False(string.IsNullOrWhiteSpace(a.ScriptPubKeyHex)));
    }

    [Fact]
    public void Sortedmulti_differs_from_unsorted_multi_for_same_keys()
    {
        var xpubA = DeriveXpub("48'/0'/0'/2'");
        var xpubB = DeriveXpub("48'/0'/0'/3'");
        var xpubC = DeriveXpub("48'/0'/0'/4'");
        var sorted = $"wsh(sortedmulti(2,[d34db33f/48h/0h/0h/2h]{xpubA}/<0;1>/*,[a1a1a1a1/48h/0h/0h/3h]{xpubB}/<0;1>/*,[b2b2b2b2/48h/0h/0h/4h]{xpubC}/<0;1>/*))";
        var unsorted = $"wsh(multi(2,[d34db33f/48h/0h/0h/2h]{xpubA}/<0;1>/*,[a1a1a1a1/48h/0h/0h/3h]{xpubB}/<0;1>/*,[b2b2b2b2/48h/0h/0h/4h]{xpubC}/<0;1>/*))";

        var sortedParsed = _parser.Parse(sorted, BitcoinNetwork.Mainnet);
        var unsortedParsed = _parser.Parse(unsorted, BitcoinNetwork.Mainnet);

        var sortedAddr = _parser.Derive(sortedParsed, BitcoinNetwork.Mainnet, false, 0, 1)[0].Address;
        var unsortedAddr = _parser.Derive(unsortedParsed, BitcoinNetwork.Mainnet, false, 0, 1)[0].Address;

        Assert.NotEqual(sortedAddr, unsortedAddr);
    }

    [Fact]
    public void Derives_sh_and_sh_wsh_multisig_addresses()
    {
        var xpubA = DeriveXpub("48'/0'/0'/2'");
        var xpubB = DeriveXpub("48'/0'/0'/3'");
        var sh = $"sh(sortedmulti(2,[d34db33f/48h/0h/0h/2h]{xpubA}/0/*,[a1a1a1a1/48h/0h/0h/3h]{xpubB}/0/*))";
        var shwsh = $"sh(wsh(sortedmulti(2,[d34db33f/48h/0h/0h/2h]{xpubA}/0/*,[a1a1a1a1/48h/0h/0h/3h]{xpubB}/0/*)))";

        var shParsed = _parser.Parse(sh, BitcoinNetwork.Mainnet);
        var shwshParsed = _parser.Parse(shwsh, BitcoinNetwork.Mainnet);

        Assert.Equal(ScriptType.P2sh, shParsed.ScriptType);
        Assert.Equal(ScriptType.P2shP2wsh, shwshParsed.ScriptType);

        var shAddr = _parser.Derive(shParsed, BitcoinNetwork.Mainnet, false, 0, 1)[0];
        var shwshAddr = _parser.Derive(shwshParsed, BitcoinNetwork.Mainnet, false, 0, 1)[0];

        Assert.StartsWith("3", shAddr.Address);
        Assert.StartsWith("3", shwshAddr.Address);
        Assert.NotEqual(shAddr.Address, shwshAddr.Address);
    }

    private static string DeriveXpub(string path)
    {
        var seed = Encoders.Hex.DecodeData("000102030405060708090a0b0c0d0e0f");
        var master = ExtKey.CreateFromSeed(seed);
        return master.Derive(new KeyPath(path)).Neuter().ToString(Network.Main);
    }
}
