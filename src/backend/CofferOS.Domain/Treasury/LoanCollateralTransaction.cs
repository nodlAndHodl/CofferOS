using CofferOS.Domain.Common;

namespace CofferOS.Domain.Treasury;

/// <summary>
/// Records a single collateral adjustment on a loan (adding or removing BTC).
/// Each transaction captures the before/after state for a full audit trail.
/// </summary>
public sealed class LoanCollateralTransaction
{
    private LoanCollateralTransaction() { }

    public LoanCollateralTransaction(
        Guid loanId,
        CollateralTransactionType transactionType,
        decimal amountBtc,
        decimal btcPriceAtTime,
        decimal collateralAmountBtcBefore,
        decimal collateralAmountBtcAfter,
        decimal ltvAtTime,
        DateTimeOffset transactionDate,
        string? notes = null)
    {
        if (loanId == Guid.Empty) throw new ArgumentException("LoanId is required.", nameof(loanId));
        if (amountBtc <= 0) throw new ArgumentException("Amount must be positive.", nameof(amountBtc));
        if (btcPriceAtTime < 0) throw new ArgumentException("BTC price cannot be negative.", nameof(btcPriceAtTime));
        if (collateralAmountBtcBefore < 0) throw new ArgumentException("Collateral before cannot be negative.", nameof(collateralAmountBtcBefore));
        if (collateralAmountBtcAfter < 0) throw new ArgumentException("Collateral after cannot be negative.", nameof(collateralAmountBtcAfter));
        if (ltvAtTime < 0) throw new ArgumentException("LTV cannot be negative.", nameof(ltvAtTime));

        LoanId = loanId;
        TransactionType = transactionType;
        AmountBtc = amountBtc;
        BtcPriceAtTime = btcPriceAtTime;
        CollateralAmountBtcBefore = collateralAmountBtcBefore;
        CollateralAmountBtcAfter = collateralAmountBtcAfter;
        LtvAtTime = ltvAtTime;
        TransactionDate = transactionDate;
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid LoanId { get; private set; }
    public CollateralTransactionType TransactionType { get; private set; }

    /// <summary>The absolute BTC amount added or removed (always positive).</summary>
    public decimal AmountBtc { get; private set; }

    /// <summary>BTC price in the loan's currency at the time of the transaction.</summary>
    public decimal BtcPriceAtTime { get; private set; }

    /// <summary>Total collateral BTC before this transaction.</summary>
    public decimal CollateralAmountBtcBefore { get; private set; }

    /// <summary>Total collateral BTC after this transaction.</summary>
    public decimal CollateralAmountBtcAfter { get; private set; }

    /// <summary>LTV calculated immediately after this transaction.</summary>
    public decimal LtvAtTime { get; private set; }

    public DateTimeOffset TransactionDate { get; private set; }
    public string? Notes { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
}
