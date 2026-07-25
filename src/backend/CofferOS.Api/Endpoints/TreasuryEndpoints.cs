using CofferOS.Application.Abstractions.Persistence;
using CofferOS.Application.Abstractions.Providers;
using CofferOS.Application.Contracts;
using CofferOS.Application.Treasury;

namespace CofferOS.Api.Endpoints;

public static class TreasuryEndpoints
{
    public static IEndpointRouteBuilder MapTreasuryEndpoints(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api");

        // Treasury summary (dashboard widget)
        api.MapGet("/treasury/summary", async (TreasuryService treasury, CancellationToken ct) =>
                Results.Ok(await treasury.GetTreasurySummaryAsync(ct)))
            .WithName("GetTreasurySummary");

        // Loans collection
        api.MapGet("/loans", async (TreasuryService treasury, CancellationToken ct) =>
                Results.Ok(await treasury.GetSummariesAsync(ct)))
            .WithName("ListLoans");

        api.MapGet("/loans/{id:guid}", async (Guid id, TreasuryService treasury, CancellationToken ct) =>
            {
                var detail = await treasury.GetDetailAsync(id, ct);
                return detail is null ? Results.NotFound() : Results.Ok(detail);
            })
            .WithName("GetLoan");

        api.MapPost("/loans", async (CreateLoanRequest request, TreasuryService treasury, CancellationToken ct) =>
            {
                try
                {
                    var created = await treasury.CreateAsync(request, ct);
                    return Results.Created($"/api/loans/{created.Id}", created);
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(new { error = ex.Message });
                }
            })
            .WithName("CreateLoan");

        api.MapPut("/loans/{id:guid}", async (Guid id, UpdateLoanRequest request, TreasuryService treasury, CancellationToken ct) =>
            {
                try
                {
                    var updated = await treasury.UpdateAsync(id, request, ct);
                    return updated is null ? Results.NotFound() : Results.Ok(updated);
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(new { error = ex.Message });
                }
            })
            .WithName("UpdateLoan");

        api.MapDelete("/loans/{id:guid}", async (Guid id, TreasuryService treasury, CancellationToken ct) =>
            {
                var deleted = await treasury.DeleteAsync(id, ct);
                return deleted ? Results.NoContent() : Results.NotFound();
            })
            .WithName("DeleteLoan");

        // Partial updates
        api.MapPut("/loans/{id:guid}/balance", async (Guid id, UpdateLoanBalanceRequest request, TreasuryService treasury, CancellationToken ct) =>
            {
                try
                {
                    var updated = await treasury.UpdateBalanceAsync(id, request, ct);
                    return updated is null ? Results.NotFound() : Results.Ok(updated);
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(new { error = ex.Message });
                }
            })
            .WithName("UpdateLoanBalance");

        api.MapPut("/loans/{id:guid}/collateral", async (Guid id, UpdateLoanCollateralRequest request, TreasuryService treasury, CancellationToken ct) =>
            {
                try
                {
                    var updated = await treasury.UpdateCollateralAsync(id, request, ct);
                    return updated is null ? Results.NotFound() : Results.Ok(updated);
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(new { error = ex.Message });
                }
            })
            .WithName("UpdateLoanCollateral");

        // Manual BTC price (Phase 1)
        api.MapPost("/price", async (SetBtcPriceRequest request, TreasuryService treasury, CancellationToken ct) =>
            {
                try
                {
                    await treasury.SetBtcPriceAsync(request, ct);
                    return Results.Ok(new { price = request.Price });
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(new { error = ex.Message });
                }
            })
            .WithName("SetBtcPrice");

        api.MapGet("/price", async (IBitcoinPriceProvider priceProvider, IBitcoinPriceHistoryRepository historyRepo, CancellationToken ct) =>
            {
                var price = await priceProvider.GetCurrentPriceAsync(ct);
                var latest = await historyRepo.GetLatestAsync(ct);

                // Determine a simple source note for the UI/dashboard
                string? note = null;
                if (priceProvider.ProviderId == "manual")
                {
                    note = "Using manually configured Bitcoin price.";
                }
                else if (latest is not null && latest.Provider != priceProvider.ProviderId)
                {
                    note = "Using cached Bitcoin price.";
                }

                return Results.Ok(new
                {
                    price,
                    providerId = priceProvider.ProviderId,
                    displayName = priceProvider.DisplayName,
                    lastUpdated = latest?.Timestamp,
                    note
                });
            })
            .WithName("GetBtcPrice");

        return app;
    }
}
