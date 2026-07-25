using CofferOS.Domain.Common;
using CofferOS.Domain.Treasury;

namespace CofferOS.Application.Treasury;

/// <summary>
/// Pure calculation service for Bitcoin-backed loan metrics.
/// No side effects; safe to call from any layer.
/// </summary>
public static class LoanCalculator
{
    /// <summary>
    /// Current collateral value = collateral BTC × current BTC price.
    /// </summary>
    public static decimal CalculateCollateralValue(decimal collateralBtc, decimal btcPrice)
    {
        if (collateralBtc < 0) throw new ArgumentException("Collateral cannot be negative.", nameof(collateralBtc));
        if (btcPrice < 0) throw new ArgumentException("BTC price cannot be negative.", nameof(btcPrice));
        return collateralBtc * btcPrice;
    }

    /// <summary>
    /// Current LTV = loan balance / collateral value.
    /// Returns 0 if collateral value is zero to avoid division by zero.
    /// </summary>
    public static decimal CalculateCurrentLtv(decimal loanBalance, decimal collateralValue)
    {
        if (loanBalance < 0) throw new ArgumentException("Loan balance cannot be negative.", nameof(loanBalance));
        if (collateralValue < 0) throw new ArgumentException("Collateral value cannot be negative.", nameof(collateralValue));
        if (collateralValue == 0) return 0m;
        return loanBalance / collateralValue;
    }

    /// <summary>
    /// LTV from a loan entity (convenience).
    /// </summary>
    public static decimal CalculateCurrentLtv(Loan loan)
    {
        ArgumentNullException.ThrowIfNull(loan);
        var collateralValue = CalculateCollateralValue(loan.CollateralAmountBtc, loan.CurrentBtcPrice);
        return CalculateCurrentLtv(loan.CurrentBalance, collateralValue);
    }

    /// <summary>
    /// Price at which LTV would equal the liquidation threshold.
    /// liquidationPrice = loanBalance / (collateralBtc × liquidationLtv)
    /// Returns 0 if inputs would cause division by zero.
    /// </summary>
    public static decimal CalculateLiquidationPrice(decimal loanBalance, decimal collateralBtc, decimal liquidationLtv)
    {
        if (loanBalance < 0) throw new ArgumentException("Loan balance cannot be negative.", nameof(loanBalance));
        if (collateralBtc < 0) throw new ArgumentException("Collateral cannot be negative.", nameof(collateralBtc));
        if (liquidationLtv <= 0 || liquidationLtv > 1)
            throw new ArgumentException("Liquidation LTV must be between 0 (exclusive) and 1.", nameof(liquidationLtv));

        var denom = collateralBtc * liquidationLtv;
        if (denom == 0) return 0m;
        return loanBalance / denom;
    }

    /// <summary>
    /// Price at which LTV would equal the warning threshold.
    /// </summary>
    public static decimal CalculateWarningPrice(decimal loanBalance, decimal collateralBtc, decimal warningLtv)
    {
        if (loanBalance < 0) throw new ArgumentException("Loan balance cannot be negative.", nameof(loanBalance));
        if (collateralBtc < 0) throw new ArgumentException("Collateral cannot be negative.", nameof(collateralBtc));
        if (warningLtv <= 0 || warningLtv > 1)
            throw new ArgumentException("Warning LTV must be between 0 (exclusive) and 1.", nameof(warningLtv));

        var denom = collateralBtc * warningLtv;
        if (denom == 0) return 0m;
        return loanBalance / denom;
    }

    /// <summary>
    /// Distance (in LTV decimal) to the warning threshold. Positive means buffer remains.
    /// </summary>
    public static decimal CalculateDistanceToWarning(decimal currentLtv, decimal warningLtv)
    {
        if (currentLtv < 0) throw new ArgumentException("Current LTV cannot be negative.", nameof(currentLtv));
        if (warningLtv <= 0 || warningLtv > 1)
            throw new ArgumentException("Warning LTV must be between 0 (exclusive) and 1.", nameof(warningLtv));
        return warningLtv - currentLtv;
    }

    /// <summary>
    /// Distance (in LTV decimal) to the liquidation threshold. Positive means buffer remains.
    /// </summary>
    public static decimal CalculateDistanceToLiquidation(decimal currentLtv, decimal liquidationLtv)
    {
        if (currentLtv < 0) throw new ArgumentException("Current LTV cannot be negative.", nameof(currentLtv));
        if (liquidationLtv <= 0 || liquidationLtv > 1)
            throw new ArgumentException("Liquidation LTV must be between 0 (exclusive) and 1.", nameof(liquidationLtv));
        return liquidationLtv - currentLtv;
    }

    /// <summary>
    /// Remaining collateral buffer in BTC at current price before hitting warning LTV.
    /// bufferBtc = (balance / warningLtv) / price - collateralBtc
    /// Returns 0 if already past threshold.
    /// </summary>
    public static decimal CalculateRemainingCollateralBuffer(decimal loanBalance, decimal collateralBtc, decimal btcPrice, decimal warningLtv)
    {
        if (loanBalance < 0) throw new ArgumentException("Loan balance cannot be negative.", nameof(loanBalance));
        if (collateralBtc < 0) throw new ArgumentException("Collateral cannot be negative.", nameof(collateralBtc));
        if (btcPrice < 0) throw new ArgumentException("BTC price cannot be negative.", nameof(btcPrice));
        if (warningLtv <= 0 || warningLtv > 1)
            throw new ArgumentException("Warning LTV must be between 0 (exclusive) and 1.", nameof(warningLtv));

        if (btcPrice == 0) return 0m;

        var collateralNeeded = loanBalance / warningLtv / btcPrice;
        var buffer = collateralNeeded - collateralBtc;
        return buffer > 0 ? buffer : 0m;
    }

    /// <summary>
    /// Simple interest accrual estimate over a number of periods.
    /// For fixed rate loans. Not compounded.
    /// </summary>
    public static decimal CalculateSimpleInterest(decimal principalOrBalance, decimal annualRate, int periods, PaymentFrequency frequency)
    {
        if (principalOrBalance < 0) throw new ArgumentException("Amount cannot be negative.", nameof(principalOrBalance));
        if (annualRate < 0) throw new ArgumentException("Interest rate cannot be negative.", nameof(annualRate));
        if (periods < 0) throw new ArgumentException("Periods cannot be negative.", nameof(periods));

        var ratePerPeriod = frequency switch
        {
            PaymentFrequency.Weekly => annualRate / 52m,
            PaymentFrequency.BiWeekly => annualRate / 26m,
            PaymentFrequency.Monthly => annualRate / 12m,
            PaymentFrequency.Quarterly => annualRate / 4m,
            PaymentFrequency.Annually => annualRate,
            PaymentFrequency.OneTime => annualRate,
            _ => annualRate / 12m
        };

        return principalOrBalance * ratePerPeriod * periods;
    }
}
