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
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly TimeSpan _cacheTtl = TimeSpan.FromSeconds(60);

    private decimal? _lastPrice;
    private DateTimeOffset? _lastFetched;

    public CoinbasePriceProvider(HttpClient http, ILogger<CoinbasePriceProvider> logger)
    {
        _http = http;
        _logger = logger;
    }

    public string ProviderId => "coinbase";
    public string DisplayName => "Coinbase";
    public DateTimeOffset? LastUpdated => _lastFetched;

    public async Task<decimal?> GetCurrentPriceAsync(CancellationToken cancellationToken = default)
    {
        if (IsCacheFresh)
            return _lastPrice;

        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (IsCacheFresh)
                return _lastPrice;

            var url = "https://api.coinbase.com/v2/exchange-rates?currency=BTC";
            try
            {
                using var resp = await _http.GetAsync(url, cancellationToken);
                if (resp.IsSuccessStatusCode)
                {
                    var data = await resp.Content.ReadFromJsonAsync<CoinbaseResponse>(cancellationToken);
                    if (data?.Data?.Rates?.Usd is string usdStr &&
                        decimal.TryParse(usdStr, out var price) && price > 0)
                    {
                        _lastPrice = price;
                        _lastFetched = DateTimeOffset.UtcNow;
                        return price;
                    }
                }
                else
                {
                    _logger.LogWarning("Coinbase returned {Status}", resp.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch price from Coinbase");
            }

            if (_lastPrice is not null)
            {
                _logger.LogWarning("Returning stale cached Coinbase price");
                return _lastPrice;
            }

            return null;
        }
        finally
        {
            _lock.Release();
        }
    }

    private bool IsCacheFresh => _lastFetched is not null &&
                                 DateTimeOffset.UtcNow - _lastFetched < _cacheTtl;

    private sealed record CoinbaseResponse(CoinbaseData Data);
    private sealed record CoinbaseData(CoinbaseRates Rates);
    private sealed record CoinbaseRates(string Usd);
}
