using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace CofferOS.Api.WebSockets;

/// <summary>
/// Manages WebSocket connections and broadcasts wallet-related notifications to connected clients.
/// </summary>
public sealed class WalletNotificationHub
{
    private readonly ConcurrentDictionary<string, WebSocketConnection> _connections = new();
    private readonly ILogger<WalletNotificationHub> _logger;

    public WalletNotificationHub(ILogger<WalletNotificationHub> logger)
    {
        _logger = logger;
    }

    public async Task RegisterAsync(string connectionId, WebSocket webSocket)
    {
        var connection = new WebSocketConnection(connectionId, webSocket);
        _connections.TryAdd(connectionId, connection);
        _logger.LogInformation("WebSocket client {ConnectionId} connected; {Count} total connections", connectionId, _connections.Count);
        
        await HandleConnectionAsync(connection);
    }

    public async Task NotifyWalletImportedAsync(Guid walletId, string walletName)
    {
        var notification = new WalletNotification(
            "wallet_imported",
            new { walletId, walletName },
            DateTimeOffset.UtcNow);
        
        await BroadcastAsync(notification);
    }

    public async Task NotifyWalletRescanStartedAsync(Guid walletId)
    {
        var notification = new WalletNotification(
            "wallet_rescan_started",
            new { walletId },
            DateTimeOffset.UtcNow);
        
        await BroadcastAsync(notification);
    }

    public async Task NotifyWalletRescanCompletedAsync(Guid walletId, int utxoCount, long balanceSats)
    {
        var notification = new WalletNotification(
            "wallet_rescan_completed",
            new { walletId, utxoCount, balanceSats },
            DateTimeOffset.UtcNow);
        
        await BroadcastAsync(notification);
    }

    public async Task NotifyWalletRescanFailedAsync(Guid walletId, string error)
    {
        var notification = new WalletNotification(
            "wallet_rescan_failed",
            new { walletId, error },
            DateTimeOffset.UtcNow);
        
        await BroadcastAsync(notification);
    }

    private async Task BroadcastAsync(WalletNotification notification)
    {
        var json = JsonSerializer.Serialize(notification);
        var bytes = Encoding.UTF8.GetBytes(json);

        var deadConnections = new List<string>();

        foreach (var (connectionId, connection) in _connections)
        {
            try
            {
                if (connection.WebSocket.State == WebSocketState.Open)
                {
                    await connection.WebSocket.SendAsync(
                        new ArraySegment<byte>(bytes),
                        WebSocketMessageType.Text,
                        endOfMessage: true,
                        CancellationToken.None);
                }
                else
                {
                    deadConnections.Add(connectionId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send notification to {ConnectionId}", connectionId);
                deadConnections.Add(connectionId);
            }
        }

        foreach (var connectionId in deadConnections)
        {
            _connections.TryRemove(connectionId, out var removed);
            if (removed is not null)
            {
                try
                {
                    await removed.WebSocket.CloseAsync(
                        WebSocketCloseStatus.NormalClosure,
                        "Connection closed",
                        CancellationToken.None);
                    removed.WebSocket.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error closing WebSocket {ConnectionId}", connectionId);
                }
            }
        }
    }

    private async Task HandleConnectionAsync(WebSocketConnection connection)
    {
        var buffer = new byte[1024 * 4];

        try
        {
            while (connection.WebSocket.State == WebSocketState.Open)
            {
                var result = await connection.WebSocket.ReceiveAsync(
                    new ArraySegment<byte>(buffer),
                    CancellationToken.None);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await connection.WebSocket.CloseAsync(
                        WebSocketCloseStatus.NormalClosure,
                        "Closing",
                        CancellationToken.None);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "WebSocket error for {ConnectionId}", connection.Id);
        }
        finally
        {
            _connections.TryRemove(connection.Id, out _);
            connection.WebSocket.Dispose();
            _logger.LogInformation("WebSocket client {ConnectionId} disconnected; {Count} remaining", connection.Id, _connections.Count);
        }
    }

    private sealed class WebSocketConnection
    {
        public string Id { get; }
        public WebSocket WebSocket { get; }

        public WebSocketConnection(string id, WebSocket webSocket)
        {
            Id = id;
            WebSocket = webSocket;
        }
    }
}

public sealed record WalletNotification
{
    [System.Text.Json.Serialization.JsonPropertyName("eventType")]
    public string EventType { get; init; }

    [System.Text.Json.Serialization.JsonPropertyName("data")]
    public object Data { get; init; }

    [System.Text.Json.Serialization.JsonPropertyName("timestamp")]
    public DateTimeOffset Timestamp { get; init; }

    public WalletNotification(string eventType, object data, DateTimeOffset timestamp)
    {
        EventType = eventType;
        Data = data;
        Timestamp = timestamp;
    }
}
