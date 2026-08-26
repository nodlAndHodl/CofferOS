using CofferOS.Application.Abstractions.Persistence;
using CofferOS.Domain.Treasury;
using Microsoft.EntityFrameworkCore;

namespace CofferOS.Infrastructure.Persistence.Repositories;

public sealed class LoanCollateralTransactionRepository : ILoanCollateralTransactionRepository
{
    private readonly CofferOSDbContext _db;

    public LoanCollateralTransactionRepository(CofferOSDbContext db) => _db = db;

    public async Task<IReadOnlyList<LoanCollateralTransaction>> GetByLoanAsync(Guid loanId, CancellationToken cancellationToken = default) =>
        await _db.LoanCollateralTransactions
            .Where(t => t.LoanId == loanId)
            .OrderBy(t => t.TransactionDate)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(LoanCollateralTransaction transaction, CancellationToken cancellationToken = default) =>
        await _db.LoanCollateralTransactions.AddAsync(transaction, cancellationToken);

    public void Remove(LoanCollateralTransaction transaction) =>
        _db.LoanCollateralTransactions.Remove(transaction);
}
