using CofferOS.Domain.Treasury;

namespace CofferOS.Application.Treasury;

/// <summary>
/// Service responsible for interest accrual calculations and derived loan balances.
/// Balances are computed from original terms + accrual + payment history.
/// </summary>
public interface ILoanAccrualService
{
    /// <summary>
    /// Computes a full snapshot of derived values for the given loan using current payments.
    /// Does not mutate state.
    /// </summary>
    Task<LoanAccrualSnapshot> CalculateAsync(Loan loan, IReadOnlyList<LoanPayment> payments, DateTimeOffset? asOf = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies simple daily interest accrual to the loan up to the given date.
    /// Mutates the loan (AccruedInterest + LastAccruedOn) and returns the interest added.
    /// Caller is responsible for persisting.
    /// </summary>
    Task<decimal> AccrueSimpleDailyInterestAsync(Loan loan, DateTimeOffset asOf, CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a payment and allocates it between interest and principal according to current accrued state.
    /// Returns the created payment.
    /// </summary>
    Task<LoanPayment> RecordPaymentAsync(Loan loan, DateTimeOffset paymentDate, decimal totalAmount, string? notes = null, CancellationToken cancellationToken = default);
}

/// <summary>
/// Snapshot of derived loan values produced by the accrual engine.
/// </summary>
public sealed record LoanAccrualSnapshot(
    decimal OutstandingPrincipal,
    decimal AccruedInterest,
    decimal CurrentBalance,
    decimal DailyInterestRate,
    decimal DailyInterest,
    decimal TotalPayoffAmount,
    decimal TotalInterestPaid,
    decimal CurrentLtv);
