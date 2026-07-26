using CofferOS.Application.Abstractions.Notifications;

namespace CofferOS.Api.WebSockets;

/// <summary>Implementation of generic notifications using WebSocket hub.</summary>
public sealed class NotificationService : INotificationService
{
    private readonly NotificationHub _hub;

    public NotificationService(NotificationHub hub)
    {
        _hub = hub;
    }

    public Task BroadcastAsync(string eventType, object data, CancellationToken cancellationToken = default)
    {
        return _hub.BroadcastAsync(eventType, data);
    }

    public Task BroadcastAsync<T>(string eventType, T data, CancellationToken cancellationToken = default) where T : class
    {
        return _hub.BroadcastAsync(eventType, data);
    }
}
