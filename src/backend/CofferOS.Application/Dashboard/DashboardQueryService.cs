using CofferOS.Application.Abstractions.Dashboard;
using CofferOS.Application.Abstractions.Holdings;
using CofferOS.Application.Abstractions.Persistence;
using CofferOS.Application.Abstractions.Providers;
using CofferOS.Application.Contracts;
using CofferOS.Application.Treasury;

namespace CofferOS.Application.Dashboard;

/// <summary>
/// Orchestrates the assembly of the complete dashboard overview.
/// Single point of entry for the frontend to retrieve dashboard data.
/// Coordinates between holdings, treasury, wallets, and activity services.
/// </summary>
public sealed class DashboardQueryService : IDashboardQueryService
{
    private readonly IHoldingsService _holdings;
    private readonly DashboardService _dashboard;
    private readonly IBitcoinPriceProvider _priceProvider;
    private readonly ILoanRepository _loans;
    private readonly ILoanPaymentRepository _payments;
    private readonly ILoanAccrualService _accrual;

    public DashboardQueryService(
        IHoldingsService holdings,
        DashboardService dashboard,
        IBitcoinPriceProvider priceProvider,
        ILoanRepository loans,
        ILoanPaymentRepository payments,
        ILoanAccrualService accrual)
    {
        _holdings = holdings;
        _dashboard = dashboard;
        _priceProvider = priceProvider;
        _loans = loans;
        _payments = payments;
        _accrual = accrual;
    }

    public async Task<DashboardOverviewDto> GetOverviewAsync(CancellationToken cancellationToken = default)
    {
        // Wallet summary and recent activity (from existing dashboard aggregation)
        var walletDashboard = await _dashboard.GetAsync(cancellationToken);

        // Bitcoin holdings (aggregated, not per-wallet)
        var holdings = await _holdings.GetBreakdownAsync(cancellationToken);

        // Current price for value calc
        var btcPrice = await _priceProvider.GetCurrentPriceAsync(cancellationToken) ?? 0m;
        var totalValueUsd = holdings.TotalBitcoin * btcPrice;

        // Treasury / loan metrics (computed here so we don't block on slow external calls)
        var activeLoanCount = 0;
        decimal outstandingLoanBalanceUsd = 0m;
        decimal weightedAvgLtv = 0m;
        LoanSummaryDto? highestRiskLoan = null;

        var activeLoans = await _loans.GetActiveAsync(cancellationToken);
        if (activeLoans.Count > 0)
        {
            activeLoanCount = activeLoans.Count;

            decimal totalLoanBalance = 0m;
            decimal totalCollateralValue = 0m;
            decimal? highestLtv = null;

            foreach (var loan in activeLoans)
            {
                var pays = await _payments.GetByLoanAsync(loan.Id, cancellationToken);
                var snap = await _accrual.CalculateAsync(loan, pays, null, cancellationToken);
                var balance = snap?.CurrentBalance ?? loan.CurrentBalance;

                totalLoanBalance += balance;
                var collateralValue = LoanCalculator.CalculateCollateralValue(loan.CollateralAmountBtc, loan.CurrentBtcPrice);
                totalCollateralValue += collateralValue;

                var ltv = snap?.CurrentLtv ?? LoanCalculator.CalculateCurrentLtv(balance, collateralValue);
                if (highestLtv is null || ltv > highestLtv)
                {
                    highestLtv = ltv;
                    highestRiskLoan = ToLoanSummary(loan, snap);
                }
            }

            outstandingLoanBalanceUsd = totalLoanBalance;
            if (totalCollateralValue > 0)
            {
                weightedAvgLtv = totalLoanBalance / totalCollateralValue;
            }
        }

        return new DashboardOverviewDto(
            TotalBitcoin: holdings.TotalBitcoin,
            AvailableBitcoin: holdings.AvailableBitcoin,
            CollateralBitcoin: holdings.CollateralBitcoin,
            BitcoinPriceUsd: btcPrice,
            TotalValueUsd: totalValueUsd,
            ActiveLoanCount: activeLoanCount,
            OutstandingLoanBalanceUsd: outstandingLoanBalanceUsd,
            WeightedAverageLtv: weightedAvgLtv,
            HighestRiskLoan: highestRiskLoan,
            WalletCount: walletDashboard.WalletCount,
            TotalBalance: walletDashboard.TotalBalance,
            Wallets: walletDashboard.Wallets,
            RecentActivity: walletDashboard.RecentActivity,
            LastUpdatedUtc: DateTime.UtcNow);
    }

    private static LoanSummaryDto ToLoanSummary(Domain.Treasury.Loan loan, LoanAccrualSnapshot? snap = null)
    {
        var balance = snap?.CurrentBalance ?? loan.CurrentBalance;
        var collateralValue = LoanCalculator.CalculateCollateralValue(loan.CollateralAmountBtc, loan.CurrentBtcPrice);
        var ltv = LoanCalculator.CalculateCurrentLtv(balance, collateralValue);
        var distWarn = LoanCalculator.CalculateDistanceToWarning(ltv, loan.WarningLtv);
        var distLiq = LoanCalculator.CalculateDistanceToLiquidation(ltv, loan.LiquidationLtv);

        return new LoanSummaryDto(
            loan.Id,
            loan.Name,
            loan.Lender,
            loan.Status.ToString(),
            loan.PrincipalAmount,
            balance,
            loan.InterestRate,
            loan.InterestType.ToString(),
            loan.CollateralAmountBtc,
            loan.CurrentBtcPrice,
            collateralValue,
            ltv,
            loan.WarningLtv,
            loan.LiquidationLtv,
            distWarn,
            distLiq,
            loan.CreatedAt,
            loan.UpdatedAt);
    }
}
