using System.Net.Http.Json;
using CofferOS.Application.Abstractions.Providers;
using Microsoft.Extensions.Logging;

namespace CofferOS.Infrastructure.Providers;

/// <summary>
/// Fetches current BTC price from Coinbase public API.
/// </summary>
public sealed class CoinbasePriceProvider : IBitcoinPriceProvider
{
    private readonly HttpClient _http;
    private readonly ILogger<CoinbasePriceProvider> _logger;

    public CoinbasePriceProvider(HttpClient http, ILogger<CoinbasePriceProvider> logger)
    {
        _http = http;
        _logger = logger;
    }

    public string ProviderId => "coinbase";
    public string DisplayName => "Coinbase";

    public async Task<decimal?> GetCurrentPriceAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Coinbase: https://api.coinbase.com/v2/exchange-rates?currency=BTC
            var url = "https://api.coinbase.com/v2/exchange-rates?currency=BTC";
            using var resp = await _http.GetAsync(url, cancellationToken);
            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogWarning("Coinbase returned {Status}", resp.StatusCode);
                return null;
            }

            var data = await resp.Content.ReadFromJsonAsync<CoinbaseResponse>(cancellationToken);
            if (data?.Data?.Rates?.Usd is string usdStr &&
                decimal.TryParse(usdStr, out var price) && price > 0)
            {
                return price;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch price from Coinbase");
            return null;
        }
    }

    private sealed record CoinbaseResponse(CoinbaseData Data);
    private sealed record CoinbaseData(CoinbaseRates Rates);
    private sealed record CoinbaseRates(string Usd);
}
