using CofferOS.Application.Abstractions.Persistence;
using CofferOS.Domain.Retirement;
using Microsoft.EntityFrameworkCore;

namespace CofferOS.Infrastructure.Persistence.Repositories;

public sealed class RetirementAccountRepository : IRetirementAccountRepository
{
    private readonly CofferOSDbContext _db;

    public RetirementAccountRepository(CofferOSDbContext db) => _db = db;

    public Task<RetirementAccount?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.RetirementAccounts
            .Include(a => a.CostBasisEntries)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public async Task<IReadOnlyList<RetirementAccount>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _db.RetirementAccounts
            .Include(a => a.CostBasisEntries)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(RetirementAccount account, CancellationToken cancellationToken = default) =>
        await _db.RetirementAccounts.AddAsync(account, cancellationToken);

    public void Remove(RetirementAccount account) => _db.RetirementAccounts.Remove(account);
}
