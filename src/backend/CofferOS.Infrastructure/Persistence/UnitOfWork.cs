using CofferOS.Application.Abstractions.Persistence;

namespace CofferOS.Infrastructure.Persistence;

/// <summary>
/// Thin unit-of-work over the DbContext. Domain events are dispatched inside
/// <see cref="CofferOSDbContext.SaveChangesAsync"/> after the transaction commits.
/// </summary>
public sealed class UnitOfWork : IUnitOfWork
{
    private readonly CofferOSDbContext _db;

    public UnitOfWork(CofferOSDbContext db) => _db = db;

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _db.SaveChangesAsync(cancellationToken);
}
