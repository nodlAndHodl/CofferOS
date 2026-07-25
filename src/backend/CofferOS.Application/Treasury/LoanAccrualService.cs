using CofferOS.Application.Abstractions.Persistence;
using CofferOS.Domain.Treasury;

namespace CofferOS.Application.Treasury;

/// <summary>
/// Default implementation of the loan accrual engine using simple daily interest (actual/365).
/// Derived values come from original terms + accrual state + payment history.
/// </summary>
public sealed class LoanAccrualService : ILoanAccrualService
{
    private readonly ILoanRepository _loans;
    private readonly ILoanPaymentRepository _payments;
    private readonly IUnitOfWork _uow;

    public LoanAccrualService(ILoanRepository loans, ILoanPaymentRepository payments, IUnitOfWork uow)
    {
        _loans = loans;
        _payments = payments;
        _uow = uow;
    }

    public async Task<LoanAccrualSnapshot> CalculateAsync(Loan loan, IReadOnlyList<LoanPayment> payments, DateTimeOffset? asOf = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(loan);

        var asOfDate = (asOf ?? DateTimeOffset.UtcNow).Date;
        var startDate = loan.LoanStartDate.Date;

        // Outstanding principal = original principal - principal portions of all recorded payments
        decimal principalPaid = payments.Sum(p => p.PrincipalAmount);
        decimal outstandingPrincipal = Math.Max(0m, loan.PrincipalAmount - principalPaid);

        decimal totalInterestPaid = payments.Sum(p => p.InterestAmount);

        // Start with stored accrued interest
        decimal accrued = Math.Max(0m, loan.AccruedInterest);

        // Add any additional accrual since LastAccruedOn (or start)
        var lastAccrual = loan.LastAccruedOn?.Date ?? startDate;
        int daysSinceLast = (asOfDate - lastAccrual).Days;
        if (daysSinceLast > 0)
        {
            decimal dailyRate = loan.InterestRate / 365m;
            accrued += outstandingPrincipal * dailyRate * daysSinceLast;
        }

        decimal currentBalance = outstandingPrincipal + Math.Max(0m, accrued);
        decimal dailyRateNow = loan.InterestRate / 365m;
        decimal dailyInterest = outstandingPrincipal * dailyRateNow;

        decimal collateralValue = LoanCalculator.CalculateCollateralValue(loan.CollateralAmountBtc, loan.CurrentBtcPrice);
        decimal currentLtv = LoanCalculator.CalculateCurrentLtv(currentBalance, collateralValue);

        return new LoanAccrualSnapshot(
            outstandingPrincipal,
            Math.Max(0m, accrued),
            currentBalance,
            dailyRateNow,
            dailyInterest,
            currentBalance, // total payoff amount
            totalInterestPaid,
            currentLtv);
    }

    public async Task<decimal> AccrueSimpleDailyInterestAsync(Loan loan, DateTimeOffset asOf, CancellationToken cancellationToken = default)
    {
        if (loan.Status != LoanStatus.Active)
            return 0m;

        var payments = await _payments.GetByLoanAsync(loan.Id, cancellationToken);
        var snapshot = await CalculateAsync(loan, payments, asOf, cancellationToken);

        var last = loan.LastAccruedOn?.Date ?? loan.LoanStartDate.Date;
        int days = (asOf.Date - last).Days;

        if (days <= 0)
            return 0m;

        decimal interest = snapshot.OutstandingPrincipal * snapshot.DailyInterestRate * days;

        if (interest > 0)
        {
            loan.AddAccruedInterest(interest, asOf);
            loan.RefreshCurrentBalance();
        }

        return Math.Max(0m, interest);
    }

    public async Task<LoanPayment> RecordPaymentAsync(Loan loan, DateTimeOffset paymentDate, decimal totalAmount, string? notes = null, CancellationToken cancellationToken = default)
    {
        if (totalAmount <= 0)
            throw new ArgumentException("Payment amount must be positive.", nameof(totalAmount));

        var payments = await _payments.GetByLoanAsync(loan.Id, cancellationToken);
        var snapshot = await CalculateAsync(loan, payments, paymentDate, cancellationToken);

        decimal interestPortion = Math.Min(totalAmount, snapshot.AccruedInterest);
        decimal principalPortion = totalAmount - interestPortion;

        if (interestPortion > 0)
        {
            loan.ReduceAccruedInterest(interestPortion, paymentDate);
        }

        if (principalPortion > 0)
        {
            loan.ReducePrincipal(principalPortion, paymentDate);
        }

        loan.RefreshCurrentBalance();

        var payment = new LoanPayment(loan.Id, paymentDate, totalAmount, principalPortion, interestPortion, notes);

        await _payments.AddAsync(payment, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return payment;
    }
}
