using CofferOS.Application.CostBasis;
using CofferOS.Domain.Common;

namespace CofferOS.Api.Endpoints;

public static class CostBasisEndpoints
{
    public static IEndpointRouteBuilder MapCostBasisEndpoints(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api");

        // PUT /api/cost-basis/{target}/{reference}
        api.MapPut("/cost-basis/{target}/{reference:required}", async (
            string target,
            string reference,
            SetCostBasisRequest request,
            CostBasisService service,
            CancellationToken ct) =>
        {
            if (!Enum.TryParse<CostBasisTarget>(target, true, out var targetEnum))
                return Results.BadRequest(new { error = "Invalid target." });

            if (request.Amount < 0)
                return Results.BadRequest(new { error = "Amount cannot be negative." });

            await service.SetAsync(targetEnum, reference, request.Amount, ct);
            return Results.NoContent();
        }).WithName("SetCostBasis");

        // DELETE /api/cost-basis/{target}/{reference}
        api.MapDelete("/cost-basis/{target}/{reference:required}", async (
            string target,
            string reference,
            CostBasisService service,
            CancellationToken ct) =>
        {
            if (!Enum.TryParse<CostBasisTarget>(target, true, out var targetEnum))
                return Results.BadRequest(new { error = "Invalid target." });

            await service.ClearAsync(targetEnum, reference, ct);
            return Results.NoContent();
        }).WithName("ClearCostBasis");

        return app;
    }
}

public sealed record SetCostBasisRequest(decimal Amount);
