using CofferOS.Application.Abstractions.Persistence;
using CofferOS.Application.Prices;
using CofferOS.Domain.Treasury;
using Microsoft.Extensions.DependencyInjection;

namespace CofferOS.Api.BackgroundServices;

/// <summary>
/// Daily background worker that fetches and stores the latest Bitcoin price.
/// Runs once per day (at startup + every 24h).
/// </summary>
public sealed class DailyPriceHistoryService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<DailyPriceHistoryService> _logger;

    public DailyPriceHistoryService(IServiceProvider services, ILogger<DailyPriceHistoryService> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Small delay to let the app fully start
        await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunPriceUpdateCycleAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Price history update cycle failed");
            }

            try
            {
                // Run roughly once per day. A real implementation could schedule at a fixed time (e.g. 00:10 local).
                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
        }
    }

    private async Task RunPriceUpdateCycleAsync(CancellationToken ct)
    {
        await using var scope = _services.CreateAsyncScope();
        var loansRepo = scope.ServiceProvider.GetRequiredService<ILoanRepository>();
        var priceSnapshotsRepo = scope.ServiceProvider.GetRequiredService<ILoanPriceSnapshotRepository>();
        var historicalPriceService = scope.ServiceProvider.GetRequiredService<CoinGeckoHistoricalPriceService>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var activeLoans = await loansRepo.GetActiveAsync(ct);
        _logger.LogInformation("Daily price update cycle starting for {LoanCount} active loans", activeLoans.Count);
        if (activeLoans.Count == 0)
        {
            _logger.LogDebug("No active loans to update price history for.");
            return;
        }

        var today = DateTimeOffset.UtcNow.Date;
        var snapshotsToAdd = new List<LoanPriceSnapshot>();

        foreach (var loan in activeLoans)
        {
            var latestSnapshot = await priceSnapshotsRepo.GetLatestByLoanAsync(loan.Id, ct);
            if (latestSnapshot != null && latestSnapshot.SnapshotDate.Date >= today)
                continue;

            // If the latest snapshot is in a different currency, the loan's currency changed; re-fetch everything from loan start.
            var currency = string.IsNullOrWhiteSpace(loan.Currency) ? "USD" : loan.Currency;
            var latestCurrency = latestSnapshot?.Currency;
            var fetchStart = latestSnapshot?.SnapshotDate.AddDays(1) ?? loan.LoanStartDate;
            if (latestSnapshot != null && !string.IsNullOrEmpty(latestCurrency) &&
                !latestCurrency.Equals(currency, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Loan {LoanId} currency changed from {OldCurrency} to {NewCurrency}; removing {Count} snapshots and re-fetching from {Start:s}",
                    loan.Id, latestCurrency, currency, (await priceSnapshotsRepo.GetByLoanAsync(loan.Id, ct)).Count, loan.LoanStartDate);
                foreach (var existing in await priceSnapshotsRepo.GetByLoanAsync(loan.Id, ct))
                    priceSnapshotsRepo.Remove(existing);
                fetchStart = loan.LoanStartDate;
            }

            _logger.LogInformation("Fetching BTC-{Currency} daily prices for loan {LoanId} from {Start:s} to {End:s}",
                currency, loan.Id, fetchStart, today);

            var prices = await historicalPriceService.GetDailyPricesAsync(fetchStart, today, currency, ct);
            _logger.LogInformation("CoinGecko returned {Count} daily BTC-{Currency} prices for loan {LoanId}", prices.Count, currency, loan.Id);

            var addedForLoan = 0;
            foreach (var (date, price) in prices)
            {
                if (date.Date > (latestSnapshot?.SnapshotDate.Date ?? DateTimeOffset.MinValue))
                {
                    snapshotsToAdd.Add(new LoanPriceSnapshot(loan.Id, date, price, currency, "coingecko"));
                    addedForLoan++;
                }
            }

            if (addedForLoan > 0)
                _logger.LogInformation("Prepared {Count} BTC-{Currency} snapshots for loan {LoanId}", addedForLoan, currency, loan.Id);
        }

        if (snapshotsToAdd.Count > 0)
        {
            await priceSnapshotsRepo.AddRangeAsync(snapshotsToAdd, ct);
            await unitOfWork.SaveChangesAsync(ct);
            _logger.LogInformation("Saved {Count} daily BTC price snapshots for {LoanCount} loans", snapshotsToAdd.Count, activeLoans.Count);
        }
        else
        {
            _logger.LogInformation("No new price snapshots to add");
        }
    }
}
