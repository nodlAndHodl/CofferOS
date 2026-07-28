using CofferOS.Application.Abstractions.Persistence;
using CofferOS.Application.Abstractions.Treasury;

namespace CofferOS.Application.Treasury;

/// <summary>
/// Analyzes loan portfolio for risk metrics and health indicators.
/// </summary>
public sealed class LoanAnalyticsService : ILoanAnalyticsService
{
    private readonly ILoanRepository _loans;
    private readonly ILoanPaymentRepository _payments;
    private readonly ILoanAccrualService _accrual;

    public LoanAnalyticsService(
        ILoanRepository loans,
        ILoanPaymentRepository payments,
        ILoanAccrualService accrual)
    {
        _loans = loans;
        _payments = payments;
        _accrual = accrual;
    }

    public async Task<LoanRiskAnalysis?> GetHighestRiskLoanAsync(CancellationToken cancellationToken = default)
    {
        var active = await _loans.GetActiveAsync(cancellationToken);
        if (active.Count == 0) return null;

        LoanRiskAnalysis? highest = null;
        decimal? highestLtv = null;

        foreach (var loan in active)
        {
            var pays = await _payments.GetByLoanAsync(loan.Id, cancellationToken);
            var snap = await _accrual.CalculateAsync(loan, pays, null, cancellationToken);
            var ltv = snap?.CurrentLtv ?? LoanCalculator.CalculateCurrentLtv(loan);

            if (highestLtv is null || ltv > highestLtv)
            {
                highestLtv = ltv;
                var collateralValue = LoanCalculator.CalculateCollateralValue(loan.CollateralAmountBtc, loan.CurrentBtcPrice);
                var distWarn = LoanCalculator.CalculateDistanceToWarning(ltv, loan.WarningLtv);
                var distLiq = LoanCalculator.CalculateDistanceToLiquidation(ltv, loan.LiquidationLtv);
                var warnPrice = LoanCalculator.CalculateWarningPrice(snap?.CurrentBalance ?? loan.CurrentBalance, loan.CollateralAmountBtc, loan.WarningLtv);
                var liqPrice = LoanCalculator.CalculateLiquidationPrice(snap?.CurrentBalance ?? loan.CurrentBalance, loan.CollateralAmountBtc, loan.LiquidationLtv);

                highest = new LoanRiskAnalysis(
                    loan.Id,
                    loan.Name,
                    ltv,
                    loan.WarningLtv,
                    loan.LiquidationLtv,
                    distWarn,
                    distLiq,
                    warnPrice,
                    liqPrice);
            }
        }

        return highest;
    }

    public async Task<LoanRiskAnalysis?> GetNearestWarningThresholdAsync(CancellationToken cancellationToken = default)
    {
        var active = await _loans.GetActiveAsync(cancellationToken);
        if (active.Count == 0) return null;

        LoanRiskAnalysis? nearest = null;
        decimal? smallestDistance = null;

        foreach (var loan in active)
        {
            var pays = await _payments.GetByLoanAsync(loan.Id, cancellationToken);
            var snap = await _accrual.CalculateAsync(loan, pays, null, cancellationToken);
            var ltv = snap?.CurrentLtv ?? LoanCalculator.CalculateCurrentLtv(loan);
            var distWarn = LoanCalculator.CalculateDistanceToWarning(ltv, loan.WarningLtv);

            if (smallestDistance is null || distWarn < smallestDistance)
            {
                smallestDistance = distWarn;
                var collateralValue = LoanCalculator.CalculateCollateralValue(loan.CollateralAmountBtc, loan.CurrentBtcPrice);
                var distLiq = LoanCalculator.CalculateDistanceToLiquidation(ltv, loan.LiquidationLtv);
                var warnPrice = LoanCalculator.CalculateWarningPrice(snap?.CurrentBalance ?? loan.CurrentBalance, loan.CollateralAmountBtc, loan.WarningLtv);
                var liqPrice = LoanCalculator.CalculateLiquidationPrice(snap?.CurrentBalance ?? loan.CurrentBalance, loan.CollateralAmountBtc, loan.LiquidationLtv);

                nearest = new LoanRiskAnalysis(
                    loan.Id,
                    loan.Name,
                    ltv,
                    loan.WarningLtv,
                    loan.LiquidationLtv,
                    distWarn,
                    distLiq,
                    warnPrice,
                    liqPrice);
            }
        }

        return nearest;
    }

