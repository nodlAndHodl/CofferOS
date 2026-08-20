using System.Net.Http.Json;
using CofferOS.Application.Abstractions.Providers;
using Microsoft.Extensions.Logging;

namespace CofferOS.Infrastructure.Providers;

/// <summary>
/// Fetches current BTC price and multi-currency exchange rates from CoinGecko public API.
/// Implements both IBitcoinPriceProvider (USD price) and IExchangeRateProvider (all rates).
/// </summary>
public sealed class CoinGeckoPriceProvider : IBitcoinPriceProvider, IExchangeRateProvider
{
    private static readonly string[] _currencies = ["usd", "eur", "gbp", "cad", "aud", "chf", "jpy"];
    private static readonly string _vsParam = string.Join(",", _currencies);

    private readonly HttpClient _http;
    private readonly ILogger<CoinGeckoPriceProvider> _logger;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly TimeSpan _cacheTtl = TimeSpan.FromSeconds(60);

    private decimal? _lastPrice;
    private DateTimeOffset? _lastFetched;
    private IReadOnlyDictionary<string, decimal> _exchangeRates = new Dictionary<string, decimal>();

    public CoinGeckoPriceProvider(HttpClient http, ILogger<CoinGeckoPriceProvider> logger)
    {
        _http = http;
        _logger = logger;
    }

    public string ProviderId => "coingecko";
    public string DisplayName => "CoinGecko";
    public DateTimeOffset? LastUpdated => _lastFetched;

    public IReadOnlyDictionary<string, decimal> GetCachedRates() => _exchangeRates;

    public async Task<decimal?> GetCurrentPriceAsync(CancellationToken cancellationToken = default)
    {
        if (IsCacheFresh)
            return _lastPrice;

        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (IsCacheFresh)
                return _lastPrice;

            var url = $"https://api.coingecko.com/api/v3/simple/price?ids=bitcoin&vs_currencies={_vsParam}";
            try
            {
                using var resp = await _http.GetAsync(url, cancellationToken);
                if (resp.IsSuccessStatusCode)
                {
                    var data = await resp.Content.ReadFromJsonAsync<CoinGeckoResponse>(cancellationToken);
                    var rates = data?.bitcoin;
                    if (rates != null && rates.usd > 0)
                    {
                        _lastPrice = rates.usd;
                        _lastFetched = DateTimeOffset.UtcNow;
                        _exchangeRates = new Dictionary<string, decimal>
                        {
                            ["usd"] = rates.usd,
                            ["eur"] = rates.eur,
                            ["gbp"] = rates.gbp,
                            ["cad"] = rates.cad,
                            ["aud"] = rates.aud,
                            ["chf"] = rates.chf,
                            ["jpy"] = rates.jpy,
                        };
                        return rates.usd;
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

    private sealed record CoinGeckoResponse(BitcoinRates bitcoin);
    private sealed record BitcoinRates(
        decimal usd,
        decimal eur,
        decimal gbp,
        decimal cad,
        decimal aud,
        decimal chf,
        decimal jpy
    );
}
