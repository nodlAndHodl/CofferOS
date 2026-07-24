using CofferOS.Application.Abstractions.Persistence;
using CofferOS.Application.Contracts;
using CofferOS.Application.Wallets;
using CofferOS.Domain.Common;

namespace CofferOS.Api.Endpoints;

public static class MetadataEndpoints
{
    public static IEndpointRouteBuilder MapMetadataEndpoints(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api");
        var wallets = api.MapGroup("/wallets");

        // GET /api/wallets/{walletId}/objects/{target}/{reference}/metadata
        wallets.MapGet("/{walletId:guid}/objects/{target}/{reference}/metadata", async (
            Guid walletId,
            string target,
            string reference,
            TransactionMetadataService service,
            CancellationToken ct) =>
        {
            if (!Enum.TryParse<LabelTarget>(target, true, out var targetEnum))
                return Results.BadRequest(new { error = "Invalid target." });

            var dto = await service.GetForObjectAsync(walletId, targetEnum, reference, ct);
            return Results.Ok(dto);
        }).WithName("GetObjectMetadata");

        // PUT /api/wallets/{walletId}/objects/{target}/{reference}/metadata
        wallets.MapPut("/{walletId:guid}/objects/{target}/{reference}/metadata", async (
            Guid walletId,
            string target,
            string reference,
            UpdateMetadataRequest request,
            TransactionMetadataService service,
            CancellationToken ct) =>
        {
            if (!Enum.TryParse<LabelTarget>(target, true, out var targetEnum))
                return Results.BadRequest(new { error = "Invalid target." });

            await service.UpdateForObjectAsync(walletId, targetEnum, reference, request, ct);
            return Results.NoContent();
        }).WithName("UpdateObjectMetadata");

        return app;
    }
}
