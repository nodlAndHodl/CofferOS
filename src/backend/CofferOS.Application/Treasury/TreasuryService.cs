using CofferOS.Application.Abstractions.Persistence;
using CofferOS.Application.Abstractions.Providers;
using CofferOS.Application.Contracts;
using CofferOS.Application.CostBasis;
using CofferOS.Application.Prices;
using CofferOS.Domain.Common;
using CofferOS.Domain.Treasury;
using Microsoft.Extensions.Logging;

namespace CofferOS.Application.Treasury;

/// <summary>
/// Use-case service for managing Bitcoin-collateralized loans (Phase 1: manual).
/// All calculations are performed via LoanCalculator to keep logic centralized and testable.
/// </summary>
public sealed class TreasuryService
{
    private readonly ILoanRepository _loans;
    private readonly ILoanPaymentRepository _payments;
    private readonly ILoanPriceSnapshotRepository _priceSnapshots;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IBitcoinPriceProvider _priceProvider;
    private readonly IExchangeRateProvider _exchangeRates;
    private readonly ILoanAccrualService _accrual;
    private readonly CoinGeckoHistoricalPriceService _historicalPriceService;
    private readonly CostBasisService _costBasis;
    private readonly ILogger<TreasuryService> _logger;

    public TreasuryService(
        ILoanRepository loans,
        ILoanPaymentRepository payments,
        ILoanPriceSnapshotRepository priceSnapshots,
        IUnitOfWork unitOfWork,
        IBitcoinPriceProvider priceProvider,
        IExchangeRateProvider exchangeRates,
        ILoanAccrualService accrual,
        CoinGeckoHistoricalPriceService historicalPriceService,
        CostBasisService costBasis,
        ILogger<TreasuryService> logger)
    {
        _loans = loans;
        _payments = payments;
        _priceSnapshots = priceSnapshots;
        _unitOfWork = unitOfWork;
        _priceProvider = priceProvider;
        _exchangeRates = exchangeRates;
        _accrual = accrual;
        _historicalPriceService = historicalPriceService;
        _costBasis = costBasis;
        _logger = logger;
    }

    public async Task<IReadOnlyList<LoanSummaryDto>> GetSummariesAsync(CancellationToken cancellationToken = default)
    {
        var loans = await _loans.GetAllAsync(cancellationToken);
        var results = new List<LoanSummaryDto>(loans.Count);
        foreach (var loan in loans)
        {
            var pays = await _payments.GetByLoanAsync(loan.Id, cancellationToken);
            var snap = await _accrual.CalculateAsync(loan, pays, null, cancellationToken);
            results.Add(await ToSummary(loan, snap, cancellationToken));
        }
        return results;
    }

