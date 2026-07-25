using CofferOS.Application.Abstractions.Persistence;
using CofferOS.Domain.Prices;
using Microsoft.EntityFrameworkCore;

namespace CofferOS.Infrastructure.Persistence.Repositories;

public sealed class BitcoinPriceHistoryRepository : IBitcoinPriceHistoryRepository
{
    private readonly CofferOSDbContext _db;

    public BitcoinPriceHistoryRepository(CofferOSDbContext db) => _db = db;

    public Task<BitcoinPriceHistory?> GetLatestAsync(CancellationToken cancellationToken = default) =>
        _db.BitcoinPriceHistory
            .OrderByDescending(x => x.Timestamp)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<BitcoinPriceHistory>> GetRecentAsync(int count, CancellationToken cancellationToken = default) =>
        await _db.BitcoinPriceHistory
            .OrderByDescending(x => x.Timestamp)
            .Take(Math.Max(1, count))
            .ToListAsync(cancellationToken);

    public async Task AddAsync(BitcoinPriceHistory entry, CancellationToken cancellationToken = default) =>
        await _db.BitcoinPriceHistory.AddAsync(entry, cancellationToken);
}
