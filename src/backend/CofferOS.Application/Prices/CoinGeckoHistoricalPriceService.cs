using System.Net.Http.Json;
using Microsoft.Extensions.Logging;

namespace CofferOS.Application.Prices;

/// <summary>
/// Fetches historical daily Bitcoin prices from CoinGecko.
/// Uses the /api/v3/coins/bitcoin/market_chart/range endpoint for arbitrary date ranges.
/// </summary>
public sealed class CoinGeckoHistoricalPriceService
{
    private readonly HttpClient _http;
    private readonly ILogger<CoinGeckoHistoricalPriceService> _logger;

    public CoinGeckoHistoricalPriceService(HttpClient http, ILogger<CoinGeckoHistoricalPriceService> logger)
    {
        _http = http;
        _logger = logger;
    }

    /// <summary>
    /// Fetches daily BTC prices from startDate to endDate (inclusive).
    /// Returns one point per day, sorted by date ascending.
    /// </summary>
    public async Task<List<(DateTimeOffset Date, decimal Price)>> GetDailyPricesAsync(
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        CancellationToken cancellationToken = default)
    {
        if (endDate < startDate)
            throw new ArgumentException("End date must be >= start date.", nameof(endDate));

        var result = new List<(DateTimeOffset, decimal)>();

        try
        {
            var startUnix = (long)startDate.ToUnixTimeSeconds();
            var endUnix = (long)endDate.ToUnixTimeSeconds();

            // market_chart/range is designed for arbitrary date windows, one API call only.
            var url = $"https://api.coingecko.com/api/v3/coins/bitcoin/market_chart/range?vs_currency=usd&from={startUnix}&to={endUnix}";

            using var resp = await _http.GetAsync(url, cancellationToken);
            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogWarning("CoinGecko returned {Status} for historical range", resp.StatusCode);
                return result;
            }

            var data = await resp.Content.ReadFromJsonAsync<CoinGeckoMarketChartResponse>(cancellationToken);
            if (data?.Prices == null || data.Prices.Count == 0)
            {
                _logger.LogWarning("CoinGecko returned no price data");
                return result;
            }

            // Downsample to one sample per UTC day, preferring the last sample of each day.
            var byDay = new Dictionary<DateTimeOffset, double>();
            foreach (var pricePoint in data.Prices)
            {
                var timestamp = DateTimeOffset.FromUnixTimeMilliseconds((long)pricePoint[0]);
                var day = new DateTimeOffset(timestamp.Year, timestamp.Month, timestamp.Day, 0, 0, 0, TimeSpan.Zero);
                byDay[day] = pricePoint[1];
            }

            foreach (var kvp in byDay.OrderBy(x => x.Key))
            {
                if (kvp.Key >= startDate.Date && kvp.Key <= endDate.Date)
                {
                    result.Add((kvp.Key, (decimal)kvp.Value));
                }
            }

            _logger.LogInformation("Fetched {Count} daily prices from CoinGecko for range {Start:s} to {End:s}",
                result.Count, startDate, endDate);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch historical prices from CoinGecko");
        }

        return result;
    }

    private sealed record CoinGeckoMarketChartResponse(List<List<double>> Prices);
}
