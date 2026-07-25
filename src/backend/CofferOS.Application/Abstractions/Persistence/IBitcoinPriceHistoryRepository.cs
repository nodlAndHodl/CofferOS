using CofferOS.Domain.Prices;

namespace CofferOS.Application.Abstractions.Persistence;

/// <summary>
/// Read/write access to persisted Bitcoin price snapshots.
/// </summary>
public interface IBitcoinPriceHistoryRepository
{
    /// <summary>Returns the most recent price snapshot (if any).</summary>
    Task<BitcoinPriceHistory?> GetLatestAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns recent price history, newest first.</summary>
    Task<IReadOnlyList<BitcoinPriceHistory>> GetRecentAsync(int count, CancellationToken cancellationToken = default);

    /// <summary>Persists a new price snapshot.</summary>
    Task AddAsync(BitcoinPriceHistory entry, CancellationToken cancellationToken = default);
}
