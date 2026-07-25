using System.Net.Http.Json;
using CofferOS.Application.Abstractions.Providers;
using Microsoft.Extensions.Logging;

namespace CofferOS.Infrastructure.Providers;

/// <summary>
/// Fetches current BTC price from CoinGecko public API.
/// </summary>
public sealed class CoinGeckoPriceProvider : IBitcoinPriceProvider
{
    private readonly HttpClient _http;
    private readonly ILogger<CoinGeckoPriceProvider> _logger;

    public CoinGeckoPriceProvider(HttpClient http, ILogger<CoinGeckoPriceProvider> logger)
    {
        _http = http;
        _logger = logger;
    }

    public string ProviderId => "coingecko";
    public string DisplayName => "CoinGecko";

    public async Task<decimal?> GetCurrentPriceAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // CoinGecko simple price endpoint: https://api.coingecko.com/api/v3/simple/price?ids=bitcoin&vs_currencies=usd
            var url = "https://api.coingecko.com/api/v3/simple/price?ids=bitcoin&vs_currencies=usd";
            using var resp = await _http.GetAsync(url, cancellationToken);
            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogWarning("CoinGecko returned {Status}", resp.StatusCode);
                return null;
            }

            var data = await resp.Content.ReadFromJsonAsync<CoinGeckoResponse>(cancellationToken);
            if (data?.Bitcoin?.Usd is decimal price && price > 0)
            {
                return price;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch price from CoinGecko");
            return null;
        }
    }

    private sealed record CoinGeckoResponse(BitcoinPrice Bitcoin);
    private sealed record BitcoinPrice(decimal Usd);
}
