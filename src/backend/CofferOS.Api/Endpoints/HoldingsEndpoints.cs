using CofferOS.Application.Abstractions.Holdings;

namespace CofferOS.Api.Endpoints;

public static class HoldingsEndpoints
{
    public static IEndpointRouteBuilder MapHoldingsEndpoints(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api/holdings");

        api.MapGet("/summary", async (IHoldingsService holdings, CancellationToken ct) =>
                Results.Ok(await holdings.GetSummaryAsync(ct)))
            .WithName("GetHoldingsSummary");

        api.MapGet("/", async (IHoldingsService holdings, CancellationToken ct) =>
                Results.Ok(await holdings.GetHoldingsAsync(ct)))
            .WithName("ListHoldings");

        return app;
    }
}
