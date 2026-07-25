using CofferOS.Domain.Common;

namespace CofferOS.Domain.Treasury;

/// <summary>
/// A Bitcoin-collateralized loan tracked locally by the user.
/// All values are entered manually (Phase 1); no exchange integration.
/// </summary>
public sealed class Loan : Entity
{
    private Loan() { }

    private Loan(
        string name,
        string? lender,
        decimal principalAmount,
        decimal currentBalance,
        decimal interestRate,
        InterestType interestType,
        DateTimeOffset loanStartDate,
        int? loanTermMonths,
        PaymentFrequency paymentFrequency,
        decimal collateralAmountBtc,
        decimal currentBtcPrice,
        decimal warningLtv,
        decimal liquidationLtv,
        string? notes)
    {
        Name = name;
        Lender = lender;
        Status = LoanStatus.Active;
        PrincipalAmount = principalAmount;
        CurrentBalance = currentBalance;
        InterestRate = interestRate;
        InterestType = interestType;
        LoanStartDate = loanStartDate;
        LoanTermMonths = loanTermMonths;
        PaymentFrequency = paymentFrequency;
        CollateralAmountBtc = collateralAmountBtc;
        CurrentBtcPrice = currentBtcPrice;
        WarningLtv = warningLtv;
        LiquidationLtv = liquidationLtv;
        Notes = notes;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;

        // Initialize accrual engine state
        AccruedInterest = 0m;
        LastAccruedOn = loanStartDate;
    }

    public string Name { get; private set; } = string.Empty;
    public string? Lender { get; private set; }
    public LoanStatus Status { get; private set; }

    public string? Notes { get; private set; }

    // Financial
    public decimal PrincipalAmount { get; private set; }

    // Derived balances (CurrentBalance kept for backward display compatibility during transition;
    // new accrual engine computes authoritative values).
    public decimal CurrentBalance { get; private set; }

    public decimal InterestRate { get; private set; }
    public InterestType InterestType { get; private set; }
    public DateTimeOffset LoanStartDate { get; private set; }
    public int? LoanTermMonths { get; private set; }
    public PaymentFrequency PaymentFrequency { get; private set; }

    // Accrual engine state (source of truth for interest)
    public decimal AccruedInterest { get; private set; }
    public DateTimeOffset? LastAccruedOn { get; private set; }

    // Collateral
    public decimal CollateralAmountBtc { get; private set; }
    public decimal CurrentBtcPrice { get; private set; }

    // Thresholds (as decimals, e.g. 0.70 = 70%)
    public decimal WarningLtv { get; private set; }
    public decimal LiquidationLtv { get; private set; }

    // Audit
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>Factory for creating a new loan.</summary>
    public static Loan Create(
        string name,
        string? lender,
        decimal principalAmount,
        decimal currentBalance,
        decimal interestRate,
        InterestType interestType,
        DateTimeOffset loanStartDate,
        int? loanTermMonths,
        PaymentFrequency paymentFrequency,
        decimal collateralAmountBtc,
        decimal currentBtcPrice,
        decimal warningLtv,
        decimal liquidationLtv,
        string? notes)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Loan name is required.", nameof(name));
        if (principalAmount < 0)
            throw new ArgumentException("Principal amount cannot be negative.", nameof(principalAmount));
        if (currentBalance < 0)
            throw new ArgumentException("Current balance cannot be negative.", nameof(currentBalance));
        if (collateralAmountBtc < 0)
            throw new ArgumentException("Collateral amount cannot be negative.", nameof(collateralAmountBtc));
        if (currentBtcPrice < 0)
            throw new ArgumentException("BTC price cannot be negative.", nameof(currentBtcPrice));
        if (warningLtv < 0 || warningLtv > 1)
            throw new ArgumentException("Warning LTV must be between 0 and 1.", nameof(warningLtv));
        if (liquidationLtv < 0 || liquidationLtv > 1)
            throw new ArgumentException("Liquidation LTV must be between 0 and 1.", nameof(liquidationLtv));
        if (liquidationLtv <= warningLtv)
            throw new ArgumentException("Liquidation LTV must be greater than warning LTV.", nameof(liquidationLtv));

