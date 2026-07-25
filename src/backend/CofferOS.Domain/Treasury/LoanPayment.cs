using CofferOS.Domain.Common;

namespace CofferOS.Domain.Treasury;

/// <summary>
/// A payment made against a loan. Records the split between principal and interest.
/// Balances are derived from original terms + accrual + payment history.
/// </summary>
public sealed class LoanPayment
{
    private LoanPayment() { }

    public LoanPayment(
        Guid loanId,
        DateTimeOffset paymentDate,
        decimal totalAmount,
        decimal principalAmount,
        decimal interestAmount,
        string? notes = null)
    {
        if (loanId == Guid.Empty) throw new ArgumentException("LoanId is required.", nameof(loanId));
        if (paymentDate == default) throw new ArgumentException("Payment date is required.", nameof(paymentDate));
        if (totalAmount < 0) throw new ArgumentException("Total amount cannot be negative.", nameof(totalAmount));
        if (principalAmount < 0) throw new ArgumentException("Principal amount cannot be negative.", nameof(principalAmount));
        if (interestAmount < 0) throw new ArgumentException("Interest amount cannot be negative.", nameof(interestAmount));

        LoanId = loanId;
        PaymentDate = paymentDate;
        TotalAmount = totalAmount;
        PrincipalAmount = principalAmount;
        InterestAmount = interestAmount;
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid LoanId { get; private set; }
    public DateTimeOffset PaymentDate { get; private set; }
    public decimal TotalAmount { get; private set; }
    public decimal PrincipalAmount { get; private set; }
    public decimal InterestAmount { get; private set; }
    public string? Notes { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
}
