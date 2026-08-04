using CofferOS.Application.Abstractions.Persistence;
using CofferOS.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace CofferOS.Infrastructure.Persistence.Repositories;

/// <summary>Read/write access to user-provided cost basis amounts.</summary>
public sealed class CostBasisRepository : ICostBasisRepository
{
    private readonly CofferOSDbContext _context;

    public CostBasisRepository(CofferOSDbContext context)
    {
        _context = context;
    }

    public Task<CostBasisEntry?> GetAsync(CostBasisTarget target, string reference, CancellationToken cancellationToken = default)
        => _context.CostBasisEntries
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Target == target && x.Reference == reference, cancellationToken);

    public Task<IReadOnlyList<CostBasisEntry>> GetByReferencesAsync(
        CostBasisTarget target,
        IReadOnlyList<string> references,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(references);
        if (references.Count == 0)
            return Task.FromResult<IReadOnlyList<CostBasisEntry>>(Array.Empty<CostBasisEntry>());

        return _context.CostBasisEntries
            .AsNoTracking()
            .Where(x => x.Target == target && references.Contains(x.Reference))
            .ToListAsync(cancellationToken)
            .ContinueWith(t => (IReadOnlyList<CostBasisEntry>)t.Result, cancellationToken, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
    }

    public Task<IReadOnlyList<CostBasisEntry>> GetByTargetAsync(CostBasisTarget target, CancellationToken cancellationToken = default)
        => _context.CostBasisEntries
            .AsNoTracking()
            .Where(x => x.Target == target)
            .ToListAsync(cancellationToken)
            .ContinueWith(t => (IReadOnlyList<CostBasisEntry>)t.Result, cancellationToken, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);

    public async Task AddAsync(CostBasisEntry entry, CancellationToken cancellationToken = default)
    {
        var existing = await _context.CostBasisEntries
            .FirstOrDefaultAsync(x => x.Target == entry.Target && x.Reference == entry.Reference, cancellationToken);

        if (existing is not null)
        {
            existing.UpdateAmount(entry.Amount);
            _context.CostBasisEntries.Update(existing);
        }
        else
        {
            await _context.CostBasisEntries.AddAsync(entry, cancellationToken);
        }
    }

    public void Remove(CostBasisEntry entry)
    {
        _context.CostBasisEntries.Remove(entry);
    }
}