        return new Loan(
            name.Trim(),
            lender?.Trim(),
            principalAmount,
            currentBalance,
            interestRate,
            interestType,
            loanStartDate,
            loanTermMonths,
            paymentFrequency,
            collateralAmountBtc,
            currentBtcPrice,
            warningLtv,
            liquidationLtv,
            notes?.Trim());
    }

    public void UpdateDetails(
        string name,
        string? lender,
        decimal principalAmount,
        decimal currentBalance,
        decimal interestRate,
        InterestType interestType,
        DateTimeOffset loanStartDate,
        int? loanTermMonths,
        PaymentFrequency paymentFrequency,
        decimal collateralAmountBtc,
        decimal currentBtcPrice,
        decimal warningLtv,
        decimal liquidationLtv,
        string? notes)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Loan name is required.", nameof(name));
        if (principalAmount < 0)
            throw new ArgumentException("Principal amount cannot be negative.", nameof(principalAmount));
        if (currentBalance < 0)
            throw new ArgumentException("Current balance cannot be negative.", nameof(currentBalance));
        if (collateralAmountBtc < 0)
            throw new ArgumentException("Collateral amount cannot be negative.", nameof(collateralAmountBtc));
        if (currentBtcPrice < 0)
            throw new ArgumentException("BTC price cannot be negative.", nameof(currentBtcPrice));
        if (warningLtv < 0 || warningLtv > 1)
            throw new ArgumentException("Warning LTV must be between 0 and 1.", nameof(warningLtv));
        if (liquidationLtv < 0 || liquidationLtv > 1)
            throw new ArgumentException("Liquidation LTV must be between 0 and 1.", nameof(liquidationLtv));
        if (liquidationLtv <= warningLtv)
            throw new ArgumentException("Liquidation LTV must be greater than warning LTV.", nameof(liquidationLtv));

        Name = name.Trim();
        Lender = lender?.Trim();
        PrincipalAmount = principalAmount;
        CurrentBalance = currentBalance;
        InterestRate = interestRate;
        InterestType = interestType;
        LoanStartDate = loanStartDate;
        LoanTermMonths = loanTermMonths;
        PaymentFrequency = paymentFrequency;
        CollateralAmountBtc = collateralAmountBtc;
        CurrentBtcPrice = currentBtcPrice;
        WarningLtv = warningLtv;
        LiquidationLtv = liquidationLtv;
        Notes = notes?.Trim();
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdateBalance(decimal newBalance)
    {
        if (newBalance < 0)
            throw new ArgumentException("Balance cannot be negative.", nameof(newBalance));
        CurrentBalance = newBalance;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdateCollateral(decimal collateralBtc, decimal btcPrice)
    {
        if (collateralBtc < 0)
            throw new ArgumentException("Collateral cannot be negative.", nameof(collateralBtc));
        if (btcPrice < 0)
            throw new ArgumentException("BTC price cannot be negative.", nameof(btcPrice));
        CollateralAmountBtc = collateralBtc;
        CurrentBtcPrice = btcPrice;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdatePrice(decimal btcPrice)
    {
        if (btcPrice < 0)
            throw new ArgumentException("BTC price cannot be negative.", nameof(btcPrice));
        CurrentBtcPrice = btcPrice;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SetStatus(LoanStatus status)
    {
        Status = status;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Increases accrued interest (called by accrual engine).</summary>
    public void AddAccruedInterest(decimal amount, DateTimeOffset asOf)
    {
        if (amount < 0) throw new ArgumentException("Amount cannot be negative.", nameof(amount));
        AccruedInterest += amount;
        LastAccruedOn = asOf;
        // Keep CurrentBalance roughly in sync for display during transition
        CurrentBalance = PrincipalAmount + AccruedInterest; // will be adjusted by payments
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Reduces accrued interest when a payment is applied to interest first.</summary>
    public void ReduceAccruedInterest(decimal amount, DateTimeOffset asOf)
    {
        if (amount < 0) throw new ArgumentException("Amount cannot be negative.", nameof(amount));
        AccruedInterest = Math.Max(0, AccruedInterest - amount);
        LastAccruedOn = asOf;
        CurrentBalance = PrincipalAmount + AccruedInterest;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Reduces the principal balance (after interest is covered by a payment).</summary>
    public void ReducePrincipal(decimal amount, DateTimeOffset asOf)
    {
        if (amount < 0) throw new ArgumentException("Amount cannot be negative.", nameof(amount));
        // We track effective principal via AccruedInterest + remaining principal logic.
        // For now we reduce the displayed CurrentBalance and also reduce PrincipalAmount to keep derived calcs correct.
        var newPrincipal = Math.Max(0, PrincipalAmount - amount);
        PrincipalAmount = newPrincipal;
        CurrentBalance = newPrincipal + AccruedInterest;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Refreshes the displayed CurrentBalance from principal + accrued.</summary>
    public void RefreshCurrentBalance()
    {
        CurrentBalance = PrincipalAmount + Math.Max(0, AccruedInterest);
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
