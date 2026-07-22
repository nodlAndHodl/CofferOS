using System.Text.Json;
using CofferOS.Application.Abstractions.Providers;
using Microsoft.Extensions.Logging;

namespace CofferOS.Integrations.BitcoinCore;

/// <summary>
/// Bitcoin Core implementation of the node provider plugin contracts. Talks to a
/// local Bitcoin Core node over JSON-RPC. Read-only: it only calls query RPCs.
/// </summary>
public sealed class BitcoinCoreProvider : IBitcoinNodeProvider, ITransactionProvider, IUtxoProvider
{
    private const long SatsPerBtc = 100_000_000L;

    private readonly BitcoinCoreRpcClient _rpc;
    private readonly ILogger<BitcoinCoreProvider> _logger;

    public BitcoinCoreProvider(BitcoinCoreRpcClient rpc, ILogger<BitcoinCoreProvider> logger)
    {
        _rpc = rpc;
        _logger = logger;
    }

    public string ProviderId => "bitcoin-core";
    public string DisplayName => "Bitcoin Core";

    public async Task<NodeConnectionResult> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _rpc.CallAsync("getblockchaininfo", cancellationToken: cancellationToken);
            return new NodeConnectionResult(true, ProviderId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Bitcoin Core connection test failed");
            return new NodeConnectionResult(false, ProviderId, ex.Message);
        }
    }

    public async Task<BlockchainInfo> GetBlockchainInfoAsync(CancellationToken cancellationToken = default)
    {
        var r = await _rpc.CallAsync("getblockchaininfo", cancellationToken: cancellationToken);
        return new BlockchainInfo(
            Chain: GetString(r, "chain") ?? "unknown",
            Blocks: GetLong(r, "blocks"),
            Headers: GetLong(r, "headers"),
            BestBlockHash: GetString(r, "bestblockhash") ?? string.Empty,
            VerificationProgress: GetDouble(r, "verificationprogress"),
            InitialBlockDownload: GetBool(r, "initialblockdownload"),
            Pruned: GetBool(r, "pruned"),
            SizeOnDiskBytes: GetLong(r, "size_on_disk"));
    }

    public async Task<NodeTransaction?> GetTransactionAsync(string txId, CancellationToken cancellationToken = default)
    {
        try
        {
            var r = await _rpc.CallAsync("getrawtransaction", new object?[] { txId, true }, cancellationToken);
            var confirmations = r.TryGetProperty("confirmations", out var c) ? c.GetInt32() : 0;
            var blockHash = GetString(r, "blockhash");
            DateTimeOffset? time = r.TryGetProperty("blocktime", out var bt)
                ? DateTimeOffset.FromUnixTimeSeconds(bt.GetInt64())
                : null;

            return new NodeTransaction(txId, confirmations, null, blockHash, time, 0);
        }
        catch (BitcoinCoreRpcException ex)
        {
            _logger.LogInformation("Transaction {TxId} not found: {Message}", txId, ex.Message);
            return null;
        }
    }

    public async Task<IReadOnlyList<NodeUtxo>> ScanUtxoSetAsync(
        IReadOnlyCollection<string> descriptors,
        CancellationToken cancellationToken = default)
    {
        var scanObjects = descriptors.Select(d => (object)new { desc = d, range = 1000 }).ToArray();
        var r = await _rpc.CallAsync("scantxoutset", new object?[] { "start", scanObjects }, cancellationToken);

        var result = new List<NodeUtxo>();
        if (!r.TryGetProperty("unspents", out var unspents) || unspents.ValueKind != JsonValueKind.Array)
            return result;

        foreach (var u in unspents.EnumerateArray())
        {
            var amountBtc = GetDouble(u, "amount");
            result.Add(new NodeUtxo(
                TxId: GetString(u, "txid") ?? string.Empty,
                Vout: u.TryGetProperty("vout", out var v) ? v.GetInt32() : 0,
                ValueSats: (long)Math.Round(amountBtc * SatsPerBtc),
                ScriptPubKeyHex: GetString(u, "scriptPubKey") ?? string.Empty,
                Address: GetString(u, "desc"),
                Height: u.TryGetProperty("height", out var h) ? h.GetInt64() : null));
        }

        return result;
    }

    private static string? GetString(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    private static long GetLong(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetInt64() : 0;

    private static double GetDouble(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetDouble() : 0d;

    private static bool GetBool(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) && (v.ValueKind == JsonValueKind.True || v.ValueKind == JsonValueKind.False) && v.GetBoolean();
}
