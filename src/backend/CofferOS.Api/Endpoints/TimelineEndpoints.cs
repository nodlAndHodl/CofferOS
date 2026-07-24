using CofferOS.Application.Abstractions.Persistence;
using CofferOS.Application.Contracts;
using CofferOS.Application.Wallets;
using CofferOS.Domain.Common;
using CofferOS.Domain.Wallets;

namespace CofferOS.Api.Endpoints;

public static class TimelineEndpoints
{
    public static IEndpointRouteBuilder MapTimelineEndpoints(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api");
        var wallets = api.MapGroup("/wallets");

        wallets.MapGet("/{walletId:guid}/timeline", async (Guid walletId, WalletTimelineService service, CancellationToken ct) =>
        {
            try
            {
                var timeline = await service.GetTimelineAsync(walletId, ct);
                return Results.Ok(timeline);
            }
            catch (InvalidOperationException ex)
            {
                return Results.NotFound(new { error = ex.Message });
            }
        }).WithName("GetWalletTimeline");

        wallets.MapPost("/{walletId:guid}/timeline", async (
            Guid walletId,
            CreateTimelineEventRequest request,
            IWalletRepository wallets,
            ITimelineEventRepository events,
            IUnitOfWork unitOfWork,
            CancellationToken ct) =>
        {
            if (!Enum.TryParse<TimelineEventType>(request.Type ?? "Annotation", true, out var type))
                return Results.BadRequest(new { error = "Invalid event type." });

            var wallet = await wallets.GetByIdAsync(walletId, ct);
            if (wallet is null) return Results.NotFound();

            var timelineEvent = new TimelineEvent(
                walletId,
                type,
                request.OccurredAt,
                request.Title,
                request.Description,
                request.Reference);

            await events.AddAsync(timelineEvent, ct);
            await unitOfWork.SaveChangesAsync(ct);

            return Results.Created(
                $"/api/wallets/{walletId}/timeline/{timelineEvent.Id}",
                new TimelineEventDto(
                    timelineEvent.Id,
                    timelineEvent.Type.ToString(),
                    timelineEvent.OccurredAt,
                    timelineEvent.Title,
                    timelineEvent.Description,
                    timelineEvent.Reference,
                    timelineEvent.CreatedAt,
                    timelineEvent.UpdatedAt));
        }).WithName("CreateTimelineEvent");

        api.MapPut("/timeline-events/{id:guid}", async (
            Guid id,
            UpdateTimelineEventRequest request,
            ITimelineEventRepository events,
            IUnitOfWork unitOfWork,
            CancellationToken ct) =>
        {
            var timelineEvent = await events.GetByIdAsync(id, ct);
            if (timelineEvent is null) return Results.NotFound();

            timelineEvent.Update(request.OccurredAt, request.Title, request.Description, request.Reference);
            await unitOfWork.SaveChangesAsync(ct);

            return Results.Ok(new TimelineEventDto(
                timelineEvent.Id,
                timelineEvent.Type.ToString(),
                timelineEvent.OccurredAt,
                timelineEvent.Title,
                timelineEvent.Description,
                timelineEvent.Reference,
                timelineEvent.CreatedAt,
                timelineEvent.UpdatedAt));
        }).WithName("UpdateTimelineEvent");

        api.MapDelete("/timeline-events/{id:guid}", async (
            Guid id,
            ITimelineEventRepository events,
            IUnitOfWork unitOfWork,
            CancellationToken ct) =>
        {
            var timelineEvent = await events.GetByIdAsync(id, ct);
            if (timelineEvent is null) return Results.NotFound();

            events.Remove(timelineEvent);
            await unitOfWork.SaveChangesAsync(ct);
            return Results.NoContent();
        }).WithName("DeleteTimelineEvent");

        return app;
    }
}