    public async Task<IReadOnlyList<LoanLiquidationEstimate>> GetLiquidationEstimatesAsync(CancellationToken cancellationToken = default)
    {
        var active = await _loans.GetActiveAsync(cancellationToken);
        var estimates = new List<LoanLiquidationEstimate>();

        foreach (var loan in active)
        {
            var pays = await _payments.GetByLoanAsync(loan.Id, cancellationToken);
            var snap = await _accrual.CalculateAsync(loan, pays, null, cancellationToken);
            var balance = snap?.CurrentBalance ?? loan.CurrentBalance;

            var warnPrice = LoanCalculator.CalculateWarningPrice(balance, loan.CollateralAmountBtc, loan.WarningLtv);
            var liqPrice = LoanCalculator.CalculateLiquidationPrice(balance, loan.CollateralAmountBtc, loan.LiquidationLtv);

            var percentToWarn = loan.CurrentBtcPrice > 0 ? ((warnPrice - loan.CurrentBtcPrice) / loan.CurrentBtcPrice) * 100 : 0;
            var percentToLiq = loan.CurrentBtcPrice > 0 ? ((liqPrice - loan.CurrentBtcPrice) / loan.CurrentBtcPrice) * 100 : 0;

            estimates.Add(new LoanLiquidationEstimate(
                loan.Id,
                loan.Name,
                loan.CurrentBtcPrice,
                warnPrice,
                liqPrice,
                percentToWarn,
                percentToLiq));
        }

        return estimates;
    }

    public async Task<CollateralUtilization> GetCollateralUtilizationAsync(CancellationToken cancellationToken = default)
    {
        var active = await _loans.GetActiveAsync(cancellationToken);

        decimal totalCollateralBtc = 0m;
        decimal totalCollateralValueUsd = 0m;
        decimal totalLoanBalanceUsd = 0m;

        foreach (var loan in active)
        {
            var pays = await _payments.GetByLoanAsync(loan.Id, cancellationToken);
            var snap = await _accrual.CalculateAsync(loan, pays, null, cancellationToken);
            var balance = snap?.CurrentBalance ?? loan.CurrentBalance;

            totalCollateralBtc += loan.CollateralAmountBtc;
            var collateralValue = LoanCalculator.CalculateCollateralValue(loan.CollateralAmountBtc, loan.CurrentBtcPrice);
            totalCollateralValueUsd += collateralValue;
            totalLoanBalanceUsd += balance;
        }

        var activeCount = active.Count;
        decimal weightedAvgLtv = 0m;
        if (activeCount > 0 && totalCollateralValueUsd > 0)
        {
            weightedAvgLtv = totalLoanBalanceUsd / totalCollateralValueUsd;
        }

        var avgCollateralPerLoan = activeCount > 0 ? totalCollateralBtc / activeCount : 0m;

        return new CollateralUtilization(
            totalCollateralBtc,
            totalCollateralValueUsd,
            totalLoanBalanceUsd,
            weightedAvgLtv,
            activeCount,
            avgCollateralPerLoan);
    }

    public async Task<int> CalculatePortfolioRiskScoreAsync(CancellationToken cancellationToken = default)
    {
        var active = await _loans.GetActiveAsync(cancellationToken);
        if (active.Count == 0) return 0;

        decimal totalRiskScore = 0m;
        int loanCount = 0;

        foreach (var loan in active)
        {
            var pays = await _payments.GetByLoanAsync(loan.Id, cancellationToken);
            var snap = await _accrual.CalculateAsync(loan, pays, null, cancellationToken);
            var ltv = snap?.CurrentLtv ?? LoanCalculator.CalculateCurrentLtv(loan);

            var ltvRange = loan.LiquidationLtv - 0m;
            var ltvPosition = ltv / ltvRange;
            var loanScore = Math.Min(100m, ltvPosition * 100m);

            totalRiskScore += loanScore;
            loanCount++;
        }

        var portfolioScore = loanCount > 0 ? (int)(totalRiskScore / loanCount) : 0;
        return Math.Min(100, portfolioScore);
    }
}
