# Real-Time Notifications System

CofferOS uses WebSocket-based real-time notifications to keep clients updated about domain events. The system is extensible and supports any event type.

## Architecture

### Backend Components

1. **NotificationHub** (`CofferOS.Api/WebSockets/NotificationHub.cs`)
   - Manages WebSocket connections
   - Broadcasts events to all connected clients
   - Handles connection lifecycle

2. **INotificationService** (Generic)
   - Core abstraction for broadcasting events
   - Decouples application logic from WebSocket implementation
   - Supports both typed and untyped broadcasts

3. **Domain-Specific Services** (e.g., `IWalletNotificationService`, `ILoanNotificationService`)
   - Implement domain-specific event types
   - Use `INotificationService` internally
   - Provide strongly-typed methods for each event

### Frontend Components

1. **useNotifications Hook** (`src/hooks/useWalletNotifications.ts`)
   - Manages WebSocket connection
   - Parses incoming events
   - Auto-reconnects on disconnect
   - Supports both generic and specific event handlers

## Event Structure

All notifications follow this structure:

```json
{
  "eventType": "wallet_rescan_completed",
  "data": {
    "walletId": "123e4567-e89b-12d3-a456-426614174000",
    "utxoCount": 42,
    "balanceSats": 1000000
  },
  "timestamp": "2026-07-26T07:35:00Z"
}
```

## Adding a New Event Type

### 1. Create a Notification Service Interface

```csharp
// CofferOS.Application/Abstractions/Notifications/IMyFeatureNotificationService.cs
public interface IMyFeatureNotificationService
{
    Task NotifyMyEventAsync(Guid id, string name, CancellationToken cancellationToken = default);
}
```

### 2. Implement the Service

```csharp
// CofferOS.Api/WebSockets/MyFeatureNotificationService.cs
public sealed class MyFeatureNotificationService : IMyFeatureNotificationService
{
    private readonly INotificationService _notificationService;

    public MyFeatureNotificationService(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    public Task NotifyMyEventAsync(Guid id, string name, CancellationToken cancellationToken = default)
    {
        return _notificationService.BroadcastAsync("my_event", new { id, name }, cancellationToken);
    }
}
```

### 3. Register in Program.cs

```csharp
builder.Services.AddScoped<IMyFeatureNotificationService, MyFeatureNotificationService>();
```

### 4. Use in Your Application Logic

```csharp
public class MyService
{
    private readonly IMyFeatureNotificationService _notifications;

    public MyService(IMyFeatureNotificationService notifications)
    {
        _notifications = notifications;
    }

    public async Task DoSomethingAsync(Guid id, string name)
    {
        // ... do work ...
        await _notifications.NotifyMyEventAsync(id, name);
    }
}
```

### 5. Handle on Frontend

```typescript
import { useNotifications } from '../hooks/useWalletNotifications';

export function MyComponent() {
  useNotifications({
    onEvent: (notification) => {
      if (notification.eventType === 'my_event') {
        console.log('My event received:', notification.data);
      }
    },
  });
}
```

## Current Event Types

### Wallet Events
- `wallet_imported` - Wallet has been imported
- `wallet_rescan_started` - Rescan operation started
- `wallet_rescan_completed` - Rescan operation finished
- `wallet_rescan_failed` - Rescan operation failed

### Loan Events (Example)
- `loan_created` - New loan created
- `loan_updated` - Loan details updated
- `loan_deleted` - Loan deleted
- `loan_payment_recorded` - Payment recorded
- `loan_liquidation_warning` - LTV warning threshold reached

## Connection Details

- **Endpoint**: `/ws/notifications`
- **Protocol**: WebSocket (ws:// or wss://)
- **Auto-reconnect**: Yes (3 second delay)
- **Message Format**: JSON

## Error Handling

The notification system gracefully handles:
- Network disconnections (auto-reconnect)
- Invalid JSON (logged, connection continues)
- Closed connections (cleanup and removal)
- Missing handlers (no-op, logged as warning)

## Performance Considerations

- Notifications are broadcast to all connected clients
- Each client receives all events (filtering happens on client-side)
- No persistence (events are lost if client is disconnected)
- Suitable for real-time UI updates, not for audit logs
