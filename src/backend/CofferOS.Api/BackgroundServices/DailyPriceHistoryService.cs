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
        var historicalPriceService = scope.ServiceProvider.GetRequiredService<CoinbaseHistoricalPriceService>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var activeLoans = await loansRepo.GetActiveAsync(ct);
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

            // Fetch from the day after the latest snapshot (or loan start) through today
            var fetchStart = latestSnapshot?.SnapshotDate.AddDays(1) ?? loan.LoanStartDate;
            var prices = await historicalPriceService.GetDailyPricesAsync(fetchStart, today, ct);

            foreach (var (date, price) in prices)
            {
                if (date.Date > (latestSnapshot?.SnapshotDate.Date ?? DateTimeOffset.MinValue))
                {
                    snapshotsToAdd.Add(new LoanPriceSnapshot(loan.Id, date, price, "coinbase"));
                }
            }
        }

        if (snapshotsToAdd.Count > 0)
        {
            await priceSnapshotsRepo.AddRangeAsync(snapshotsToAdd, ct);
            await unitOfWork.SaveChangesAsync(ct);
            _logger.LogInformation("Added {Count} price snapshots for {LoanCount} loans", snapshotsToAdd.Count, activeLoans.Count);
        }
        else
        {
            _logger.LogDebug("All loans already have today's price snapshot");
        }
    }
}
