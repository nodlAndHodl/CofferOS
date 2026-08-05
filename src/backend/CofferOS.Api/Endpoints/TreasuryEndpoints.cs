using CofferOS.Application.Abstractions.Dashboard;
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

        // Dashboard overview (complete treasury state)
        api.MapGet("/dashboard/overview", async (IDashboardQueryService dashboard, CancellationToken ct) =>
                Results.Ok(await dashboard.GetOverviewAsync(ct)))
            .WithName("GetDashboardOverview");

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

        api.MapGet("/price", async (IBitcoinPriceProvider priceProvider, CancellationToken ct) =>
            {
                var price = await priceProvider.GetCurrentPriceAsync(ct);

                string? note = null;
                if (priceProvider.ProviderId == "manual")
                {
                    note = "Using manually configured Bitcoin price.";
                }
                else if (price is not null)
                {
                    note = $"Using live Bitcoin price from {priceProvider.DisplayName}.";
                }
                else
                {
                    note = "Bitcoin price is currently unavailable.";
                }

                return Results.Ok(new
                {
                    price,
                    providerId = priceProvider.ProviderId,
                    displayName = priceProvider.DisplayName,
                    lastUpdated = priceProvider.LastUpdated,
                    note
                });
            })
            .WithName("GetBtcPrice");

        api.MapGet("/loans/{id:guid}/historical", async (Guid id, TreasuryService treasury, CancellationToken ct) =>
            {
                try
                {
                    var data = await treasury.GetHistoricalDataAsync(id, ct);
                    return Results.Ok(data);
                }
                catch (ArgumentException ex)
                {
                    return Results.NotFound(new { error = ex.Message });
                }
            })
            .WithName("GetLoanHistoricalData");

        return app;
    }
}
