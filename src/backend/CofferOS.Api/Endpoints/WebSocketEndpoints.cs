using CofferOS.Api.WebSockets;

namespace CofferOS.Api.Endpoints;

public static class WebSocketEndpoints
{
    public static IEndpointRouteBuilder MapWebSocketEndpoints(this IEndpointRouteBuilder app)
    {
        app.Map("/ws/notifications", async (HttpContext context, NotificationHub hub) =>
        {
            if (context.WebSockets.IsWebSocketRequest)
            {
                var webSocket = await context.WebSockets.AcceptWebSocketAsync();
                var connectionId = Guid.NewGuid().ToString();
                await hub.RegisterAsync(connectionId, webSocket);
            }
            else
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
            }
        })
        .WithName("RealtimeNotifications");

        return app;
    }
}
