using CofferOS.Application.Abstractions.Persistence;
using CofferOS.Domain.Treasury;
using Microsoft.EntityFrameworkCore;

namespace CofferOS.Infrastructure.Persistence.Repositories;

public sealed class LoanPriceSnapshotRepository : ILoanPriceSnapshotRepository
{
    private readonly CofferOSDbContext _db;

    public LoanPriceSnapshotRepository(CofferOSDbContext db) => _db = db;

    public async Task<IReadOnlyList<LoanPriceSnapshot>> GetByLoanAsync(Guid loanId, CancellationToken cancellationToken = default) =>
        await _db.LoanPriceSnapshots
            .Where(x => x.LoanId == loanId)
            .OrderBy(x => x.SnapshotDate)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<LoanPriceSnapshot>> GetByLoanInRangeAsync(Guid loanId, DateTimeOffset startDate, DateTimeOffset endDate, CancellationToken cancellationToken = default) =>
        await _db.LoanPriceSnapshots
            .Where(x => x.LoanId == loanId && x.SnapshotDate >= startDate && x.SnapshotDate <= endDate)
            .OrderBy(x => x.SnapshotDate)
            .ToListAsync(cancellationToken);

    public async Task<LoanPriceSnapshot?> GetLatestByLoanAsync(Guid loanId, CancellationToken cancellationToken = default) =>
        await _db.LoanPriceSnapshots
            .Where(x => x.LoanId == loanId)
            .OrderByDescending(x => x.SnapshotDate)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task AddAsync(LoanPriceSnapshot snapshot, CancellationToken cancellationToken = default) =>
        await _db.LoanPriceSnapshots.AddAsync(snapshot, cancellationToken);

    public async Task AddRangeAsync(IEnumerable<LoanPriceSnapshot> snapshots, CancellationToken cancellationToken = default) =>
        await _db.LoanPriceSnapshots.AddRangeAsync(snapshots, cancellationToken);

    public void Remove(LoanPriceSnapshot snapshot) =>
        _db.LoanPriceSnapshots.Remove(snapshot);
}
