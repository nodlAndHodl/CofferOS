using CofferOS.Application.Abstractions.Persistence;
using CofferOS.Domain.Treasury;
using Microsoft.EntityFrameworkCore;

namespace CofferOS.Infrastructure.Persistence.Repositories;

public sealed class LoanPaymentRepository : ILoanPaymentRepository
{
    private readonly CofferOSDbContext _db;

    public LoanPaymentRepository(CofferOSDbContext db) => _db = db;

    public async Task<IReadOnlyList<LoanPayment>> GetByLoanAsync(Guid loanId, CancellationToken cancellationToken = default) =>
        await _db.LoanPayments
            .Where(p => p.LoanId == loanId)
            .OrderBy(p => p.PaymentDate)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(LoanPayment payment, CancellationToken cancellationToken = default) =>
        await _db.LoanPayments.AddAsync(payment, cancellationToken);

    public void Remove(LoanPayment payment) => _db.LoanPayments.Remove(payment);
}
