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
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly TimeSpan _cacheTtl = TimeSpan.FromSeconds(60);

    private decimal? _lastPrice;
    private DateTimeOffset? _lastFetched;

    public CoinGeckoPriceProvider(HttpClient http, ILogger<CoinGeckoPriceProvider> logger)
    {
        _http = http;
        _logger = logger;
    }

    public string ProviderId => "coingecko";
    public string DisplayName => "CoinGecko";
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

            var url = "https://api.coingecko.com/api/v3/simple/price?ids=bitcoin&vs_currencies=usd";
            try
            {
                using var resp = await _http.GetAsync(url, cancellationToken);
                if (resp.IsSuccessStatusCode)
                {
                    var data = await resp.Content.ReadFromJsonAsync<CoinGeckoResponse>(cancellationToken);
                    if (data?.Bitcoin?.Usd is decimal price && price > 0)
                    {
                        _lastPrice = price;
                        _lastFetched = DateTimeOffset.UtcNow;
                        return price;
                    }
                }
                else
                {
                    _logger.LogWarning("CoinGecko returned {Status}", resp.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch price from CoinGecko");
            }

            if (_lastPrice is not null)
            {
                _logger.LogWarning("Returning stale cached CoinGecko price");
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

    private sealed record CoinGeckoResponse(BitcoinPrice Bitcoin);
    private sealed record BitcoinPrice(decimal Usd);
}
