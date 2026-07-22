using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CofferOS.Application.Abstractions.Descriptors;
using CofferOS.Application.Abstractions.Providers;
using CofferOS.Application.Contracts;
using CofferOS.Domain.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NBitcoin;

namespace CofferOS.Integrations.BitcoinCore;

/// <summary>
/// Electrum X1 protocol client. Fetches UTXOs by scripthash for each derived address.
/// Can connect directly or over the configured Tor SOCKS5 proxy.
/// </summary>
public sealed class ElectrumServerProvider : IUtxoProvider, IWalletHistoryProvider
{
    private readonly ElectrumOptions _options;
    private readonly IDescriptorParser _parser;
    private readonly ILogger<ElectrumServerProvider> _logger;

    public ElectrumServerProvider(
        IOptions<ElectrumOptions> options,
        IDescriptorParser parser,
        ILogger<ElectrumServerProvider> logger)
    {
        _options = options.Value;
        _parser = parser;
        _logger = logger;
    }

    public async Task<IReadOnlyList<NodeUtxo>> ScanUtxoSetAsync(
        IReadOnlyCollection<string> descriptors,
        CancellationToken cancellationToken = default)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(_options.TimeoutSeconds));
        var token = cts.Token;

        var scripthashes = new List<(string Scripthash, DerivedAddress Address)>();
        foreach (var raw in descriptors)
        {
            var (parsed, network) = ParseDescriptor(raw);
            var receive = _parser.Derive(parsed, network, false, 0, _options.AddressScanCount);
            var change = _parser.Derive(parsed, network, true, 0, _options.AddressScanCount);
            scripthashes.AddRange(receive.Concat(change).Select(a => (ToScripthash(a.ScriptPubKeyHex), a)));
        }

        var results = new List<NodeUtxo>();

        _logger.LogInformation(
            "Connecting to Electrum server {Host}:{Port} via proxy {Proxy}...",
            _options.Host,
            _options.Port,
            _options.Socks5Proxy ?? "none");

        await using var stream = await ConnectAsync(token);
        var encoding = new UTF8Encoding(false);
        using var writer = new StreamWriter(stream, encoding, bufferSize: 1024, leaveOpen: true) { AutoFlush = true, NewLine = "\n" };
        using var reader = new StreamReader(stream, encoding, detectEncodingFromByteOrderMarks: false, bufferSize: 1024, leaveOpen: true);

        _logger.LogInformation("Negotiating Electrum protocol version...");
        await HandshakeAsync(writer, reader, token);

        for (var i = 0; i < scripthashes.Count; i += _options.BatchSize)
        {
            var batch = scripthashes.Skip(i).Take(_options.BatchSize).ToList();
            var requests = batch.Select((s, idx) => new { jsonrpc = "2.0", method = "blockchain.scripthash.listunspent", @params = new[] { s.Scripthash }, id = i + idx + 1 }).ToList();
            var requestLine = JsonSerializer.Serialize(requests);
            _logger.LogInformation(
                "Sending Electrum batch {BatchIndex} with {Count} scripthashes...",
                i / _options.BatchSize + 1,
                batch.Count);
            await writer.WriteLineAsync(requestLine.AsMemory(), token);

            var responses = await ReadBatchAsync(reader, batch.Count, token);
            foreach (var resp in responses)
            {
                var id = resp.GetProperty("id").GetInt32();
                var batchIndex = id - i - 1;
                if (batchIndex < 0 || batchIndex >= batch.Count)
                    throw new InvalidOperationException($"Unexpected Electrum response id {id}.");

                var address = batch[batchIndex].Address;

                if (resp.TryGetProperty("error", out var err) && err.ValueKind != JsonValueKind.Null)
                    throw new InvalidOperationException($"Electrum error: {err}");

                foreach (var u in resp.GetProperty("result").EnumerateArray())
                {
                    var height = u.GetProperty("height");
                    long? heightValue = height.ValueKind == JsonValueKind.Number ? height.GetInt64() : null;
                    if (heightValue <= 0)
                        heightValue = null;

                    results.Add(new NodeUtxo(
                        u.GetProperty("tx_hash").GetString()!,
                        u.GetProperty("tx_pos").GetInt32(),
                        u.GetProperty("value").GetInt64(),
                        address.ScriptPubKeyHex,
                        address.Address,
                        heightValue));
                }
            }
        }

        _logger.LogInformation("Electrum scan complete; found {Count} UTXOs.", results.Count);
        return results;
    }

    public async Task<WalletHistoryScan> GetWalletHistoryAsync(
        IReadOnlyCollection<(Guid DescriptorId, string Raw)> descriptors,
        CancellationToken cancellationToken = default)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(_options.TimeoutSeconds));
        var token = cts.Token;

        var addressesByScriptPubKey = new Dictionary<string, DerivedAddress>();
        var scripthashes = new List<(string Scripthash, DerivedAddress Address)>();
        var derivedAddresses = new List<NodeAddressInfo>();
        Network? nbNetwork = null;

        foreach (var (descriptorId, raw) in descriptors)
        {
            var (parsed, network) = ParseDescriptor(raw);
            nbNetwork ??= ToNetwork(network);
            var receive = _parser.Derive(parsed, network, false, 0, _options.AddressScanCount);
            var change = _parser.Derive(parsed, network, true, 0, _options.AddressScanCount);
            foreach (var a in receive.Concat(change))
            {
                var key = a.ScriptPubKeyHex.ToLowerInvariant();
                derivedAddresses.Add(new NodeAddressInfo(descriptorId, a.Index, a.IsChange, a.Address, a.ScriptPubKeyHex));
                if (addressesByScriptPubKey.ContainsKey(key))
                    continue;
                addressesByScriptPubKey[key] = a;
                scripthashes.Add((ToScripthash(a.ScriptPubKeyHex), a));
            }
        }

        if (nbNetwork is null || scripthashes.Count == 0)
            return new WalletHistoryScan(Array.Empty<NodeWalletTransaction>(), Array.Empty<AddressTxRef>(), derivedAddresses);

        _logger.LogInformation(
            "Fetching Electrum history for {AddressCount} addresses from {Host}:{Port}...",
            scripthashes.Count,
            _options.Host,
            _options.Port);

        await using var stream = await ConnectAsync(token);
        var encoding = new UTF8Encoding(false);
        using var writer = new StreamWriter(stream, encoding, bufferSize: 1024, leaveOpen: true) { AutoFlush = true, NewLine = "\n" };
        using var reader = new StreamReader(stream, encoding, detectEncodingFromByteOrderMarks: false, bufferSize: 1024, leaveOpen: true);

        await HandshakeAsync(writer, reader, token);

        var addressHistory = new List<AddressTxRef>();
        var txIdToHeight = new Dictionary<string, long>();

        for (var i = 0; i < scripthashes.Count; i += _options.BatchSize)
        {
            var batch = scripthashes.Skip(i).Take(_options.BatchSize).ToList();
            var requests = batch.Select((s, idx) => new { jsonrpc = "2.0", method = "blockchain.scripthash.get_history", @params = new[] { s.Scripthash }, id = i + idx + 1 }).ToList();
            var requestLine = JsonSerializer.Serialize(requests);
            _logger.LogInformation("Sending get_history batch {BatchIndex} with {Count} scripthashes...", i / _options.BatchSize + 1, batch.Count);
            await writer.WriteLineAsync(requestLine.AsMemory(), token);

            var responses = await ReadBatchAsync(reader, batch.Count, token);
            foreach (var resp in responses)
            {
                var id = resp.GetProperty("id").GetInt32();
                var batchIndex = id - i - 1;
                if (batchIndex < 0 || batchIndex >= batch.Count)
                    throw new InvalidOperationException($"Unexpected Electrum response id {id}.");

                var address = batch[batchIndex].Address;

                if (resp.TryGetProperty("error", out var err) && err.ValueKind != JsonValueKind.Null)
                    throw new InvalidOperationException($"Electrum error: {err}");

                foreach (var e in resp.GetProperty("result").EnumerateArray())
                {
                    var txId = e.GetProperty("tx_hash").GetString()!;
                    var height = e.GetProperty("height").GetInt64();
                    addressHistory.Add(new AddressTxRef(address.Address, txId, height));
                    txIdToHeight[txId] = height;
                }
            }
        }

        _logger.LogInformation("Found {TxCount} unique transactions in history; fetching raw transactions...", txIdToHeight.Count);
        var txCache = await FetchTransactionsAsync(reader, writer, txIdToHeight.Keys.ToList(), nbNetwork, token);

        var prevTxIds = new HashSet<string>();
        foreach (var tx in txCache.Values)
        {
            foreach (var input in tx.Inputs)
                prevTxIds.Add(input.PrevOut.Hash.ToString());
        }

        _logger.LogInformation("Fetching {PrevCount} prevout transactions...", prevTxIds.Count);
        var prevTxCache = await FetchTransactionsAsync(reader, writer, prevTxIds.ToList(), nbNetwork, token);

        var uniqueHeights = txIdToHeight.Values.Where(h => h > 0).Distinct().ToList();
        var blockTimestamps = await FetchBlockTimestampsAsync(reader, writer, uniqueHeights, token);

        var transactions = new List<NodeWalletTransaction>();
        foreach (var (txId, height) in txIdToHeight)
        {
            var tx = txCache[txId];
            long totalOutput = 0, totalInput = 0;
            long walletOutput = 0, walletInput = 0;

            for (var o = 0; o < tx.Outputs.Count; o++)
            {
                var output = tx.Outputs[o];
                totalOutput += output.Value.Satoshi;
                var scriptHex = output.ScriptPubKey.ToHex().ToLowerInvariant();
                if (addressesByScriptPubKey.TryGetValue(scriptHex, out _))
                    walletOutput += output.Value.Satoshi;
            }

            for (var n = 0; n < tx.Inputs.Count; n++)
            {
                var input = tx.Inputs[n];
                var prevTx = prevTxCache[input.PrevOut.Hash.ToString()];
                var prevOut = prevTx.Outputs[(int)input.PrevOut.N];
                totalInput += prevOut.Value.Satoshi;
                var scriptHex = prevOut.ScriptPubKey.ToHex().ToLowerInvariant();
                if (addressesByScriptPubKey.TryGetValue(scriptHex, out _))
                    walletInput += prevOut.Value.Satoshi;
            }

            var net = walletOutput - walletInput;
            var direction = net > 0 ? TransactionDirection.Incoming : net < 0 ? TransactionDirection.Outgoing : TransactionDirection.Internal;
            var fee = totalInput - totalOutput;
            blockTimestamps.TryGetValue(height, out var timestamp);

            transactions.Add(new NodeWalletTransaction(
                txId,
                net,
                fee,
                direction,
                height > 0 ? height : null,
                timestamp));
        }

        _logger.LogInformation("Electrum history complete; parsed {TransactionCount} transactions.", transactions.Count);
        return new WalletHistoryScan(transactions, addressHistory, derivedAddresses);
    }

    private static Network ToNetwork(BitcoinNetwork network) => network switch
    {
        BitcoinNetwork.Mainnet => Network.Main,
        BitcoinNetwork.Testnet => Network.TestNet,
        BitcoinNetwork.Regtest => Network.RegTest,
        BitcoinNetwork.Signet => Network.GetNetwork("signet") ?? Network.TestNet,
        _ => Network.Main
    };

    private async Task<Dictionary<string, Transaction>> FetchTransactionsAsync(
        StreamReader reader,
        StreamWriter writer,
        IReadOnlyList<string> txIds,
        Network network,
        CancellationToken token)
    {
        var result = new Dictionary<string, Transaction>();
        for (var i = 0; i < txIds.Count; i += _options.BatchSize)
        {
            var batch = txIds.Skip(i).Take(_options.BatchSize).ToList();
            var requests = batch.Select((txid, idx) => new { jsonrpc = "2.0", method = "blockchain.transaction.get", @params = new[] { txid }, id = i + idx + 1 }).ToList();
            var requestLine = JsonSerializer.Serialize(requests);
            await writer.WriteLineAsync(requestLine.AsMemory(), token);

            var responses = await ReadBatchAsync(reader, batch.Count, token);
            foreach (var resp in responses)
            {
                var id = resp.GetProperty("id").GetInt32();
                var batchIndex = id - i - 1;
                if (batchIndex < 0 || batchIndex >= batch.Count)
                    throw new InvalidOperationException($"Unexpected Electrum response id {id}.");

                if (resp.TryGetProperty("error", out var err) && err.ValueKind != JsonValueKind.Null)
                    throw new InvalidOperationException($"Electrum error: {err}");

                var rawHex = resp.GetProperty("result").GetString()!;
                result[batch[batchIndex]] = Transaction.Parse(rawHex, network);
            }
        }

        return result;
    }

    private async Task<Dictionary<long, DateTimeOffset>> FetchBlockTimestampsAsync(
        StreamReader reader,
        StreamWriter writer,
        IReadOnlyList<long> heights,
        CancellationToken token)
    {
        var result = new Dictionary<long, DateTimeOffset>();
        for (var i = 0; i < heights.Count; i += _options.BatchSize)
        {
            var batch = heights.Skip(i).Take(_options.BatchSize).ToList();
            var requests = batch.Select((h, idx) => new { jsonrpc = "2.0", method = "blockchain.block.header", @params = new object[] { (int)h }, id = i + idx + 1 }).ToList();
            var requestLine = JsonSerializer.Serialize(requests);
            await writer.WriteLineAsync(requestLine.AsMemory(), token);

            var responses = await ReadBatchAsync(reader, batch.Count, token);
            foreach (var resp in responses)
            {
                var id = resp.GetProperty("id").GetInt32();
                var batchIndex = id - i - 1;
                if (batchIndex < 0 || batchIndex >= batch.Count)
                    throw new InvalidOperationException($"Unexpected Electrum response id {id}.");

                if (resp.TryGetProperty("error", out var err) && err.ValueKind != JsonValueKind.Null)
                    throw new InvalidOperationException($"Electrum error: {err}");

                var headerHex = resp.GetProperty("result").GetString()!;
                var bytes = Convert.FromHexString(headerHex);
                if (bytes.Length < 80)
                    throw new InvalidOperationException("Invalid block header received from Electrum server.");

                var unix = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(68, 4));
                result[batch[batchIndex]] = DateTimeOffset.FromUnixTimeSeconds(unix);
            }
        }

        return result;
    }

    private (ParsedDescriptor Parsed, BitcoinNetwork Network) ParseDescriptor(string raw)
    {
        foreach (var network in new[] { BitcoinNetwork.Mainnet, BitcoinNetwork.Testnet })
        {
            try
            {
                return (_parser.Parse(raw, network), network);
            }
            catch (ArgumentException) { }
        }
        throw new ArgumentException($"Could not parse descriptor for Electrum scan: {raw}");
    }

    private static string ToScripthash(string scriptPubKeyHex)
    {
        var bytes = Convert.FromHexString(scriptPubKeyHex);
        var hash = SHA256.HashData(bytes);
        Array.Reverse(hash);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private async Task<Stream> ConnectAsync(CancellationToken token)
    {
        var target = new DnsEndPoint(_options.Host, _options.Port);
        if (!string.IsNullOrWhiteSpace(_options.Socks5Proxy))
        {
            var (proxyHost, proxyPort) = BitcoinCoreRpcClient.ParseSocks5Proxy(_options.Socks5Proxy);
            return await BitcoinCoreRpcClient.ConnectSocks5Async(proxyHost, proxyPort, target, token);
        }

        var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        try
        {
            await socket.ConnectAsync(target.Host, target.Port, token);
            return new NetworkStream(socket, ownsSocket: true);
        }
        catch
        {
            socket.Dispose();
            throw;
        }
    }

    private static async Task HandshakeAsync(StreamWriter writer, StreamReader reader, CancellationToken token)
    {
        const string Request = "{\"jsonrpc\":\"2.0\",\"method\":\"server.version\",\"params\":[\"CofferOS\",\"1.4\"],\"id\":0}\n";
        await writer.WriteAsync(Request.AsMemory(), token);
        await writer.FlushAsync(token);

        var line = await reader.ReadLineAsync(token) ?? throw new IOException("Electrum server closed connection during handshake.");
        using var doc = JsonDocument.Parse(line);
        var root = doc.RootElement;

        if (root.TryGetProperty("error", out var err) && err.ValueKind != JsonValueKind.Null)
            throw new InvalidOperationException($"Electrum handshake failed: {err}");

        if (!root.TryGetProperty("result", out _))
            throw new InvalidOperationException("Electrum handshake did not return a result.");
    }

    private static async Task<List<JsonElement>> ReadBatchAsync(StreamReader reader, int count, CancellationToken token)
    {
        var results = new List<JsonElement>(count);
        while (results.Count < count)
        {
            var line = await reader.ReadLineAsync(token) ?? throw new IOException("Electrum server closed connection unexpectedly.");
            if (string.IsNullOrWhiteSpace(line))
                continue;

            using var doc = JsonDocument.Parse(line);
            if (doc.RootElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in doc.RootElement.EnumerateArray())
                    results.Add(item.Clone());
            }
            else
            {
                results.Add(doc.RootElement.Clone());
            }
        }

        return results;
    }

    public async Task<ElectrumStatusDto> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(_options.TimeoutSeconds));
            var token = cts.Token;
            await using var stream = await ConnectAsync(token);
            var encoding = new UTF8Encoding(false);
            using var writer = new StreamWriter(stream, encoding, bufferSize: 1024, leaveOpen: true) { AutoFlush = true, NewLine = "\n" };
            using var reader = new StreamReader(stream, encoding, detectEncodingFromByteOrderMarks: false, bufferSize: 1024, leaveOpen: true);
            await HandshakeAsync(writer, reader, token);

            const string heightRequest = "{\"jsonrpc\":\"2.0\",\"method\":\"blockchain.headers.subscribe\",\"params\":[],\"id\":1}\n";
            await writer.WriteAsync(heightRequest.AsMemory(), token);
            await writer.FlushAsync(token);

            var line = await reader.ReadLineAsync(token) ?? throw new IOException("No response from Electrum server.");
            using var doc = JsonDocument.Parse(line);
            var root = doc.RootElement;
            if (root.TryGetProperty("error", out var err) && err.ValueKind != JsonValueKind.Null)
                throw new InvalidOperationException($"Electrum error: {err}");
            var result = root.GetProperty("result");
            long? height = result.TryGetProperty("height", out var h) && h.ValueKind == JsonValueKind.Number ? h.GetInt64() : null;

            return new ElectrumStatusDto(true, "electrum", _options.Host, _options.Port, _options.Socks5Proxy, height, null);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to query Electrum server {Host}:{Port}", _options.Host, _options.Port);
            return new ElectrumStatusDto(false, "electrum", _options.Host, _options.Port, _options.Socks5Proxy, null, ex.Message);
        }
    }
}
