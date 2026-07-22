using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace CofferOS.Integrations.BitcoinCore;

/// <summary>
/// Minimal Bitcoin Core JSON-RPC client built on HttpClient + System.Text.Json.
/// Supports optional SOCKS5 proxy for .onion/Tor RPC endpoints.
/// </summary>
public sealed class BitcoinCoreRpcClient : IDisposable
{
    private readonly HttpClient _http;
    private readonly BitcoinCoreOptions _options;

    public BitcoinCoreRpcClient(IOptions<BitcoinCoreOptions> options)
    {
        _options = options.Value;

        var handler = new SocketsHttpHandler();
        if (!string.IsNullOrWhiteSpace(_options.Socks5Proxy))
        {
            var (proxyHost, proxyPort) = ParseSocks5Proxy(_options.Socks5Proxy);
            handler.ConnectCallback = (ctx, token) => ConnectSocks5Async(proxyHost, proxyPort, ctx.DnsEndPoint, token);
        }

        _http = new HttpClient(handler);
        _http.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);

        if (!string.IsNullOrEmpty(_options.RpcUser))
        {
            var raw = $"{_options.RpcUser}:{_options.RpcPassword}";
            var token = Convert.ToBase64String(Encoding.UTF8.GetBytes(raw));
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", token);
        }
    }

    private Uri Endpoint
    {
        get
        {
            var baseUrl = _options.RpcUrl.TrimEnd('/');
            return string.IsNullOrEmpty(_options.WalletName)
                ? new Uri(baseUrl)
                : new Uri($"{baseUrl}/wallet/{_options.WalletName}");
        }
    }

    public async Task<JsonElement> CallAsync(string method, object?[]? parameters = null, CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            jsonrpc = "1.0",
            id = "cofferos",
            method,
            @params = parameters ?? Array.Empty<object?>()
        };

        using var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        using var response = await _http.PostAsync(Endpoint, content, cancellationToken);

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        if (root.TryGetProperty("error", out var error) && error.ValueKind != JsonValueKind.Null)
        {
            var message = error.TryGetProperty("message", out var m) ? m.GetString() : error.ToString();
            throw new BitcoinCoreRpcException($"RPC '{method}' failed: {message}");
        }

        // Clone so the value survives disposal of the JsonDocument.
        return root.GetProperty("result").Clone();
    }

    public void Dispose() => _http.Dispose();

    internal static (string Host, int Port) ParseSocks5Proxy(string value)
    {
        var trimmed = value.Trim();
        var parts = trimmed.Split(':', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2 || !int.TryParse(parts[1], out var port))
            throw new InvalidOperationException("Socks5Proxy must be 'host:port'.");
        return (parts[0], port);
    }

    internal static async ValueTask<Stream> ConnectSocks5Async(string proxyHost, int proxyPort, DnsEndPoint target, CancellationToken token)
    {
        var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        try
        {
            await socket.ConnectAsync(proxyHost, proxyPort, token);

            await socket.SendAsync(new byte[] { 0x05, 0x01, 0x00 }, SocketFlags.None, token);
            var auth = new byte[2];
            await ReceiveExactAsync(socket, auth, token);
            if (auth[0] != 0x05 || auth[1] != 0x00)
                throw new HttpRequestException("SOCKS5 authentication failed.");

            var host = Encoding.UTF8.GetBytes(target.Host);
            if (host.Length > 255)
                throw new HttpRequestException("SOCKS5 target host too long.");

            var request = new byte[7 + host.Length];
            request[0] = 0x05;
            request[1] = 0x01; // CONNECT
            request[2] = 0x00;
            request[3] = 0x03; // domain name
            request[4] = (byte)host.Length;
            host.CopyTo(request, 5);
            request[5 + host.Length] = (byte)(target.Port >> 8);
            request[6 + host.Length] = (byte)(target.Port & 0xFF);
            await socket.SendAsync(request, SocketFlags.None, token);

            var header = new byte[4];
            await ReceiveExactAsync(socket, header, token);
            if (header[0] != 0x05 || header[1] != 0x00)
                throw new HttpRequestException($"SOCKS5 CONNECT failed: {header[1]}");

            var bindLength = header[3] switch
            {
                0x01 => 4 + 2,
                0x03 => (await ReadByteAsync(socket, token)) + 2,
                0x04 => 16 + 2,
                _ => throw new HttpRequestException("SOCKS5 unknown address type.")
            };
            var bind = new byte[bindLength];
            await ReceiveExactAsync(socket, bind, token);

            return new NetworkStream(socket, ownsSocket: true);
        }
        catch
        {
            socket.Dispose();
            throw;
        }
    }

    internal static async ValueTask<int> ReadByteAsync(Socket socket, CancellationToken token)
    {
        var buffer = new byte[1];
        await ReceiveExactAsync(socket, buffer, token);
        return buffer[0];
    }

    internal static async ValueTask ReceiveExactAsync(Socket socket, Memory<byte> buffer, CancellationToken token)
    {
        var total = 0;
        while (total < buffer.Length)
        {
            var read = await socket.ReceiveAsync(buffer[total..], SocketFlags.None, token);
            if (read == 0)
                throw new HttpRequestException("SOCKS5 connection closed unexpectedly.");
            total += read;
        }
    }
}

public sealed class BitcoinCoreRpcException : Exception
{
    public BitcoinCoreRpcException(string message) : base(message) { }
}