    public async Task<LoanDetailDto?> GetDetailAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var loan = await _loans.GetByIdAsync(id, cancellationToken);
        if (loan is null) return null;
        var pays = await _payments.GetByLoanAsync(loan.Id, cancellationToken);
        var snap = await _accrual.CalculateAsync(loan, pays, null, cancellationToken);
        return await ToDetail(loan, snap, cancellationToken);
    }

    public async Task<LoanSummaryDto> CreateAsync(CreateLoanRequest request, CancellationToken cancellationToken = default)
    {
        var interestType = ParseInterestType(request.InterestType);
        var paymentFreq = ParsePaymentFrequency(request.PaymentFrequency);
        var interestPaymentSchedule = ParseInterestPaymentSchedule(request.InterestPaymentSchedule);

        // Calculate balance based on loan start date and interest accrual
        decimal calculatedBalance;
        var isBalanceOverridden = false;

        if (interestPaymentSchedule == InterestPaymentSchedule.InterestOnly)
        {
            // Interest-only loans don't accrue interest on the balance
            calculatedBalance = request.PrincipalAmount;
        }
        else
        {
            // Accruing loans: calculate balance from principal + accrued interest since loan start date
            var daysSinceStart = (DateTimeOffset.UtcNow.Date - request.LoanStartDate.Date).Days;
            if (daysSinceStart > 0)
            {
                var dailyRate = request.InterestRate / 365m;
                var accruedInterest = request.PrincipalAmount * dailyRate * daysSinceStart;
                calculatedBalance = request.PrincipalAmount + accruedInterest;
            }
            else
            {
                calculatedBalance = request.PrincipalAmount;
            }
        }

        var loan = Loan.Create(
            request.Name,
            request.Lender,
            request.PrincipalAmount,
            calculatedBalance,
            request.InterestRate,
            interestType,
            request.LoanStartDate,
            request.LoanTermMonths,
            paymentFreq,
            request.CollateralAmountBtc,
            request.CurrentBtcPrice,
            request.WarningLtv,
            request.LiquidationLtv,
            request.Notes,
            interestPaymentSchedule,
            request.Currency);

        if (isBalanceOverridden)
        {
            loan.UpdateBalance(request.CurrentBalance, isOverride: true);
        }

        await _loans.AddAsync(loan, cancellationToken);

        if (request.CollateralCostBasis is > 0)
        {
            await _costBasis.SetAsync(
                CostBasisTarget.LoanCollateral,
                loan.Id.ToString(),
                request.CollateralCostBasis.Value,
                cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await ToSummary(loan, null, cancellationToken);
    }

    public async Task<LoanDetailDto?> UpdateAsync(Guid id, UpdateLoanRequest request, CancellationToken cancellationToken = default)
    {
        var loan = await _loans.GetByIdAsync(id, cancellationToken);
        if (loan is null) return null;

        var interestType = ParseInterestType(request.InterestType);
        var paymentFreq = ParsePaymentFrequency(request.PaymentFrequency);
        var interestPaymentSchedule = ParseInterestPaymentSchedule(request.InterestPaymentSchedule);

        // Determine if the balance was explicitly edited in the form.
        var isBalanceOverridden = loan.BalanceOverridden ||
            Math.Abs(request.CurrentBalance - loan.CurrentBalance) >= 0.01m;

        // Recalculate the balance whenever the interest payment schedule changes.
        var scheduleChanged = loan.InterestPaymentSchedule != interestPaymentSchedule;
        var shouldRecalculateBalance = !isBalanceOverridden || scheduleChanged;

        decimal calculatedBalance;
        if (shouldRecalculateBalance)
        {
            // Use the same calculation logic as CreateAsync
            if (interestPaymentSchedule == InterestPaymentSchedule.InterestOnly)
            {
                // Interest-only loans don't accrue interest on the balance
                calculatedBalance = request.PrincipalAmount;
            }
            else
            {
                // Accruing loans: calculate balance from principal + accrued interest since loan start date
                var daysSinceStart = (DateTimeOffset.UtcNow.Date - request.LoanStartDate.Date).Days;
                if (daysSinceStart > 0)
                {
                    var dailyRate = request.InterestRate / 365m;
                    var accruedInterest = request.PrincipalAmount * dailyRate * daysSinceStart;
                    calculatedBalance = request.PrincipalAmount + accruedInterest;
                }
                else
                {
                    calculatedBalance = request.PrincipalAmount;
                }
            }
        }
        else
        {
            calculatedBalance = request.CurrentBalance;
        }

        loan.UpdateDetails(
            request.Name,
            request.Lender,
            request.PrincipalAmount,
            calculatedBalance,
            request.InterestRate,
            interestType,
            request.LoanStartDate,
            request.LoanTermMonths,
            paymentFreq,
            request.CollateralAmountBtc,
            request.CurrentBtcPrice,
            request.WarningLtv,
            request.LiquidationLtv,
            request.Notes,
            interestPaymentSchedule,
            request.Currency);

        if (shouldRecalculateBalance)
        {
            // Reset accrual state to the new start date so previously accrued interest from a different start date is cleared.
            loan.ResetAccrual(request.LoanStartDate);

            if (interestPaymentSchedule == InterestPaymentSchedule.Accruing)
            {
                var daysSinceStart = (DateTimeOffset.UtcNow.Date - request.LoanStartDate.Date).Days;
                if (daysSinceStart > 0)
                {
                    var dailyRate = request.InterestRate / 365m;
                    var accruedInterest = request.PrincipalAmount * dailyRate * daysSinceStart;
                    loan.AddAccruedInterest(accruedInterest, DateTimeOffset.UtcNow);
                }
                else
                {
                    loan.RefreshCurrentBalance();
                }
            }
            // For interest-only loans, ResetAccrual already set balance to principal, no further action needed.
        }
        else
        {
            loan.UpdateBalance(calculatedBalance, isOverride: true);
        }

        await _costBasis.SetAsync(
            CostBasisTarget.LoanCollateral,
            loan.Id.ToString(),
            request.CollateralCostBasis ?? 0m,
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Return a fresh accrual snapshot so the UI gets the updated accrued interest immediately.
        var payments = await _payments.GetByLoanAsync(loan.Id, cancellationToken);
        var snap = await _accrual.CalculateAsync(loan, payments, null, cancellationToken);
        return await ToDetail(loan, snap, cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var loan = await _loans.GetByIdAsync(id, cancellationToken);
        if (loan is null) return false;

        _loans.Remove(loan);
        await _costBasis.SetAsync(CostBasisTarget.LoanCollateral, id.ToString(), 0m, cancellationToken);
        return true;
    }

    public async Task<TreasurySummaryDto> GetTreasurySummaryAsync(CancellationToken cancellationToken = default)
    {
        var active = await _loans.GetActiveAsync(cancellationToken);

        decimal totalBalance = 0m;
        decimal totalCollateralBtc = 0m;
        decimal totalCollateralValue = 0m;

        LoanSummaryDto? highestRisk = null;
        decimal? highestLtv = null;

        foreach (var loan in active)
        {
            var pays = await _payments.GetByLoanAsync(loan.Id, cancellationToken);
            var snap = await _accrual.CalculateAsync(loan, pays, null, cancellationToken);

            totalBalance += snap.CurrentBalance;
            totalCollateralBtc += loan.CollateralAmountBtc;
            var cv = LoanCalculator.CalculateCollateralValue(loan.CollateralAmountBtc, loan.CurrentBtcPrice);
            totalCollateralValue += cv;

            if (cv > 0)
            {
                // accumulate for avg LTV
                // we will compute average after the loop using count
            }

            var ltv = snap.CurrentLtv;
            if (highestLtv is null || ltv > highestLtv)
            {
                highestLtv = ltv;
                highestRisk = await ToSummary(loan, snap, cancellationToken);
            }
        }

        var activeCount = active.Count;
        decimal avgLtv = 0m;
        if (activeCount > 0 && totalCollateralValue > 0)
        {
            avgLtv = totalBalance / totalCollateralValue;
        }

        var price = await _priceProvider.GetCurrentPriceAsync(cancellationToken);

        return new TreasurySummaryDto(
            activeCount,
            totalBalance,
            totalCollateralBtc,
            totalCollateralValue,
            avgLtv,
            highestRisk,
            price,
            _priceProvider.ProviderId);
    }

    private async Task<LoanSummaryDto> ToSummary(Loan loan, LoanAccrualSnapshot? snap = null, CancellationToken cancellationToken = default)
    {
        decimal balance = snap?.CurrentBalance ?? loan.CurrentBalance;
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
            await _costBasis.GetByReferenceAsync(CostBasisTarget.LoanCollateral, loan.Id.ToString(), cancellationToken),
            loan.Currency,
            loan.CreatedAt,
            loan.UpdatedAt);
    }

    private async Task<LoanDetailDto> ToDetail(Loan loan, LoanAccrualSnapshot? snap = null, CancellationToken cancellationToken = default)
    {
        decimal balance = snap?.CurrentBalance ?? loan.CurrentBalance;
        var collateralValue = LoanCalculator.CalculateCollateralValue(loan.CollateralAmountBtc, loan.CurrentBtcPrice);
        var ltv = LoanCalculator.CalculateCurrentLtv(balance, collateralValue);
        var warnPrice = LoanCalculator.CalculateWarningPrice(balance, loan.CollateralAmountBtc, loan.WarningLtv);
        var liqPrice = LoanCalculator.CalculateLiquidationPrice(balance, loan.CollateralAmountBtc, loan.LiquidationLtv);
        var distWarn = LoanCalculator.CalculateDistanceToWarning(ltv, loan.WarningLtv);
        var distLiq = LoanCalculator.CalculateDistanceToLiquidation(ltv, loan.LiquidationLtv);
        var buffer = LoanCalculator.CalculateRemainingCollateralBuffer(balance, loan.CollateralAmountBtc, loan.CurrentBtcPrice, loan.WarningLtv);

        return new LoanDetailDto(
            loan.Id,
            loan.Name,
            loan.Lender,
            loan.Status.ToString(),
            loan.Notes,
            loan.PrincipalAmount,
            balance,
            loan.InterestRate,
            loan.InterestType.ToString(),
            loan.LoanStartDate,
            loan.LoanTermMonths,
            loan.PaymentFrequency.ToString(),
            loan.InterestPaymentSchedule.ToString(),
            loan.CollateralAmountBtc,
            loan.CurrentBtcPrice,
            collateralValue,
            ltv,
            loan.WarningLtv,
            loan.LiquidationLtv,
            warnPrice,
            liqPrice,
            distWarn,
            distLiq,
            buffer,
            await _costBasis.GetByReferenceAsync(CostBasisTarget.LoanCollateral, loan.Id.ToString(), cancellationToken),
            loan.Currency,
            loan.CreatedAt,
            loan.UpdatedAt);
    }

    private static InterestType ParseInterestType(string value)
    {
        if (Enum.TryParse<InterestType>(value, true, out var t)) return t;
        throw new ArgumentException($"Invalid interest type: {value}");
    }

    private static PaymentFrequency ParsePaymentFrequency(string value)
    {
        if (Enum.TryParse<PaymentFrequency>(value, true, out var f)) return f;
        throw new ArgumentException($"Invalid payment frequency: {value}");
    }

    private static InterestPaymentSchedule ParseInterestPaymentSchedule(string value)
    {
        if (Enum.TryParse<InterestPaymentSchedule>(value, true, out var s)) return s;
        throw new ArgumentException($"Invalid interest payment schedule: {value}");
    }

    /// <summary>
    /// Fetches historical price data for a loan and returns calculated historical LTV.
    /// Populates the loan_price_snapshots table if not already present.
    /// Reconstructs historical balance at each snapshot date using payment history and accrual.
    /// </summary>
    public async Task<LoanHistoricalDataDto> GetHistoricalDataAsync(Guid loanId, CancellationToken cancellationToken = default)
    {
        var loan = await _loans.GetByIdAsync(loanId, cancellationToken);
        if (loan is null)
            throw new ArgumentException("Loan not found.", nameof(loanId));

        var loanCurrency = string.IsNullOrWhiteSpace(loan.Currency) ? "USD" : loan.Currency;

        var payments = await _payments.GetByLoanAsync(loanId, cancellationToken);

        var endDate = DateTimeOffset.UtcNow;
        var startDate = loan.LoanStartDate;

        // Ensure we have snapshots for every day in the requested range in the loan's currency.
        // If the stored snapshots are for a different currency (e.g. the loan's currency changed),
        // delete them and re-fetch the full history in the correct currency.
        var allSnapshots = await _priceSnapshots.GetByLoanAsync(loanId, cancellationToken);
        var requestedEnd = endDate.Date;
        var requestedStart = startDate.Date;

        if (allSnapshots.Count > 0 && allSnapshots.Any(s => !s.Currency.Equals(loanCurrency, StringComparison.OrdinalIgnoreCase)))
        {
            var mismatch = allSnapshots.First(s => !s.Currency.Equals(loanCurrency, StringComparison.OrdinalIgnoreCase)).Currency;
            _logger.LogWarning("Loan {LoanId} currency changed from {OldCurrency} to {NewCurrency}; removing {Count} outdated snapshots and re-fetching",
                loanId, mismatch, loanCurrency, allSnapshots.Count);
            foreach (var s in allSnapshots)
                _priceSnapshots.Remove(s);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            allSnapshots = [];
        }

        var existingDates = allSnapshots.Select(s => s.SnapshotDate.Date).ToHashSet();
        var requiredDates = Enumerable.Range(0, (requestedEnd - requestedStart).Days + 1)
            .Select(d => requestedStart.AddDays(d))
            .ToList();

        var missingDates = requiredDates.Where(d => !existingDates.Contains(d)).ToList();

        if (missingDates.Count > 0)
        {
            var fetchStart = missingDates.Min();
            var fetchEnd = missingDates.Max();

            _logger.LogInformation("Fetching historical BTC-{Currency} prices for loan {LoanId} from {Start:s} to {End:s} ({MissingCount} missing dates)",
                loanCurrency, loanId, fetchStart, fetchEnd, missingDates.Count);

            var prices = await _historicalPriceService.GetDailyPricesAsync(fetchStart, fetchEnd, loanCurrency, cancellationToken);
            _logger.LogInformation("CoinGecko returned {Count} daily BTC-{Currency} prices for loan {LoanId}", prices.Count, loanCurrency, loanId);

            if (prices.Count > 0)
            {
                var newSnapshots = new List<LoanPriceSnapshot>();
                foreach (var (date, price) in prices)
                {
                    if (missingDates.Contains(date.Date))
                    {
                        newSnapshots.Add(new LoanPriceSnapshot(loanId, date, price, loanCurrency, "coingecko"));
                    }
                }

                if (newSnapshots.Count > 0)
                {
                    await _priceSnapshots.AddRangeAsync(newSnapshots, cancellationToken);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                    _logger.LogInformation("Saved {Count} BTC-{Currency} price snapshots for loan {LoanId}", newSnapshots.Count, loanCurrency, loanId);

                    allSnapshots = allSnapshots.Concat(newSnapshots).ToList();
                }
            }
        }

        // Filter to the requested window and sort ascending
        var snapshotsInRange = allSnapshots
            .Where(s => s.SnapshotDate.Date >= requestedStart && s.SnapshotDate.Date <= requestedEnd)
            .OrderBy(s => s.SnapshotDate)
            .ToList();

        // Build response DTOs with calculated LTV using historical balance at each snapshot date.
        // Snapshots are now stored directly in the loan's currency, so PriceUsd is the BTC price in that currency.
        var snapshotDtos = new List<LoanPriceSnapshotDto>();
        foreach (var snapshot in snapshotsInRange)
        {
            // Reconstruct the loan's balance as of this snapshot date
            var historicalBalance = CalculateHistoricalBalance(loan, payments, snapshot.SnapshotDate);
            var btcPriceInLoanCurrency = snapshot.PriceUsd;
            var collateralValue = LoanCalculator.CalculateCollateralValue(loan.CollateralAmountBtc, btcPriceInLoanCurrency);
            var ltv = LoanCalculator.CalculateCurrentLtv(historicalBalance, collateralValue);

            snapshotDtos.Add(new LoanPriceSnapshotDto(
                snapshot.SnapshotDate,
                btcPriceInLoanCurrency,
                historicalBalance,
                collateralValue,
                ltv));
        }

        return new LoanHistoricalDataDto(loanId, loan.Currency, startDate, endDate, snapshotDtos);
    }

    /// <summary>
    /// Reconstructs the loan balance as it was on a specific historical date.
    /// Uses the accrual engine to compute principal + accrued interest up to that date,
    /// accounting for all payments made before that date.
    /// </summary>
    private decimal CalculateHistoricalBalance(Loan loan, IReadOnlyList<LoanPayment> payments, DateTimeOffset asOfDate)
    {
        // Filter payments that occurred on or before the snapshot date
        var paymentsUpToDate = payments
            .Where(p => p.PaymentDate.Date <= asOfDate.Date)
            .ToList();

        // Use the accrual engine to calculate balance at this historical date
        // This accounts for principal reduction from payments and interest accrual
        var snapshot = _accrual.CalculateAsync(loan, paymentsUpToDate, asOfDate, CancellationToken.None)
            .GetAwaiter()
            .GetResult();

        return snapshot.CurrentBalance;
    }
}
