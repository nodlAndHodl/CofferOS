using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace CofferOS.Api.WebSockets;

/// <summary>
/// Manages WebSocket connections and broadcasts real-time notifications to connected clients.
/// Supports any domain event type with a flexible event structure.
/// </summary>
public sealed class NotificationHub
{
    private readonly ConcurrentDictionary<string, WebSocketConnection> _connections = new();
    private readonly ILogger<NotificationHub> _logger;

    public NotificationHub(ILogger<NotificationHub> logger)
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

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task BroadcastAsync(string eventType, object data)
    {
        var notification = new DomainEventNotification(
            eventType,
            data,
            DateTimeOffset.UtcNow);
        
        _logger.LogInformation("Broadcasting event {EventType} to {ConnectionCount} connected clients", eventType, _connections.Count);
        await BroadcastInternalAsync(notification);
    }

    public async Task BroadcastAsync<T>(string eventType, T data) where T : class
    {
        await BroadcastAsync(eventType, (object)data);
    }

    private async Task BroadcastInternalAsync(DomainEventNotification notification)
    {
        // Explicit shape with camelCase keys to match frontend DomainEventNotification contract.
        // Using an anonymous object here ensures the wire format is "eventType"/"data"/"timestamp"
        // regardless of record constructor parameter naming or serializer policy behavior.
        var payload = new
        {
            eventType = notification.EventType,
            data = notification.Data,
            timestamp = notification.Timestamp
        };
        var json = JsonSerializer.Serialize(payload, _jsonOptions);
        var bytes = Encoding.UTF8.GetBytes(json);

        var deadConnections = new List<string>();
        var sentCount = 0;

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
                    sentCount++;
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
        
        _logger.LogInformation("Sent {EventType} notification to {SentCount} clients", notification.EventType, sentCount);

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

/// <summary>Generic notification structure for all domain events.</summary>
public sealed record DomainEventNotification
{
    [JsonPropertyName("eventType")]
    public string EventType { get; init; }

    [JsonPropertyName("data")]
    public object Data { get; init; }

    [JsonPropertyName("timestamp")]
    public DateTimeOffset Timestamp { get; init; }

    public DomainEventNotification(string eventType, object data, DateTimeOffset timestamp)
    {
        EventType = eventType;
        Data = data;
        Timestamp = timestamp;
    }
}
