using CofferOS.Application.Abstractions.Persistence;
using CofferOS.Domain.Wallets;
using Microsoft.EntityFrameworkCore;

namespace CofferOS.Infrastructure.Persistence.Repositories;

public sealed class TimelineEventRepository : ITimelineEventRepository
{
    private readonly CofferOSDbContext _db;

    public TimelineEventRepository(CofferOSDbContext db) => _db = db;

    public Task<TimelineEvent?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.TimelineEvents.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public async Task<IReadOnlyList<TimelineEvent>> GetByWalletAsync(Guid walletId, CancellationToken cancellationToken = default) =>
        await _db.TimelineEvents
            .Where(e => e.WalletId == walletId)
            .OrderBy(e => e.OccurredAt)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(TimelineEvent timelineEvent, CancellationToken cancellationToken = default) =>
        await _db.TimelineEvents.AddAsync(timelineEvent, cancellationToken);

    public void Remove(TimelineEvent timelineEvent) => _db.TimelineEvents.Remove(timelineEvent);
}
