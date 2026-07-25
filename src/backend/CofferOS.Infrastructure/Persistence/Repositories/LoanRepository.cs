using CofferOS.Application.Abstractions.Persistence;
using CofferOS.Domain.Common;
using CofferOS.Domain.Treasury;
using Microsoft.EntityFrameworkCore;

namespace CofferOS.Infrastructure.Persistence.Repositories;

public sealed class LoanRepository : ILoanRepository
{
    private readonly CofferOSDbContext _db;

    public LoanRepository(CofferOSDbContext db) => _db = db;

    public Task<Loan?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.Loans.FirstOrDefaultAsync(l => l.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Loan>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _db.Loans
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Loan>> GetActiveAsync(CancellationToken cancellationToken = default) =>
        await _db.Loans
            .Where(l => l.Status == LoanStatus.Active)
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(Loan loan, CancellationToken cancellationToken = default) =>
        await _db.Loans.AddAsync(loan, cancellationToken);

    public void Remove(Loan loan) => _db.Loans.Remove(loan);
}
