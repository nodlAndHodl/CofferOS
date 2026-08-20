using CofferOS.Application.Abstractions.Providers;
using CofferOS.Application.Abstractions.Settings;
using CofferOS.Application.Contracts;

namespace CofferOS.Api.Endpoints;

public static class SettingsEndpoints
{
    public static IEndpointRouteBuilder MapSettingsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/settings");

        group.MapGet("/", async (IUserSettingsService svc, CancellationToken ct) =>
            Results.Ok(await svc.GetAsync(ct)));

        group.MapPut("/", async (
            UpdateUserSettingsRequest request,
            IUserSettingsService svc,
            CancellationToken ct) =>
            Results.Ok(await svc.UpdateAsync(request, ct)));

        group.MapGet("/bitcoin-price", async (
            IUserSettingsService settingsSvc,
            IExchangeRateProvider ratesSvc,
            CancellationToken ct) =>
        {
            var settings = await settingsSvc.GetAsync(ct);
            var rates = ratesSvc.GetCachedRates();
            var priceUsd = rates.TryGetValue("usd", out var usd) ? (decimal?)usd : null;

            return Results.Ok(new
            {
                priceUsd,
                exchangeRates = rates,
                currency = settings.Currency,
            });
        });

        return app;
    }
}
