using System.Net.Http.Json;
using Microsoft.Extensions.Logging;

namespace CofferOS.Application.Prices;

/// <summary>
/// Fetches historical daily Bitcoin prices from Coinbase public candle API.
/// Used for bulk historical data because CoinGecko's free tier does not support arbitrary date ranges.
/// Coinbase candles are returned as [timestamp, low, high, open, close] in seconds granularity.
/// </summary>
public sealed class CoinbaseHistoricalPriceService
{
    private readonly HttpClient _http;
    private readonly ILogger<CoinbaseHistoricalPriceService> _logger;

    // Coinbase allows max 300 candles per request for 86400s (1 day) granularity.
    private const int MaxCandlesPerRequest = 300;
    private const int GranularitySeconds = 86400;

    public CoinbaseHistoricalPriceService(HttpClient http, ILogger<CoinbaseHistoricalPriceService> logger)
    {
        _http = http;
        _logger = logger;
    }

    /// <summary>
    /// Fetches daily BTC close prices from startDate to endDate (inclusive).
    /// Handles Coinbase's 300-candle pagination internally.
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
        var currentStart = startDate.UtcDateTime;
        var absoluteEnd = endDate.UtcDateTime;

        try
        {
            while (currentStart < absoluteEnd)
            {
                // Each request can cover up to 300 days
                var currentEnd = currentStart.AddDays(MaxCandlesPerRequest);
                if (currentEnd > absoluteEnd)
                    currentEnd = absoluteEnd;

                var url = $"https://api.exchange.coinbase.com/products/BTC-USD/candles?granularity={GranularitySeconds}" +
                          $"&start={Uri.EscapeDataString(currentStart.ToString("o"))}" +
                          $"&end={Uri.EscapeDataString(currentEnd.ToString("o"))}";

                using var resp = await _http.GetAsync(url, cancellationToken);
                if (!resp.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Coinbase returned {Status} for historical candles", resp.StatusCode);
                    break;
                }

                var data = await resp.Content.ReadFromJsonAsync<List<List<decimal>>>(cancellationToken);
                if (data == null || data.Count == 0)
                {
                    _logger.LogWarning("Coinbase returned no candle data for range {Start:s} to {End:s}", currentStart, currentEnd);
                    break;
                }

                foreach (var candle in data)
                {
                    if (candle.Count < 5)
                        continue;

                    // Coinbase returns [timestamp, low, high, open, close]
                    var timestampSeconds = (long)candle[0];
                    var closePrice = candle[4];
                    var date = DateTimeOffset.FromUnixTimeSeconds(timestampSeconds).UtcDateTime.Date;
                    result.Add((new DateTimeOffset(date, TimeSpan.Zero), closePrice));
                }

                currentStart = currentEnd.AddDays(1);
            }

            // Coinbase returns newest first per request, so sort ascending and de-duplicate by day.
            result = result
                .GroupBy(x => x.Item1.Date)
                .Select(g => g.Last())
                .OrderBy(x => x.Item1)
                .ToList();

            _logger.LogInformation("Fetched {Count} daily prices from Coinbase for range {Start:s} to {End:s}",
                result.Count, startDate, endDate);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch historical prices from Coinbase");
        }

        return result;
    }
}
