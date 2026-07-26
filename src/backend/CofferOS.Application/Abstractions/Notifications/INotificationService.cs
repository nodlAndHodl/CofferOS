namespace CofferOS.Application.Abstractions.Notifications;

/// <summary>
/// Generic service for broadcasting real-time notifications to connected clients.
/// Supports any domain event type.
/// </summary>
public interface INotificationService
{
    /// <summary>Broadcast a notification event to all connected clients.</summary>
    Task BroadcastAsync(string eventType, object data, CancellationToken cancellationToken = default);

    /// <summary>Broadcast a typed notification event to all connected clients.</summary>
    Task BroadcastAsync<T>(string eventType, T data, CancellationToken cancellationToken = default) where T : class;
}
